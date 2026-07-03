using GoKidAPI.Data;
using GoKidAPI.Entity;
using GoKidAPI.Enums;
using GoKidAPI.Services.LevelProgression;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Points
{
    public class PointsService : IPointsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PointsService> _logger;
        private readonly ILevelProgressionService _levelProgression;

        public PointsService(
            AppDbContext context,
            ILogger<PointsService> logger,
            ILevelProgressionService levelProgression)
        {
            _context = context;
            _logger = logger;
            _levelProgression = levelProgression;
        }

        public async Task AwardPointsAsync(
            string childId,
            int points,
            PointsSourceType sourceType,
            string sourceEntityId,
            string reason,
            string updatedBy)
        {
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
            {
                _logger.LogWarning("AwardPoints: Child {ChildId} not found", childId);
                return;
            }

            child.TotalPoints += points;

            if (child.TotalPoints > child.HighestPoints)
                child.HighestPoints = child.TotalPoints;

            _context.PointsTransactions.Add(new PointsTransaction
            {
                ChildId = childId,
                Points = points,
                Reason = reason,
                SourceType = sourceType,
                SourceEntityId = sourceEntityId
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Points awarded: Child={ChildId}, Points={Points}, Total={Total}, Highest={Highest}",
                childId, points, child.TotalPoints, child.HighestPoints);

            await _levelProgression.CheckAndUpdateLevelAsync(childId, updatedBy);
        }
    }
}