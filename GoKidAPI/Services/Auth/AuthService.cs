using GoKidAPI.Data;
using GoKidAPI.DTO.Account.Auth.Requests;
using GoKidAPI.DTO.Account.Auth.Responses;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Enums;
using GoKidAPI.Helpers;
using GoKidAPI.Services.Email;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.OTP;
using GoKidAPI.Services.TokenStore;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenStoreService _tokenService;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly ResponseHandler _responseHandler;
        private readonly IFileUploader _fileUploader;

        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenStoreService tokenService,
            IOtpService otpService,
            IEmailService emailService,
            AppDbContext context,
            ILogger<AuthService> logger,
            ResponseHandler responseHandler,
            IFileUploader fileUploader)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _otpService = otpService;
            _emailService = emailService;
            _context = context;
            _logger = logger;
            _responseHandler = responseHandler;
            _fileUploader = fileUploader;
        }
        public async Task<Response<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                AppUser? user = null;
                var childDisplayName = "";

                switch (request.LoginAs)
                {
                    // 1. Parent / InstitutionAdmin / Supervisor → Email + Password
                    case UserType.Parent:
                    case UserType.InstitutionAdmin:
                    case UserType.Supervisor:
                    case UserType.PlatformAdmin:
                        user = await _userManager.FindByEmailAsync(request.Identifier);
                        if (user == null)
                            return _responseHandler.Unauthorized<AuthResponse>("Invalid login credentials");

                        if (user.UserType != request.LoginAs)
                            return _responseHandler.Unauthorized<AuthResponse>("You are not authorized to login as this role");

                        if (!user.EmailConfirmed)
                            return _responseHandler.Unauthorized<AuthResponse>("Please verify your email first");

                        if (string.IsNullOrEmpty(request.Password))
                            return _responseHandler.BadRequest<AuthResponse>("Password is required");

                        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                        if (!passwordValid)
                            return _responseHandler.Unauthorized<AuthResponse>("Invalid login credentials");

                        var accessToken = await _tokenService.CreateAccessTokenAsync(user);
                        var refreshToken = _tokenService.GenerateRefreshToken();
                        await _tokenService.InvalidateOldTokensAsync(user.Id);
                        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

                        var parentResp = new AuthResponse
                        {
                            AccessToken = accessToken,
                            RefreshToken = refreshToken,
                            UserId = user.Id,
                            DisplayName = user.DisplayName ?? user.Email!,
                            Email = user.Email!,
                            UserType = request.LoginAs.ToString()
                        };

                        return _responseHandler.Success(parentResp, "Login successful");


                    // 2. Child
                    case UserType.Child:
                        if (request.Identifier.Length != 6 || !int.TryParse(request.Identifier, out _))
                            return _responseHandler.BadRequest<AuthResponse>("Child code must be exactly 6 digits");

                        var child = await _context.Childrens
                            .Include(c => c.Parent)
                            .FirstOrDefaultAsync(c => c.RegistrationCode == request.Identifier);

                        if (child == null)
                            return _responseHandler.Unauthorized<AuthResponse>("Invalid or already used child code");


                        var childToken = _tokenService.GenerateChildJwt(child);

                        var childResp = new AuthResponse
                        {
                            AccessToken = childToken,
                            RefreshToken = _tokenService.GenerateRefreshToken(),
                            UserId = child.Id,
                            DisplayName = child.Name,
                            Email = "",
                            UserType = UserType.Child.ToString(),
                            ChildId = child.Id,
                            ParentId = child.Parent.Id
                        };

                        _logger.LogInformation("Child login successful: {Name} with code {Code}", child.Name, request.Identifier);
                        return _responseHandler.Success(childResp, "Child login successful");

                    default:
                        return _responseHandler.BadRequest<AuthResponse>("Invalid user type");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Identifier}", request.Identifier);
                return _responseHandler.ServerError<AuthResponse>("An error occurred during login");
            }
        }
        
        public async Task<Response<string>> RegisterParentAsync(RegisterParentRequest request)
        {
            try
            {
                var exists = await _userManager.FindByEmailAsync(request.Email);
                if (exists != null)
                    return _responseHandler.BadRequest<string>("Email is already in use");

                var user = new AppUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    DisplayName = request.FullName,
                    UserType = UserType.Parent,
                    EmailConfirmed = false, // will be confirmed via OTP
                };

                await _userManager.AddToRoleAsync(user, UserType.Parent.ToString());
                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Register failed for {Email}: {Errors}", request.Email, errors);
                    return _responseHandler.BadRequest<string>(errors);
                }

                // Create Parent Profile
                var parent = new Parent
                {
                    AppUserId = user.Id,
                    CreatedBy = user.Id
                };
                await _context.Parents.AddAsync(parent);
                await _context.SaveChangesAsync();

                // Generate OTP
                var otp = await _otpService.GenerateAndStoreOtpAsync(user.Id, "register");
                await _emailService.SendOtpEmailAsync(user, otp);

                _logger.LogInformation("Parent registered successfully: {Email}", request.Email);

                return _responseHandler.Success("Account created successfully. An activation code has been sent to your email.", "Registration Successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during parent registration: {Email}", request.Email);
                return _responseHandler.ServerError<string>("An error occurred during registration");
            }
        }
        public async Task<Response<string>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return _responseHandler.NotFound<string>("User not found");

            var isValid = await _otpService.ValidateOtpAsync(user.Id, request.Otp, "register");
            if (!isValid)
                return _responseHandler.BadRequest<string>("Invalid or expired verification code");

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Email verified successfully for {Email}", request.Email);

            return _responseHandler.Success("Your account has been successfully activated. You can now log in.", "Account Activated");
        }
        public async Task<Response<AuthResponse>> RefreshTokenAsync(string refreshToken)
        {
            var isValid = await _tokenService.IsValidAsync(refreshToken);
            if (!isValid)
                return _responseHandler.Unauthorized<AuthResponse>("Invalid refresh token");

            var tokenRecord = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsUsed);

            if (tokenRecord == null)
                return _responseHandler.Unauthorized<AuthResponse>("This token has already been used");

            var user = await _userManager.FindByIdAsync(tokenRecord.UserId);
            if (user == null)
                return _responseHandler.Unauthorized<AuthResponse>("User not found");

            // Mark as used
            tokenRecord.IsUsed = true;
            tokenRecord.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var newAccessToken = await _tokenService.CreateAccessTokenAsync(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            await _tokenService.SaveRefreshTokenAsync(user.Id, newRefreshToken);

            var response = new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                DisplayName = user.DisplayName ?? user.Email,
                Email = user.Email!,
                UserType = user.UserType.ToString()
            };

            return _responseHandler.Success(response, "Token refreshed successfully");
        }
        public async Task<Response<string>> ResendOtpAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.EmailConfirmed)
                return _responseHandler.BadRequest<string>("Invalid request");

            var otp = await _otpService.GenerateAndStoreOtpAsync(user.Id, "register");
            await _emailService.SendOtpEmailAsync(user, otp);

            return _responseHandler.Success("A new verification code has been sent", "Code Sent");
        }
        public async Task<Response<string>> LogoutAsync(string userId)
        {
            await _tokenService.InvalidateOldTokensAsync(userId);
            return _responseHandler.Success("Logout successful", "Logged Out");
        }

        public async Task<Response<CreateChildResponse>> CreateChildAsync(string parentId, CreateChildRequest request)
        {
            try
            {
                var parent = await _context.Parents
                    .Include(p => p.ActiveChild)
                    .FirstOrDefaultAsync(p => p.AppUserId == parentId);

                if (parent == null)
                    return _responseHandler.NotFound<CreateChildResponse>("Parent not found");

                // MVP: طفل واحد بس نشط
                if (parent.ActiveChild != null)
                    return _responseHandler.BadRequest<CreateChildResponse>("You can only add one child in the current version");

                string? avatarUrl = null;
                if (request.Avatar != null)
                {
                    var uploadResult = await _fileUploader.UploadAsync(request.Avatar);
                    avatarUrl = uploadResult.Url;
                }

                // Generate unique 6-digit code
                var code = IWWHelper.Random(6);

                var child = new Entity.Account.Users.Child
                {
                    Name = request.Name,
                    NickName = request.NickName,
                    Age = request.Age,
                    Gender = request.Gender,
                    RelationshipToParent = request.RelationshipToParent,
                    AvatarUrl = avatarUrl,
                    ParentId = parent.AppUserId,
                    RegistrationCode = code,
                    CodeGeneratedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = parent.AppUserId,
                };

                _context.Childrens.Add(child);
                await _context.SaveChangesAsync();

                //var qrBase64 = IWWHelper.GenerateQrCodeBase64(code);
                var qrBase64 = "qr-code-placeholder"; // Placeholder for QR code generation

                var responseDto = new CreateChildResponse
                {
                    ChildId = child.Id,
                    ChildName = child.Name,
                    RegistrationCode = code,
                    QrCodeBase64 = qrBase64
                };

                _logger.LogInformation("Child created successfully: {Name} - Code: {Code}", child.Name, code);
                return _responseHandler.Created(responseDto, "Child added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating child for parent {ParentId}", parentId);
                return _responseHandler.ServerError<CreateChildResponse>("Failed to create child");
            }
        }
    }
}
