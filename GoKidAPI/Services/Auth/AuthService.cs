using System.Text;

using GoKidAPI.Data;
using GoKidAPI.DTO.Account.Auth.Requests;
using GoKidAPI.DTO.Account.Auth.Responses;
using GoKidAPI.DTO.Account.Profile;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Enums;
using GoKidAPI.Helpers;
using GoKidAPI.InfrastructreManage.Options;
using GoKidAPI.Services.Email;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.OTP;
using GoKidAPI.Services.TokenStore;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using QRCoder;

namespace GoKidAPI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenStoreService _tokenService;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly ResponseHandler _responseHandler;
        private readonly IFileUploader _fileUploader;
        private readonly RedirectLinksSettings _passwordResetSettings;


        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenStoreService tokenService,
            IOtpService otpService,
            IEmailService emailService,
            AppDbContext context,
            ILogger<AuthService> logger,
            ResponseHandler responseHandler,
            IFileUploader fileUploader,
            IOptions<RedirectLinksSettings> passwordResetSettings)
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
            _passwordResetSettings = passwordResetSettings.Value;
        }
        public async Task<Response<AuthResponse>> LoginAsync(DTO.Account.Auth.Requests.LoginRequest request)
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
                            return _responseHandler.BadRequest<AuthResponse>("Invalid login credentials");

                        if (user.UserType != request.LoginAs)
                            return _responseHandler.BadRequest<AuthResponse>("You are not authorized to login as this role");

                        if (!user.EmailConfirmed)
                            return _responseHandler.BadRequest<AuthResponse>("Please verify your email first");

                        if (string.IsNullOrEmpty(request.Password))
                            return _responseHandler.BadRequest<AuthResponse>("Password is required");

                        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                        if (!passwordValid)
                            return _responseHandler.Unauthorized<AuthResponse>("Invalid login credentials");

                        var accessToken = await _tokenService.CreateAccessTokenAsync(user);
                        var refreshToken = _tokenService.GenerateRefreshToken();
                        await _tokenService.InvalidateOldTokensAsync(user.Id);
                        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

                        var parentResp = new AuthResponse();


                        if (request.LoginAs == UserType.Parent)
                        {
                            var parent = await _context.Parents
                                .Include(p => p.ActiveChild)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(p => p.AppUserId == user.Id);


                            parentResp.AccessToken = accessToken;
                            parentResp.RefreshToken = refreshToken;
                                parentResp.UserId = user.Id;
                            parentResp.DisplayName = user.DisplayName ?? user.Email!;
                            parentResp.Email = user.Email!;
                            parentResp.UserType = request.LoginAs.ToString();
                            parentResp.ChildId = parent.ActiveChildId;
                            parentResp.ParentId = parent.Id;
                            
                        }
                        else
                        {
                            parentResp.AccessToken = accessToken;
                            parentResp.RefreshToken = refreshToken;
                            parentResp.UserId = user.Id;
                            parentResp.DisplayName = user.DisplayName ?? user.Email!;
                            parentResp.Email = user.Email!;
                            parentResp.UserType = request.LoginAs.ToString();
                            
                        }


                        return _responseHandler.Success(parentResp, "Login successful");


                    // 2. Child
                    case UserType.Child:
                        if (request.Identifier.Length != 6)
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

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Register failed for {Email}: {Errors}", request.Email, errors);
                    return _responseHandler.BadRequest<string>(errors);
                }
                await _userManager.AddToRoleAsync(user, UserType.Parent.ToString());

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

                // Set the new child as the active child for the parent
                // Gharabawy : For MVP, we will allow only one child per parent
                // , so we can directly set it as active without checking for existing active child
                parent.ActiveChildId = child.Id;

                _context.Childrens.Add(child);
                await _context.SaveChangesAsync();

                //var qrBase64 = IWWHelper.GenerateQrCodeBase64(code);
                var qrBase64 = IWWHelper.GenerateQrCodeBase64(code); ; // Placeholder for QR code generation

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
       
        public async Task<Shared.Response<ForgetPasswordResponse>> ForgotPasswordAsync(ForgetPasswordRequest model, bool useOtp = true)
        {
            _logger.LogInformation("Starting ForgotPasswordAsync for Email: {Email}, UseOtp: {UseOtp}", model.Email, useOtp);

            // Find user by email or phone number
            AppUser? user = await FindUserByEmailAsync(model.Email);


            if (user == null)
            {
                _logger.LogWarning("User not found for Email: {Email}", model.Email);
                return _responseHandler.NotFound<ForgetPasswordResponse>("User not found.");
            }

            string otpOrLink = string.Empty;
            try
            {
                if (useOtp)
                {
                    // OTP mode
                    _logger.LogInformation("Generating OTP for UserId: {UserId}, Operation: forgot-password", user.Id);
                    otpOrLink = await _otpService.GenerateAndStoreOtpAsync(user.Id, "forgot-password");
                    await _emailService.SendResetPasswordEmailAsync(user.Email, "Reset Your GAHBIZ Password", user.DisplayName, otpOrLink, isOtp: true);
                }
                else
                {
                    // Link mode (using Identity token)
                    _logger.LogInformation("User found with ID: {UserId}. Generating reset token for link...", user.Id);

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)); // encode for URL

                    // Dynamic setup for tokenLink
                    // API link (direct reset)(for testing)
                    var apiLink = $"{_passwordResetSettings.BackendBaseUrl}/api/reset-password?token={token}&userId={Uri.EscapeDataString(user.Id)}";

                    // Frontend callback (when we have an UI for reseeting password)
                    var callbackUrl = $"{_passwordResetSettings.ClientBaseUrl}/reset-password?token={token}&userId={Uri.EscapeDataString(user.Id)}";

                    // choose between 2 links for confirmation link
                    otpOrLink = apiLink;

                    await _emailService.SendResetPasswordEmailAsync(user.Email, "Reset Your GO-KID Password", user.DisplayName, otpOrLink, isOtp: false); //Gharabawy TODO: link expiry longer
                }

                _logger.LogInformation("Reset {Mode} sent successfully to user ID: {UserId}", useOtp ? "OTP" : "link", user.Id);

                var response = new ForgetPasswordResponse
                {
                    UserId = user.Id
                };

                return _responseHandler.Success(response, $"Reset instructions sent to your email. Please check your inbox.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reset {Mode} to user ID: {UserId}", useOtp ? "OTP" : "link", user.Id);
                return _responseHandler.InternalServerError<ForgetPasswordResponse>("Failed to send reset instructions.");
            }
        }
        public async Task<Shared.Response<ResetPasswordResponse>> ResetPasswordAsync(DTO.Account.Auth.Requests.ResetPasswordRequest model, bool useOtp = true)
        {
            _logger.LogInformation("Starting ResetPasswordAsync for User ID: {UserId}, UseOtp: {UseOtp}", model.UserId, useOtp);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", model.UserId);
                return _responseHandler.NotFound<ResetPasswordResponse>("User not found.");
            }

            if (useOtp)
            {
                _logger.LogInformation("Validating OTP for UserId: {UserId}, Operation: forgot-password", user.Id);
                var isOtpValid = await _otpService.ValidateOtpAsync(model.UserId, model.Otp, "forgot-password");
                if (!isOtpValid)
                {
                    _logger.LogWarning("Invalid or expired OTP for User ID: {UserId}", model.UserId);
                    return _responseHandler.BadRequest<ResetPasswordResponse>("Invalid or expired OTP.");
                }
                _logger.LogInformation("Start reset Password for User ID: {UserId}", user.Id);

                var result = await _userManager.RemovePasswordAsync(user);
                _logger.LogInformation("Old Password deleted for User ID: {UserId}", user.Id);

                await _userManager.AddPasswordAsync(user, model.NewPassword);
                _logger.LogInformation("New Password Added for User ID: {UserId}", user.Id);
            }
            else
            {
                // Link mode: validate the token
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
                var isValid = await _userManager.VerifyUserTokenAsync(
                    user,
                    _userManager.Options.Tokens.PasswordResetTokenProvider,
                    "ResetPassword",
                    decodedToken
                );

                if (!isValid)
                {
                    return _responseHandler.BadRequest<ResetPasswordResponse>("Invalid or expired reset link.");
                }

                // Reset password using the SAME decoded token
                var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogWarning("Password reset failed for User ID: {UserId}. Errors: {Errors}", user.Id, string.Join(", ", errors));
                    return _responseHandler.BadRequest<ResetPasswordResponse>(string.Join(", ", errors));
                }
            }

            _logger.LogInformation("Password reset succeeded for User ID: {UserId}. Invalidating old tokens...", user.Id);

            // Invalidate all previous tokens for security
            await _tokenService.InvalidateOldTokensAsync(user.Id);

            var roles = await _userManager.GetRolesAsync(user);
            var response = new ResetPasswordResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Role = roles.FirstOrDefault()
            };
            _logger.LogInformation("ResetPasswordAsync completed successfully for User ID: {UserId}", user.Id);

            return _responseHandler.Success(response, "Password reset successfully. Please log in with your new password.");
        }
        public async Task<Response<string>> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            _logger.LogInformation("ChangePasswordAsync started for UserId: {UserId}", userId);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ChangePasswordAsync. UserId: {UserId}", userId);
                    return _responseHandler.NotFound<string>("User not found.");
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogWarning("Change password failed for UserId: {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                    return _responseHandler.BadRequest<string>(string.Join(", ", errors));
                }

                _logger.LogInformation("Password changed successfully for UserId: {UserId}", userId);

                try
                {
                    await _emailService.SendPasswordChangedEmailAsync(user.Email, user.UserName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Password changed but failed to send notification email for user {UserId}", userId);
                }

                return _responseHandler.Success<string>(null, "Password updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ChangePasswordAsync for UserId: {UserId}", userId);
                return _responseHandler.InternalServerError<string>("An error occurred while changing password.");
            }
        }

        public async Task<Shared.Response<ChangeEmailResponse>> ChangeEmailAsync(ChangeEmailRequest model, bool useOtp = true)
        {
            _logger.LogInformation("Starting ChangeEmailAsync for UserId: {UserId}, NewEmail: {NewEmail}, UseOtp: {UseOtp}", model.UserId, model.NewEmail, useOtp);

            // Find user
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", model.UserId);
                return _responseHandler.NotFound<ChangeEmailResponse>("User not found.");
            }

            // Validate password
            if (!await _userManager.CheckPasswordAsync(user, model.OldPassword))
            {
                _logger.LogWarning("Invalid password for UserId: {UserId}", model.UserId);
                return _responseHandler.BadRequest<ChangeEmailResponse>("Invalid password.");
            }

            // Check if new email is the same as current
            var currentEmail = user.Email;
            if (model.NewEmail == currentEmail)
            {
                _logger.LogWarning("New email is the same as current for UserId: {UserId}", model.UserId);
                return _responseHandler.BadRequest<ChangeEmailResponse>("Email is the same.");
            }

            // Check if new email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.NewEmail)
                               ?? await _userManager.FindByNameAsync(model.NewEmail);

            if (existingUser != null)
            {
                _logger.LogWarning("Email already exists: {NewEmail}", model.NewEmail);
                return _responseHandler.BadRequest<ChangeEmailResponse>("Email already exists.");
            }

            string otpOrLink = string.Empty;
            try
            {
                if (useOtp)
                {
                    // OTP mode
                    _logger.LogInformation("Generating OTP for UserId: {UserId}, Operation: change-email", user.Id);
                    otpOrLink = await _otpService.GenerateAndStoreOtpAsync(user.Id, "change-email");

                    // Gharabawy : TODO : Manage it and think about the cycle for cleaning
                    // Store newEmail in Redis for resend OTP (ResendOTPAsync)
                    //await _redis.StringSetAsync($"new-email:{user.Id}", model.NewEmail, TimeSpan.FromDays(7));

                    // InMemoryCahce
                    var cacheStoredEmail = _memoryCache.Set($"new-email:{user.Id}", model.NewEmail, TimeSpan.FromDays(7));

                    await _emailService.SendChangeEmailEmailAsync(model.NewEmail, user.DisplayName, otpOrLink, isOtp: true);
                }
                else
                {
                    // Link mode
                    _logger.LogInformation("Generating change email token for UserId: {UserId}", user.Id);

                    var token = await _userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);
                    token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                    // Dynamic setup for tokenLink
                    // API link (direct reset)(for testing)
                    var apiLink = $"{_passwordResetSettings.BackendBaseUrl}/api/update-email?token={token}&userId={Uri.EscapeDataString(user.Id)}";

                    // Frontend callback (when we have an UI for changing email)
                    var callbackUrl = $"{_passwordResetSettings.ClientBaseUrl}/update-email?token={token}&userId={Uri.EscapeDataString(user.Id)}";

                    // choose between 2 links for confirmation link
                    otpOrLink = apiLink;

                    await _emailService.SendChangeEmailEmailAsync(model.NewEmail, user.DisplayName, otpOrLink, isOtp: false);
                }

                _logger.LogInformation("Change email {Mode} sent successfully to {NewEmail} for UserId: {UserId}", useOtp ? "OTP" : "link", model.NewEmail, user.Id);

                var response = new ChangeEmailResponse
                {
                    UserId = user.Id,
                    NewEmail = model.NewEmail
                };

                return _responseHandler.Success(response, "Verification instructions sent to your new email.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send change email {Mode} to {NewEmail} for UserId: {UserId}", useOtp ? "OTP" : "link", model.NewEmail, user.Id);
                return _responseHandler.InternalServerError<ChangeEmailResponse>("Failed to send change email instructions.");
            }
        }
        public async Task<Shared.Response<UpdateEmailResponse>> UpdateEmailAsync(UpdateEmailRequest model, bool useOtp = true)
        {
            _logger.LogInformation("Starting UpdateEmailAsync for UserId: {UserId}, NewEmail: {NewEmail}, UseOtp: {UseOtp}", model.UserId, model.NewEmail, useOtp);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", model.UserId);
                return _responseHandler.NotFound<UpdateEmailResponse>("User not found.");
            }

            try
            {
                if (useOtp)
                {
                    // OTP mode (verification step)
                    _logger.LogInformation("Validating OTP for UserId: {UserId}, Operation: change-email", user.Id);
                    var isOtpValid = await _otpService.ValidateOtpAsync(model.UserId, model.Otp, "change-email");
                    if (!isOtpValid)
                    {
                        _logger.LogWarning("Invalid or expired OTP for UserId: {UserId}", model.UserId);
                        return _responseHandler.BadRequest<UpdateEmailResponse>("Invalid or expired OTP.");
                    }
                }
                //else
                //{
                //    // Link mode (verification step + changing step)
                //    var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
                //    var result = await _userManager.ChangeEmailAsync(user, model.NewEmail, decodedToken);
                //    if (!result.Succeeded)
                //    {
                //        var errors = result.Errors.Select(e => e.Description).ToList();
                //        _logger.LogWarning("Email change failed for UserId: {UserId}. Errors: {Errors}", user.Id, string.Join(", ", errors));
                //        return _responseHandler.BadRequest<UpdateEmailResponse>(string.Join(", ", errors));
                //    }
                //}

                // Update email if OTP is valid
                if (useOtp)
                {
                    user.Email = model.NewEmail;
                    user.NormalizedEmail = model.NewEmail.ToUpper();
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        var errors = updateResult.Errors.Select(e => e.Description).ToList();
                        _logger.LogWarning("Email update failed for UserId: {UserId}. Errors: {Errors}", user.Id, string.Join(", ", errors));
                        return _responseHandler.BadRequest<UpdateEmailResponse>(string.Join(", ", errors));
                    }
                }

                _logger.LogInformation("Email updated successfully for UserId: {UserId} to {NewEmail}", user.Id, model.NewEmail);

                var response = new UpdateEmailResponse
                {
                    UserId = user.Id,
                    NewEmail = model.NewEmail
                };

                return _responseHandler.Success(response, "Email updated successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogError("Database concurrency error updating email for UserId: {UserId}", user.Id);
                return _responseHandler.InternalServerError<UpdateEmailResponse>("Database concurrency error.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating email for UserId: {UserId}", user.Id);
                return _responseHandler.InternalServerError<UpdateEmailResponse>("Database error.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating email for UserId: {UserId}", user.Id);
                return _responseHandler.InternalServerError<UpdateEmailResponse>("Unexpected error.");
            }
        }


        private async Task<AppUser?> FindUserByEmailAsync(string email)
        {
            if (!string.IsNullOrEmpty(email))
                return await _userManager.FindByEmailAsync(email);
            return null;
        }

        public async Task<Response<bool>> UpdateFcmTokenAsync(string userId, string fcmToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return _responseHandler.NotFound<bool>("User not found.");

            user.FcmToken = fcmToken;
            await _userManager.UpdateAsync(user);

            return _responseHandler.Success(true, "FCM token updated successfully.");
        }

        public async Task<Response<ParentProfileResponse>> GetParentProfileAsync(string appUserId)
        {
            var parent = await _context.Parents
                .Include(p => p.AppUser)
                .Include(p => p.ActiveChild)
                    .ThenInclude(c => c!.Class)
                .Include(p => p.ActiveChild)
                    .ThenInclude(c => c!.Institution)
                .FirstOrDefaultAsync(p => p.AppUserId == appUserId);

            if (parent is null)
                return _responseHandler.NotFound<ParentProfileResponse>("Parent profile not found.");

            var response = new ParentProfileResponse
            {
                Id = parent.AppUserId,
                DisplayName = parent.AppUser.DisplayName,
                Email = parent.AppUser.Email,
                AvatarUrl = parent.AppUser.AvatarUrl,
                CreatedAt = parent.AppUser.CreatedAt,
                ActiveChild = parent.ActiveChild is null ? null : new ChildSummary
                {
                    Id = parent.ActiveChild.Id,
                    Name = parent.ActiveChild.Name,
                    NickName = parent.ActiveChild.NickName,
                    Age = parent.ActiveChild.Age,
                    Gender = parent.ActiveChild.Gender,
                    AvatarUrl = parent.ActiveChild.AvatarUrl,
                    TotalPoints = parent.ActiveChild.TotalPoints,
                    HighestPoints = parent.ActiveChild.HighestPoints,
                    RegistrationCode = parent.ActiveChild.RegistrationCode,
                    ClassName = parent.ActiveChild.Class?.Name,
                    InstitutionName = parent.ActiveChild.Institution?.Name,
                }
            };

            return _responseHandler.Success(response, "Parent profile retrieved successfully.");
        }

        public async Task<Response<ChildProfileResponse>> GetChildProfileAsync(string childId)
        {
            var child = await _context.Childrens
                .Include(c => c.Class)
                .Include(c => c.Institution)
                .FirstOrDefaultAsync(c => c.Id == childId);

            if (child is null)
                return _responseHandler.NotFound<ChildProfileResponse>("Child profile not found.");

            var response = new ChildProfileResponse
            {
                Id = child.Id,
                Name = child.Name,
                NickName = child.NickName,
                Age = child.Age,
                Gender = child.Gender,
                RelationshipToParent = child.RelationshipToParent,
                AvatarUrl = child.AvatarUrl,
                TotalPoints = child.TotalPoints,
                HighestPoints = child.HighestPoints,
                RegistrationCode = child.RegistrationCode,
                ClassName = child.Class?.Name,
                InstitutionName = child.Institution?.Name,
            };

            return _responseHandler.Success(response, "Child profile retrieved successfully.");
        }

    }
}
