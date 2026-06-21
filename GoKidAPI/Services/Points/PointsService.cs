using GoKidAPI.Data;
using GoKidAPI.Entity;
using GoKidAPI.Enums;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Points
{
    public class PointsService : IPointsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PointsService> _logger;

        public PointsService(AppDbContext context, ILogger<PointsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AwardPointsAsync(
            string childId,
            int points,
            PointsSourceType sourceType,
            string sourceEntityId,
            string reason)
        {
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
            {
                _logger.LogWarning("AwardPoints: Child {ChildId} not found", childId);
                return;
            }

            // زوّد النقاط الحالية
            child.TotalPoints += points;

            // حدّث HighestPoints لو النقاط الجديدة أعلى
            if (child.TotalPoints > child.HighestPoints)
                child.HighestPoints = child.TotalPoints;

            // سجّل الـ Transaction
            _context.PointsTransactions.Add(new PointsTransaction
            {
                ChildId = childId,
                Points = points,
                Reason = reason,
                SourceType = sourceType,
                SourceEntityId = sourceEntityId
            });

            _logger.LogInformation(
                "Points awarded: Child={ChildId}, Points={Points}, Total={Total}, Highest={Highest}",
                childId, points, child.TotalPoints, child.HighestPoints);
        }
    }
}