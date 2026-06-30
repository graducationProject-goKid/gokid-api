using System.Security.Claims;

using GoKidAPI.DTO.Account.Auth.Requests;
using GoKidAPI.DTO.Account.Auth.Responses;
using GoKidAPI.Services.Auth;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ResponseHandler _responseHandler;

        public AccountController(IAuthService authService, ResponseHandler responseHandler)
        {
            _authService = authService;
            _responseHandler = responseHandler;
        }

        /// <summary>
        /// Register a new parent account
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/account/register
        ///     {
        ///       "email": "parent@example.com",
        ///       "password": "Pass123!",
        ///       "confirmPassword": "Pass123!",
        ///       "fullName": "أحمد محمد"
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">Account created successfully. OTP sent to email</response>
        /// <response code="400">Email already exists or validation error</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterParentRequest request)
        {
            var result = await _authService.RegisterParentAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Verify registration OTP
        /// </summary>
        /// <response code="200">Account activated successfully</response>
        /// <response code="400">Invalid or expired OTP</response>
        /// <response code="404">User not found</response>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Login for Parent, Child, Institution Staff or Platform Admin
        /// </summary>
        /// <remarks>
        /// - Parent/Staff: Use email + password + loginAs = "Parent" or "InstitutionAdmin" or "PlatformAdmin" or "Supervisor".
        /// - Child: Use 6-digit code + loginAs = "Child" (no password)
        /// </remarks>
        /// <response code="200">Login successful + JWT token</response>
        /// <response code="401">Invalid credentials</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Refresh expired access token
        /// </summary>
        /// <response code="200">New access & refresh tokens</response>
        /// <response code="401">Invalid or expired refresh token</response>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Resend OTP (for registration or forgot password later)
        /// </summary>
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
            var result = await _authService.ResendOtpAsync(request.Email);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Logout (invalidate all refresh tokens)
        /// </summary>
        [HttpPost("logout")]
        [Authorize] // Parent only
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(_responseHandler.Unauthorized<string>("Unauthorized"));

            var result = await _authService.LogoutAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Add a new child (Parent only)
        /// </summary>
        /// <remarks>
        /// Returns child details + 6-digit registration code + QR code (base64)
        /// MVP: Only one child per parent allowed
        /// </remarks>
        /// <response code="201">Child created + code generated</response>
        /// <response code="400">Already has a child</response>
        [HttpPost("child")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> CreateChild([FromForm] CreateChildRequest request)
        {
            var parentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(parentId))
                return Unauthorized(_responseHandler.Unauthorized<object>("Unauthorized"));

            var result = await _authService.CreateChildAsync(parentId, request);
            return StatusCode((int)result.StatusCode, result);
        }
        /// <summary>
        /// Sends password reset instructions (OTP or reset link) to the user's email.
        /// </summary>
        
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(Response<ForgetPasswordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<ForgetPasswordResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgetPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request, useOtp: true);
            return StatusCode((int)result.StatusCode, result);
        }
        /// <summary>
        /// Resets the user's password using OTP or reset token.
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(Response<ResetPasswordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<ResetPasswordResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response<ResetPasswordResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request, useOtp: true);
            return StatusCode((int)result.StatusCode, result);
        }
        /// <summary>
        /// Changes the current authenticated user's password.
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(Response<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _authService.ChangePasswordAsync(userId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Returns the profile of the authenticated user.
        /// Parent: returns parent info + active child summary.
        /// Child: returns child info with class and institution.
        /// </summary>
        [Authorize(Roles = "Parent,Child")]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role == "Parent")
            {
                var result = await _authService.GetParentProfileAsync(userId);
                return StatusCode((int)result.StatusCode, result);
            }
            else
            {
                var result = await _authService.GetChildProfileAsync(userId);
                return StatusCode((int)result.StatusCode, result);
            }
        }

        /// <summary>
        /// Registers or updates the device FCM token for push notifications.
        /// Call this after login from the mobile app (Parent / Child).
        /// </summary>
        [Authorize(Roles = "Parent,Child")]
        [HttpPut("fcm-token")]
        public async Task<IActionResult> UpdateFcmToken([FromBody] string fcmToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _authService.UpdateFcmTokenAsync(userId, fcmToken);
            return StatusCode((int)result.StatusCode, result);
        }

    }
}
