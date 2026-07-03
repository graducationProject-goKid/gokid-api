using GoKidAPI.DTO.Dashboard;

namespace GoKidAPI.Services.Dashboard
{
    public interface IInstitutionDashboardService
    {
        /// <summary>Returns the institution dashboard for the given admin. Result is cached per institution for 5 minutes.</summary>
        Task<InstitutionDashboardResponse?> GetDashboardAsync(string adminUserId);

        /// <summary>Invalidates the cached dashboard for a specific institution.</summary>
        void InvalidateCache(string institutionId);
    }
}
