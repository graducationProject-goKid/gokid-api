using GoKidAPI.DTO.Statistics;
using GoKidAPI.Enums;

namespace GoKidAPI.Services.Statistics
{
    public interface IStatisticsService
    {
        Task<Shared.Response<ParentStatisticsResponse>> GetParentStatisticsAsync(
    string userId,
    string userRole,
    StatisticsPeriod period = StatisticsPeriod.ThisWeek);
    }
}
