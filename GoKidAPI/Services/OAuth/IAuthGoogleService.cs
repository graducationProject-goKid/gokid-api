using GoKidAPI.DTO.Account.Auth.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.OAuth
{
    public interface IGoogleAuthService
    {
        /// <summary>Validates the Google ID token and signs in (or auto-registers) a Parent account.</summary>
        Task<Response<AuthResponse>> AuthenticateAsync(string idToken);
    }
}
