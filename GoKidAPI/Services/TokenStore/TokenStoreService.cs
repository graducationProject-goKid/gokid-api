using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using GoKidAPI.Data;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Account.UserTokens;
using GoKidAPI.Enums;
using GoKidAPI.InfrastructreManage.Options;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GoKidAPI.Services.TokenStore
{
    public class TokenStoreService : ITokenStoreService
    {
        private readonly SymmetricSecurityKey _symmetricSecurityKey;
        private readonly UserManager<AppUser> _userManager; // To get user roles 
        private readonly JwtOptions _jwtSettings;
        private readonly AppDbContext _context;
        //private readonly IPermissionService _permissionService;
        // Dont forget to inject IPermissionService in the constructor

        public TokenStoreService(IOptions<JwtOptions> jwtOptions, UserManager<AppUser> userManager, AppDbContext context)
        {
            _jwtSettings = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
            _userManager = userManager;
            if (string.IsNullOrEmpty(_jwtSettings.Secret))
            {
                // For just confirmation on the secrect key is found
                throw new ArgumentException("JWT SigningKey is not configured.");
            }
            _symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            _context = context;
            //_permissionService = permissionService;
        }

        public async Task<string> CreateAccessTokenAsync(AppUser appUser)
        {
            var roles = await _userManager.GetRolesAsync(appUser);

            var Claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,appUser.Id.ToString()),
                new Claim(ClaimTypes.GivenName,appUser.UserName),
                new Claim(ClaimTypes.Email, appUser.Email),
            };

            foreach (var role in roles)
            {
                Claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var creds = new SigningCredentials(_symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
            var TokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(Claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds,
                Issuer = _jwtSettings.ValidIssuer,
                Audience = _jwtSettings.ValidAudience,
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(TokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        public async Task SaveRefreshTokenAsync(string userId, string refreshToken)
        {
            await _context.UserRefreshTokens.AddAsync(new UserRefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiryDateUtc = DateTime.UtcNow.AddDays(7),
                IsUsed = false
            });

            await _context.SaveChangesAsync();
        }
        public async Task InvalidateOldTokensAsync(string userId)
        {
            var tokens = await _context.UserRefreshTokens
                .Where(r => r.UserId == userId)
                .ToListAsync();

            _context.UserRefreshTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }
        public async Task<bool> IsValidAsync(string refreshToken)
        {
            return await _context.UserRefreshTokens
                .AnyAsync(r => r.Token == refreshToken && !r.IsUsed && r.ExpiryDateUtc > DateTime.UtcNow);
        }

        // Get user claims with its permissions
        //public async Task<List<Claim>> UserClaims(AppUser user)
        //{
        //    List<Claim> claims = new List<Claim>() {
        //                new(JwtRegisteredClaimNames.Name, value: user.UserName?? ""),
        //                new(JwtRegisteredClaimNames.NameId, value: user.Id.ToString()),
        //                new(JwtRegisteredClaimNames.Typ, value: user.Type.ToString().ToLower()),
        //                new(ClaimTypes.Role, value: user.Type.ToString().ToLower()),
        //                new(ClaimTypes.NameIdentifier, value: user.Id.ToString()),
        //            };

        //    //HashSet<string> hashClaims = await _permissionService.GetPermissionsAsync(user.Id);

        //    //foreach (string claim in hashClaims)
        //    //    claims.Add(new(GahbizClaims.Permissions, claim));
        //    return claims;
        //}
    }
}
