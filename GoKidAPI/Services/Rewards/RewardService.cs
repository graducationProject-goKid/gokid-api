// Services/Rewards/RewardService.cs
using GoKidAPI.Data;
using GoKidAPI.DTO.Gifts.Requests;
using GoKidAPI.DTO.Gifts.Responses;
using GoKidAPI.Entity.Gifts;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Gifts;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Shared;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Rewards
{
    public class RewardService : IRewardService
    {
        private readonly AppDbContext _context;
        private readonly IFileUploader _fileUploader;
        private readonly ResponseHandler _response;
        private readonly INotificationService _notificationService;

        public RewardService(
            AppDbContext context,
            IFileUploader fileUploader,
            ResponseHandler response,
            INotificationService notificationService)
        {
            _context = context;
            _fileUploader = fileUploader;
            _response = response;
            _notificationService = notificationService;
        }

        public async Task<Response<RewardResponse>> CreateRewardAsync(string parentId, CreateRewardRequest request)
        {
            // تحقق إن الطفل تابع للـ parent ده
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == request.ChildId
                                       && c.ParentId == parentId
                                       && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<RewardResponse>("Child not found or does not belong to you");

            var reward = new Reward
            {
                NameEn = request.NameEn,
                NameAr = request.NameAr,
                DescriptionEn = request.DescriptionEn,
                DescriptionAr = request.DescriptionAr,
                TargetPoints = request.TargetPoints,
                ParentId = parentId,
                ChildId = request.ChildId,
                Status = RewardStatus.Pending,
                CreatedBy = parentId
            };

            if (request.Image != null)
            {
                var upload = await _fileUploader.UploadAsync(request.Image);
                reward.ImageUrl = upload.Url;
                reward.ImagePublicId = upload.PublicId;
            }

            _context.Rewards.Add(reward);
            await _context.SaveChangesAsync();

            return _response.Created(MapToResponse(reward, child), "Reward created successfully");
        }

        public async Task<Response<object>> DeleteRewardAsync(string parentId, string rewardId)
        {
            var reward = await _context.Rewards
                .FirstOrDefaultAsync(r => r.Id == rewardId
                                       && r.ParentId == parentId
                                       && !r.IsDeleted);

            if (reward == null)
                return _response.NotFound<object>("Reward not found");

            if (reward.Status == RewardStatus.Given)
                return _response.BadRequest<object>("Cannot delete a reward that has already been given");

            reward.IsDeleted = true;
            reward.UpdatedAt = DateTime.UtcNow;
            reward.UpdatedBy = parentId;

            await _context.SaveChangesAsync();
            return _response.Deleted<object>("Reward deleted successfully");
        }

        public async Task<Response<List<RewardResponse>>> GetMyRewardsAsync(string parentId)
        {
            var rewards = await _context.Rewards
                .Include(r => r.Child)
                .Where(r => r.ParentId == parentId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = rewards.Select(r => MapToResponse(r, r.Child)).ToList();
            return _response.Success(result, "Rewards retrieved successfully");
        }

        public async Task<Response<RewardResponse>> GiveRewardToChildAsync(
    string parentId,
    string rewardId,
    GiveRewardRequest request)
        {
            var reward = await _context.Rewards
                .Include(r => r.Child)
                .FirstOrDefaultAsync(r => r.Id == rewardId
                                       && r.ParentId == parentId
                                       && !r.IsDeleted);

            if (reward == null)
                return _response.NotFound<RewardResponse>("Reward not found");

            if (reward.Status == RewardStatus.Given)
                return _response.BadRequest<RewardResponse>("Reward already given to child");

            if (reward.Child.TotalPoints < reward.TargetPoints)
                return _response.BadRequest<RewardResponse>(
                    $"Child hasn't reached the target yet. Current: {reward.Child.TotalPoints}, Target: {reward.TargetPoints}");

            reward.Status = RewardStatus.Given;
            reward.GivenAt = DateTime.UtcNow;
            reward.UpdatedAt = DateTime.UtcNow;
            reward.UpdatedBy = parentId;
            reward.MessageToChild = request?.MessageToChild;

            await _context.SaveChangesAsync();

            var notificationMessage = string.IsNullOrWhiteSpace(request?.MessageToChild)
                ? $"Your parent gave you: \"{reward.NameEn}\". Well done!"
                : $"Your parent gave you: \"{reward.NameEn}\".\n\nMessage: {request.MessageToChild}";

            await _notificationService.SendAsync(
                reward.ChildId,
                NotificationType.RewardGiven,
                "You Got a Reward!",
                notificationMessage,
                reward.Id);

            return _response.Success(
                MapToResponse(reward, reward.Child),
                "Reward given to child successfully!");
        }

        public async Task<Response<List<RewardResponse>>> GetChildRewardsAsync(string childId)
        {
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<List<RewardResponse>>("Child not found");

            var rewards = await _context.Rewards
                .Include(r => r.Child)
                .Where(r => r.ChildId == childId
                         && r.Status == RewardStatus.Given  // بس اللي اتادت فعلاً
                         && !r.IsDeleted)
                .OrderByDescending(r => r.GivenAt)
                .ToListAsync();

            var result = rewards.Select(r => MapToResponse(r, r.Child)).ToList();
            return _response.Success(result, "Rewards retrieved successfully");
        }


        private static RewardResponse MapToResponse(Reward reward, Entity.Account.Users.Child child) => new()
        {
            Id = reward.Id,
            NameEn = reward.NameEn,
            NameAr = reward.NameAr,
            DescriptionEn = reward.DescriptionEn,
            DescriptionAr = reward.DescriptionAr,
            ImageUrl = reward.ImageUrl,
            TargetPoints = reward.TargetPoints,
            ChildId = reward.ChildId,
            ChildName = child.Name,
            ChildCurrentPoints = child.TotalPoints,
            TargetReached = child.TotalPoints >= reward.TargetPoints,
            Status = reward.Status,
            GivenAt = reward.GivenAt,
            CreatedAt = reward.CreatedAt,
            MessageToChild = reward.MessageToChild,
        };
    }
}