using GoKidAPI.DTO.Gifts.Requests;
using GoKidAPI.DTO.Gifts.Responses;
using GoKidAPI.Enums.Gifts;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Gifts
{
    public interface IGiftService
    {
        // Platform Admin
        Task<Response<GiftResponse>> CreateGiftAsync(CreateGiftRequest request, string adminId);
        Task<Response<GiftResponse>> UpdateGiftAsync(string giftId, UpdateGiftRequest request, string adminId);
        Task<Response<object>> DeleteGiftAsync(string giftId, string adminId);
        Task<Response<object>> ChangeGiftStatusAsync(string giftId, GiftStatus status, string adminId);
        Task<Response<PaginatedList<GiftResponse>>> GetAllGiftsAsync(int pageNumber, int pageSize, GiftType? type, GiftStatus? status);

        // Child
        Task<Response<PaginatedList<GiftResponse>>> GetAvailableGiftsAsync(string childId, int pageNumber, int pageSize);
        Task<Response<PurchaseGiftResponse>> PurchaseGiftAsync(string childId, string giftId);
        Task<Response<List<GiftResponse>>> GetMyGiftsAsync(string childId);
    }
}
