using GoKidAPI.DTO.Account.Auth.Requests;
using GoKidAPI.DTO.Account.Auth.Responses;
using GoKidAPI.Services.OAuth;
using GoKidAPI.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    /// <summary>OAuth / social authentication endpoints</summary>
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IGoogleAuthService _googleAuthService;

        public AuthController(IGoogleAuthService googleAuthService)
        {
            _googleAuthService = googleAuthService;
        }

        /// <summary>
        /// Authenticate with Google Sign-In (Parent accounts only)
        /// </summary>
        /// <remarks>
        /// Pass the Google ID token obtained from Google Sign-In on the client device.
        /// On first login a Parent account is automatically created.
        /// On subsequent logins fresh JWT and refresh tokens are issued.
        /// </remarks>
        /// <response code="200">Authentication successful — returns JWT + refresh tokens</response>
        /// <response code="400">Account creation error</response>
        /// <response code="401">Invalid or expired Google ID token</response>
        [HttpPost("google")]
        [ProducesResponseType(typeof(Response<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response<string>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
        {
            var result = await _googleAuthService.AuthenticateAsync(request.IdToken);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
