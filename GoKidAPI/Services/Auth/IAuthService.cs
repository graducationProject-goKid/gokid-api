using GoKidAPI.DTO.Account.Auth.Requests;
using GoKidAPI.DTO.Account.Auth.Responses;
using GoKidAPI.DTO.Account.Profile;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Identity.Data;

namespace GoKidAPI.Services.Auth
{
    public interface IAuthService
    {
        Task<Response<string>> RegisterParentAsync(RegisterParentRequest request);
        Task<Response<CreateChildResponse>> CreateChildAsync(string parentId, CreateChildRequest request);
        Task<Response<string>> VerifyOtpAsync(VerifyOtpRequest request);
        Task<Response<AuthResponse>> LoginAsync(DTO.Account.Auth.Requests.LoginRequest request);
        Task<Response<AuthResponse>> RefreshTokenAsync(string refreshToken);
        Task<Response<string>> ResendOtpAsync(string email);
        Task<Response<string>> LogoutAsync(string userId);

        Task<Response<string>> ChangePasswordAsync(string userId, ChangePasswordRequest request);
        Task<Response<ForgetPasswordResponse>> ForgotPasswordAsync(ForgetPasswordRequest model, bool useOtp = true);
        Task<Response<ResetPasswordResponse>> ResetPasswordAsync(DTO.Account.Auth.Requests.ResetPasswordRequest model, bool useOtp = true);

        Task<Response<bool>> UpdateFcmTokenAsync(string userId, string fcmToken);

        Task<Response<ParentProfileResponse>> GetParentProfileAsync(string appUserId);
        Task<Response<ChildProfileResponse>> GetChildProfileAsync(string childId);
    }
}
