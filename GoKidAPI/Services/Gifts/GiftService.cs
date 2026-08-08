using GoKidAPI.Data;
using GoKidAPI.DTO.Gifts.Requests;
using GoKidAPI.DTO.Gifts.Responses;
using GoKidAPI.Entity;
using GoKidAPI.Entity.Gifts;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Gifts;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Gifts
{
    public class GiftService : IGiftService
    {
        private readonly AppDbContext _context;
        private readonly IFileUploader _fileUploader;
        private readonly ResponseHandler _response;
        private readonly ILogger<GiftService> _logger;
        private readonly INotificationService _notificationService;

        public GiftService(
            AppDbContext context,
            IFileUploader fileUploader,
            ResponseHandler response,
            ILogger<GiftService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _fileUploader = fileUploader;
            _response = response;
            _logger = logger;
            _notificationService = notificationService;
        }

        // =================== Platform Admin ===================

        public async Task<Response<GiftResponse>> CreateGiftAsync(CreateGiftRequest request, string adminId)
        {
            var gift = new Gift
            {
                NameEn = request.NameEn,
                NameAr = request.NameAr,
                DescriptionEn = request.DescriptionEn,
                DescriptionAr = request.DescriptionAr,
                PointsCost = request.PointsCost,
                Type = request.Type,
                Status = GiftStatus.Active,
                CreatedBy = adminId
            };

            if (request.Image != null)
            {
                var upload = await _fileUploader.UploadAsync(request.Image);
                gift.ImageUrl = upload.Url;
                gift.ImagePublicId = upload.PublicId;
            }

            _context.Gifts.Add(gift);
            await _context.SaveChangesAsync();

            return _response.Created(MapToResponse(gift), "Gift created successfully");
        }

        public async Task<Response<GiftResponse>> UpdateGiftAsync(string giftId, UpdateGiftRequest request, string adminId)
        {
            var gift = await _context.Gifts
                .FirstOrDefaultAsync(g => g.Id == giftId && !g.IsDeleted);

            if (gift == null)
                return _response.NotFound<GiftResponse>("Gift not found");

            if (!string.IsNullOrWhiteSpace(request.NameEn)) gift.NameEn = request.NameEn;
            if (!string.IsNullOrWhiteSpace(request.NameAr)) gift.NameAr = request.NameAr;
            if (!string.IsNullOrWhiteSpace(request.DescriptionEn)) gift.DescriptionEn = request.DescriptionEn;
            if (!string.IsNullOrWhiteSpace(request.DescriptionAr)) gift.DescriptionAr = request.DescriptionAr;
            if (request.PointsCost.HasValue) gift.PointsCost = request.PointsCost.Value;
            if (request.Type.HasValue) gift.Type = request.Type.Value;

            if (request.Image != null)
            {
                var upload = await _fileUploader.UploadAsync(request.Image);
                gift.ImageUrl = upload.Url;
                gift.ImagePublicId = upload.PublicId;
            }

            gift.UpdatedAt = DateTime.UtcNow;
            gift.UpdatedBy = adminId;

            await _context.SaveChangesAsync();
            return _response.Success(MapToResponse(gift), "Gift updated successfully");
        }

        public async Task<Response<object>> DeleteGiftAsync(string giftId, string adminId)
        {
            var gift = await _context.Gifts
                .FirstOrDefaultAsync(g => g.Id == giftId && !g.IsDeleted);

            if (gift == null)
                return _response.NotFound<object>("Gift not found");

            gift.IsDeleted = true;
            gift.UpdatedAt = DateTime.UtcNow;
            gift.UpdatedBy = adminId;

            await _context.SaveChangesAsync();
            return _response.Deleted<object>("Gift deleted successfully");
        }

        public async Task<Response<object>> ChangeGiftStatusAsync(string giftId, GiftStatus status, string adminId)
        {
            var gift = await _context.Gifts
                .FirstOrDefaultAsync(g => g.Id == giftId && !g.IsDeleted);

            if (gift == null)
                return _response.NotFound<object>("Gift not found");

            gift.Status = status;
            gift.UpdatedAt = DateTime.UtcNow;
            gift.UpdatedBy = adminId;

            await _context.SaveChangesAsync();
            return _response.Success<object>(null, $"Gift status changed to {status}");
        }

        public async Task<Response<PaginatedList<GiftResponse>>> GetAllGiftsAsync(
            int pageNumber, int pageSize, GiftType? type, GiftStatus? status)
        {
            var query = _context.Gifts.Where(g => !g.IsDeleted);

            if (type.HasValue) query = query.Where(g => g.Type == type.Value);
            if (status.HasValue) query = query.Where(g => g.Status == status.Value);

            var totalCount = await query.CountAsync();
            var gifts = await query
                .OrderByDescending(g => g.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginated = new PaginatedList<GiftResponse>(
                gifts.Select(MapToResponse).ToList(),
                pageNumber, pageSize, totalCount);

            return _response.Success(paginated, "Gifts retrieved successfully");
        }

        // =================== Child ===================

        public async Task<Response<PaginatedList<GiftResponse>>> GetAvailableGiftsAsync(
            string childId, int pageNumber, int pageSize)
        {
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<PaginatedList<GiftResponse>>("Child not found");

            // جيب الـ gifts اللي الطفل اشتراها قبل كده
            var purchasedGiftIds = await _context.ChildGifts
                .Where(cg => cg.ChildId == childId)
                .Select(cg => cg.GiftId)
                .ToListAsync();

            var query = _context.Gifts
                .Where(g => !g.IsDeleted
                            && g.Status == GiftStatus.Active
                            && !purchasedGiftIds.Contains(g.Id));

            var totalCount = await query.CountAsync();
            var gifts = await query
                .OrderBy(g => g.PointsCost)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginated = new PaginatedList<GiftResponse>(
                gifts.Select(MapToResponse).ToList(),
                pageNumber, pageSize, totalCount);

            return _response.Success(paginated, "Available gifts retrieved successfully");
        }

        public async Task<Response<PurchaseGiftResponse>> PurchaseGiftAsync(string childId, string giftId)
        {
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<PurchaseGiftResponse>("Child not found");

            var gift = await _context.Gifts
                .FirstOrDefaultAsync(g => g.Id == giftId && !g.IsDeleted && g.Status == GiftStatus.Active);

            if (gift == null)
                return _response.NotFound<PurchaseGiftResponse>("Gift not found or not available");

            // تحقق إن ما اشتراهاش قبل كده
            var alreadyPurchased = await _context.ChildGifts
                .AnyAsync(cg => cg.ChildId == childId && cg.GiftId == giftId);

            if (alreadyPurchased)
                return _response.Conflict<PurchaseGiftResponse>("You already own this gift");

            // تحقق إن عنده نقاط كفاية
            if (child.TotalPoints < gift.PointsCost)
                return _response.BadRequest<PurchaseGiftResponse>(
                    $"Not enough points. You need {gift.PointsCost} points but have {child.TotalPoints}");

            // اشتري الـ gift
            child.TotalPoints -= gift.PointsCost;
            // HighestPoints مش بيتقل أبداً للـ Ranking

            var childGift = new ChildGift
            {
                ChildId = childId,
                GiftId = giftId,
                PointsSpent = gift.PointsCost,
                PurchasedAt = DateTime.UtcNow,
                CreatedBy = childId
            };

            // سجل الـ transaction
            _context.PointsTransactions.Add(new PointsTransaction
            {
                ChildId = childId,
                Points = -gift.PointsCost,  // سالب لأنه إنفاق
                Reason = $"Purchased Gift: {gift.NameEn}",
                SourceType = PointsSourceType.GiftPurchase,
                SourceEntityId = childGift.Id
            });

            _context.ChildGifts.Add(childGift);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(child.ParentId))
                await _notificationService.SendAsync(
                    child.ParentId,
                    NotificationType.GiftPurchased,
                    "Gift Purchased",
                    $"{child.Name} used {gift.PointsCost} points to get \"{gift.NameEn}\".",
                    gift.Id);

            return _response.Success(new PurchaseGiftResponse
            {
                ChildGiftId = childGift.Id,
                GiftName = gift.NameEn,
                GiftImageUrl = gift.ImageUrl,
                PointsSpent = gift.PointsCost,
                RemainingPoints = child.TotalPoints,
                HighestPoints = child.HighestPoints,
                PurchasedAt = childGift.PurchasedAt
            }, "Gift purchased successfully!");
        }

        public async Task<Response<List<GiftResponse>>> GetMyGiftsAsync(string childId)
        {
            var childExists = await _context.Childrens.AnyAsync(c => c.Id == childId && !c.IsDeleted);
            if (!childExists)
                return _response.NotFound<List<GiftResponse>>("Child not found");

            var gifts = await _context.ChildGifts
                .Include(cg => cg.Gift)
                .Where(cg => cg.ChildId == childId && !cg.IsDeleted)
                .OrderByDescending(cg => cg.PurchasedAt)
                .Select(cg => MapToResponse(cg.Gift))
                .ToListAsync();

            return _response.Success(gifts, "My gifts retrieved successfully");
        }

        public async Task<Response<List<ChildGiftResponse>>> GetChildGiftsForParentAsync(string parentAppUserId)
        {
            var activeChildId = await _context.Parents
                .Where(p => p.Id == parentAppUserId)
                .Select(p => p.ActiveChildId)
                .FirstOrDefaultAsync();

            if (activeChildId is null)
                return _response.NotFound<List<ChildGiftResponse>>("No active child linked to this parent.");

            var gifts = await _context.ChildGifts
                .Include(cg => cg.Gift)
                .Where(cg => cg.ChildId == activeChildId && !cg.IsDeleted)
                .OrderByDescending(cg => cg.PurchasedAt)
                .Select(cg => new ChildGiftResponse
                {
                    ChildGiftId = cg.Id,
                    GiftId = cg.GiftId,
                    NameEn = cg.Gift.NameEn,
                    NameAr = cg.Gift.NameAr,
                    DescriptionEn = cg.Gift.DescriptionEn,
                    DescriptionAr = cg.Gift.DescriptionAr,
                    ImageUrl = cg.Gift.ImageUrl,
                    PointsCost = cg.Gift.PointsCost,
                    Type = cg.Gift.Type,
                    PointsSpent = cg.PointsSpent,
                    PurchasedAt = cg.PurchasedAt,
                })
                .ToListAsync();

            return _response.Success(gifts, "Child gifts retrieved successfully.");
        }

        private static GiftResponse MapToResponse(Gift gift) => new()
        {
            Id = gift.Id,
            NameEn = gift.NameEn,
            NameAr = gift.NameAr,
            DescriptionEn = gift.DescriptionEn,
            DescriptionAr = gift.DescriptionAr,
            ImageUrl = gift.ImageUrl,
            PointsCost = gift.PointsCost,
            Type = gift.Type,
            Status = gift.Status,
            CreatedAt = gift.CreatedAt
        };
    }

}