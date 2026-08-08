using GoKidAPI.DTO.InstitutionAdmin.Supervisor.Requests;
using GoKidAPI.DTO.InstitutionAdmin.Supervisor.Responses;
using GoKidAPI.DTO.Supervisor.Requests;
using GoKidAPI.DTO.Supervisor.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Institution.Interface
{
    public interface IInstitutionSupervisorService
    {
        Task<Response<PaginatedList<SupervisorListItemResponse>>> GetAllSupervisorsAsync(
        string currentUserId,
        GetSupervisorsFilters filters);

        /// <summary>
        /// Creates a new Supervisor account under the current InstitutionAdmin's Institution.
        /// Sends credentials email to the new Supervisor.
        /// </summary>
        Task<Response<SupervisorCreatedResponse>> CreateSupervisorAsync(
            string currentAdminUserId,
            CreateSupervisorRequest request);

        Task<Response<SupervisorUpdatedResponse>> UpdateSupervisorAsync(
    string currentAdminUserId,
    string supervisorId,
    UpdateSupervisorRequest request);

        Task<Response<object>> DeleteSupervisorAsync(
            string currentAdminUserId,
            string supervisorId);

    }
}
