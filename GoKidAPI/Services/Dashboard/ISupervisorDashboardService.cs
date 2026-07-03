using GoKidAPI.DTO.Dashboard;

namespace GoKidAPI.Services.Dashboard
{
    public interface ISupervisorDashboardService
    {
        /// <summary>Returns the supervisor dashboard for the given user. Result is cached per supervisor for 3 minutes.</summary>
        Task<SupervisorDashboardResponse?> GetDashboardAsync(string supervisorUserId);

        /// <summary>Invalidates the cached dashboard for a specific supervisor.</summary>
        void InvalidateCache(string supervisorId);
    }
}
