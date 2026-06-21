using GoKidAPI.DTO.Gifts.Requests;
using GoKidAPI.DTO.Gifts.Responses;

namespace GoKidAPI.Services.Rewards
{
    public interface IRewardService
    {
        Task<Shared.Response<RewardResponse>> CreateRewardAsync(string parentId, CreateRewardRequest request);
        Task<Shared.Response<object>> DeleteRewardAsync(string parentId, string rewardId);
        Task<Shared.Response<List<RewardResponse>>> GetMyRewardsAsync(string parentId);
        Task<Shared.Response<RewardResponse>> GiveRewardToChildAsync(string parentId, string rewardId);
        Task<Shared.Response<List<RewardResponse>>> GetChildRewardsAsync(string childId);
    }
}
