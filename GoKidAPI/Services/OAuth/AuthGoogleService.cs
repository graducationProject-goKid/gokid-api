using GoKidAPI.Data;
using GoKidAPI.DTO.Account.Auth.Responses;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Enums;
using GoKidAPI.Services.TokenStore;
using GoKidAPI.Shared;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.OAuth
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenStoreService _tokenStoreService;
        private readonly ResponseHandler _responseHandler;
        private readonly string _googleClientId;

        public GoogleAuthService(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ITokenStoreService tokenStoreService,
            ResponseHandler responseHandler,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _tokenStoreService = tokenStoreService;
            _responseHandler = responseHandler;
            _googleClientId = configuration["Authorization:Google:ClientId"]
                ?? throw new InvalidOperationException("Google ClientId is not configured in appsettings.json.");
        }

        public async Task<Response<AuthResponse>> AuthenticateAsync(string idToken)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await ValidateGoogleTokenAsync(idToken);
            }
            catch (InvalidJwtException ex)
            {
                return _responseHandler.Unauthorized<AuthResponse>($"Invalid Google token: {ex.Message}");
            }
            catch (Exception ex)
            {
                return _responseHandler.Unauthorized<AuthResponse>($"Google token validation failed: {ex.Message}");
            }

            if (string.IsNullOrEmpty(payload.Email) || !payload.EmailVerified)
                return _responseHandler.Unauthorized<AuthResponse>("Google account email is not verified.");

            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user != null)
                return await BuildAuthResponseAsync(user);

            // First-time login — create Parent account
            user = new AppUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                DisplayName = payload.Name ?? payload.Email,
                UserType = UserType.Parent,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return _responseHandler.BadRequest<AuthResponse>($"Account creation failed: {errors}");
            }

            await _userManager.AddToRoleAsync(user, "Parent");

            var parent = new Parent { Id = user.Id, CreatedBy = user.Id };
            await _context.Parents.AddAsync(parent);
            await _context.SaveChangesAsync();

            return await BuildAuthResponseAsync(user, parent);
        }

        private async Task<Response<AuthResponse>> BuildAuthResponseAsync(AppUser user, Parent? parent = null)
        {
            parent ??= await _context.Parents.FirstOrDefaultAsync(p => p.Id == user.Id);

            var accessToken = await _tokenStoreService.CreateAccessTokenAsync(user);
            var refreshToken = _tokenStoreService.GenerateRefreshToken();
            await _tokenStoreService.InvalidateOldTokensAsync(user.Id);
            await _tokenStoreService.SaveRefreshTokenAsync(user.Id, refreshToken);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                DisplayName = user.DisplayName ?? user.Email!,
                Email = user.Email!,
                UserType = UserType.Parent.ToString(),
                ProfileImageUrl = user.AvatarUrl,
                ChildId = parent?.ActiveChildId,
                ParentId = parent?.Id
            };

            return _responseHandler.Success(response, "Google authentication successful.");
        }

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId }
            };
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
    }
}
