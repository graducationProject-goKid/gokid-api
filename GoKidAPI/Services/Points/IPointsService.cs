using GoKidAPI.Enums;

namespace GoKidAPI.Services.Points
{
    public interface IPointsService
    {
        Task AwardPointsAsync(
            string childId,
            int points,
            PointsSourceType sourceType,
            string sourceEntityId,
            string reason);
    }
}
