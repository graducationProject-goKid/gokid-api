using GoKidAPI.DTO.Classes.Requests;
using GoKidAPI.DTO.Classes.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Classes
{
    public interface IClassService
    {
        Task<Response<ClassDetailsResponse>> CreateClassAsync(string institutionAdminUserId, CreateClassRequest request);

        Task<Response<ClassDetailsResponse>> UpdateClassAsync(string institutionAdminUserId, string classId, UpdateClassRequest request);

        Task<Shared.Response<string>> DeleteClassAsync(string institutionAdminUserId, string classId);

        Task<Response<ClassDetailsResponse>> GetClassByIdAsync(string institutionAdminUserId, string classId);

        Task<Response<PaginatedList<ClassListItemResponse>>> GetAllClassesAsync(string institutionAdminUserId, GetClassesFilters filters);
        /// <summary>
        /// Assigns an existing Supervisor to a Class (only if both belong to the same Institution)
        /// </summary>
        Task<Response<SupervisorAssignmentResponse>> AssignSupervisorToClassAsync(
            string currentAdminUserId,
            string classId,
            AssignSupervisorToClassRequest request);

        /// <summary>
        /// Removes a Supervisor from a Class
        /// </summary>
        Task<Response<SupervisorAssignmentResponse>> RemoveSupervisorFromClassAsync(
            string currentAdminUserId,
            string classId,
            string supervisorId);

        Task<Response<EnrollChildResponse>> EnrollChildToInstitutionAsync(
    string adminUserId,
    string registrationCode);

        Task<Response<object>> RemoveChildFromInstitutionAsync(
            string adminUserId,
            string childId);

        Task<Response<EnrollChildResponse>> EnrollChildToClassAsync(
    string adminUserId,
    string classId,
    EnrollChildToClassRequest request);

        Task<Response<object>> RemoveChildFromClassAsync(
            string adminUserId,
            string classId,
            string childId);

        Task<Response<PaginatedList<InstitutionChildResponse>>> GetInstitutionChildrenAsync(
    string userId,
    string userRole,
    int pageNumber,
    int pageSize,
    string? search = null,
    string? classId = null);
        }
    
}
