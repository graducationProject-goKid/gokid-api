using GoKidAPI.DTO.Institution.Requests;
using GoKidAPI.DTO.Institution.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Institution.Interface
{
    public interface IInstitutionService
    {
        Task<Response<InstitutionDetailsResponse>> CreateInstitutionAsync(CreateInstitutionRequest request, string platformAdminId);
        Task<Response<InstitutionDetailsResponse>> UpdateInstitutionAsync(string institutionId, UpdateInstitutionRequest request, string platformAdminId);
        Task<Response<object>> DeleteInstitutionAsync(string institutionId, string platformAdminId);
        Task<Response<PaginatedList<InstitutionListItemResponse>>> GetAllInstitutionsAsync(int pageNumber, int pageSize, string? search);
        Task<Response<InstitutionDetailsResponse>> GetInstitutionDetailsAsync(string institutionId);
        Task<Response<SupervisorProfileResponse>> GetSupervisorProfileAsync(string institutionId, string supervisorId);
    }
}
