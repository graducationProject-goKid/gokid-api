using GoKidAPI.Data;
using GoKidAPI.Entity;
using GoKidAPI.Entity.Gifts;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Gifts;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    // Finalizes each demo child's TotalPoints/HighestPoints/Level from the PointsTransaction
    // rows created by DemoDailyTaskSeeder + DemoWeeklyAdventureSeeder (so points, level and
    // history always stay consistent), then simulates realistic gift purchases and reward
    // hand-outs against those final point balances.
    public static class DemoPointsRollupSeeder
    {
        private static double GiftBuyingChance(DemoDataContext.ActivityTier tier) => tier switch
        {
            DemoDataContext.ActivityTier.Beginner => 0.15,
            DemoDataContext.ActivityTier.Casual => 0.30,
            DemoDataContext.ActivityTier.Active => 0.55,
            DemoDataContext.ActivityTier.StarPerformer => 0.80,
            DemoDataContext.ActivityTier.Veteran => 0.40,
            _ => 0.3
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.ChildGifts.IgnoreQueryFilters().AnyAsync(cg => cg.CreatedBy == "DemoSeeder"))
                return;

            var now = DateTime.UtcNow;
            var rng = DemoDataContext.Rng;

            var children = await context.Childrens
                .Where(c => c.InstitutionId != null && c.Institution!.Code.StartsWith("SCH-DEMO"))
                .ToListAsync();
            if (children.Count == 0) return;

            var pointSums = await context.PointsTransactions.IgnoreQueryFilters()
                .Where(pt => pt.CreatedBy == "DemoSeeder")
                .GroupBy(pt => pt.ChildId)
                .Select(g => new { ChildId = g.Key, Total = g.Sum(x => x.Points) })
                .ToDictionaryAsync(x => x.ChildId, x => x.Total);

            var levels = await context.Levels.IgnoreQueryFilters()
                .Where(l => !l.IsDeleted)
                .OrderByDescending(l => l.MinPoints)
                .ToListAsync();

            foreach (var child in children)
            {
                var total = pointSums.TryGetValue(child.Id, out var sum) ? Math.Max(sum, 0) : 0;
                child.TotalPoints = total;
                child.HighestPoints = total;
                child.LevelId = levels.FirstOrDefault(l => total >= l.MinPoints)?.Id ?? levels.LastOrDefault()?.Id;
            }
            await context.SaveChangesAsync();

            // ---- Gift purchases ----
            var gifts = await context.Gifts.IgnoreQueryFilters()
                .Where(g => !g.IsDeleted && g.Status == GiftStatus.Active)
                .OrderBy(g => g.PointsCost)
                .ToListAsync();

            int processed = 0;
            foreach (var child in children)
            {
                processed++;
                if (child.TotalPoints < gifts.Min(g => g.PointsCost)) continue;

                var tier = DemoDataContext.TierForChild(child.Id);
                if (rng.NextDouble() > GiftBuyingChance(tier)) continue;

                var remaining = child.TotalPoints;
                var purchaseCount = rng.Next(1, 4);
                var purchased = new HashSet<string>();
                var affordable = gifts.Where(g => g.PointsCost <= remaining).OrderBy(_ => rng.Next()).ToList();

                foreach (var gift in affordable)
                {
                    if (purchased.Count >= purchaseCount) break;
                    if (gift.PointsCost > remaining) continue;

                    var purchasedAt = DemoDataContext.RandomDateWeighted(child.CreatedAt, now, 0.9);

                    context.ChildGifts.Add(new ChildGift
                    {
                        ChildId = child.Id,
                        GiftId = gift.Id,
                        PointsSpent = gift.PointsCost,
                        PurchasedAt = purchasedAt,
                        CreatedAt = purchasedAt,
                        CreatedBy = "DemoSeeder"
                    });

                    context.PointsTransactions.Add(new PointsTransaction
                    {
                        ChildId = child.Id,
                        Points = -gift.PointsCost,
                        Reason = $"Redeemed '{gift.NameEn}'",
                        SourceType = PointsSourceType.GiftPurchase,
                        CreatedAt = purchasedAt,
                        CreatedBy = "DemoSeeder"
                    });

                    remaining -= gift.PointsCost;
                    purchased.Add(gift.Id);
                }

                child.TotalPoints = remaining; // HighestPoints intentionally left untouched (ranking rule)

                if (processed % 25 == 0)
                    await context.SaveChangesAsync();
            }
            await context.SaveChangesAsync();

            // ---- Reward hand-outs ----
            var rewards = await context.Rewards
                .Where(r => r.CreatedBy == "DemoSeeder" && r.Status == RewardStatus.Pending)
                .ToListAsync();

            var childById = children.ToDictionary(c => c.Id);
            foreach (var reward in rewards)
            {
                if (!childById.TryGetValue(reward.ChildId, out var child)) continue;
                if (child.TotalPoints < reward.TargetPoints) continue;
                if (rng.NextDouble() > 0.7) continue;

                reward.Status = RewardStatus.Given;
                reward.GivenAt = DemoDataContext.RandomDateWeighted(reward.CreatedAt, now, 0.9);
            }
            await context.SaveChangesAsync();
        }
    }
}
