using GoKidAPI.DTO.Dashboard;

namespace GoKidAPI.Services.Dashboard
{
    public interface IPlatformDashboardService
    {
        /// <summary>Returns the full platform statistics dashboard. Result is cached for 5 minutes.</summary>
        Task<PlatformDashboardResponse> GetDashboardAsync();

        /// <summary>Invalidates the cached dashboard so the next call recomputes it.</summary>
        void InvalidateCache();
    }
}
