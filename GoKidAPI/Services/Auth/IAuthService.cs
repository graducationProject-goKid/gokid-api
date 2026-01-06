using GoKidAPI.DTO.Account.Auth.Requests;
using GoKidAPI.DTO.Account.Auth.Responses;
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
    }
}
