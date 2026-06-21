using Azure;

using GoKidAPI.DTO.Adventures.Requests;
using GoKidAPI.DTO.Adventures.Responses;
using GoKidAPI.DTO.AdventureStory;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Adventure
{
    public interface IAdventureService
    {
        Task<Shared.Response<CreateAdventureResponse>> CreateAdventureAsync(string institutionAdminId, CreateAdventureRequest request);

        Task<Shared.Response<PaginatedList<AdventureListItemResponse>>> GetAllAdventuresAsync(string institutionAdminId, GetAdventuresFilters filters);

        Task<Shared.Response<AdventureDetailsResponse>> GetAdventureDetailsAsync(string institutionAdminId, string adventureId);

        Task<Shared.Response<AdventureDetailsResponse>> UpdateAdventureAsync(string institutionAdminId, string adventureId, UpdateAdventureRequest request);

        Task<Shared.Response<object>> DeleteAdventureAsync(string institutionAdminId, string adventureId);

        Task<Shared.Response<object>> ChangeAdventureStatusAsync(string institutionAdminId, string adventureId, AdventureStatus status);

        Task<Shared.Response<AssignAdventureToClassResponse>> AssignAdventureToClassAsync(
            string institutionAdminId,
            AssignAdventureToClassRequest request);
        Task<Shared.Response<List<WeeklyAdventureClassResponse>>> GetClassesByWeeklyAdventureAsync(
    string userId,
    string weeklyAdventureId);
        Task<Shared.Response<GenerateStoryResponse>> GenerateStoryAsync(
    string institutionAdminId,
    string adventureId);

         Task<Shared.Response<AdventureStoryDetailsResponse>> GetAdventureStoryDetailsAsync(string adventureId);
    }
}
