using GoKidAPI.Entity.Account.Identity;

namespace GoKidAPI.Services.TokenStore
{
    public interface ITokenStoreService
    {
        Task<string> CreateAccessTokenAsync(AppUser appUser);
        string GenerateRefreshToken();
        Task SaveRefreshTokenAsync(string userId, string refreshToken);
        Task InvalidateOldTokensAsync(string userId);
        Task<bool> IsValidAsync(string refreshToken);
        //public Task<List<Claim>> UserClaims(AppUser user);
    }
}
