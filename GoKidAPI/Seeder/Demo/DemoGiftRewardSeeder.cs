using GoKidAPI.Data;
using GoKidAPI.Entity.Gifts;
using GoKidAPI.Enums.Gifts;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    // Seeds the platform-wide Gift catalog and parent-defined Reward goals.
    // ChildGift purchases and Reward.Given flips happen later in DemoPointsRollupSeeder,
    // once each child's final TotalPoints is known.
    public static class DemoGiftRewardSeeder
    {
        private static readonly (string En, string Ar, GiftType Type, int Cost)[] Catalog =
        {
            ("Superhero Sticker Pack", "مجموعة ملصقات الأبطال الخارقين", GiftType.Badge, 60),
            ("Star Explorer Badge", "شارة المستكشف النجمي", GiftType.Badge, 90),
            ("Golden Star Badge", "شارة النجمة الذهبية", GiftType.Badge, 150),
            ("Kindness Champion Badge", "شارة بطل اللطف", GiftType.Badge, 120),
            ("Reading Master Badge", "شارة سيد القراءة", GiftType.Badge, 180),
            ("Space Ranger Character", "شخصية رائد الفضاء", GiftType.Character, 300),
            ("Jungle Explorer Character", "شخصية مستكشف الأدغال", GiftType.Character, 320),
            ("Ocean Diver Character", "شخصية غواص المحيط", GiftType.Character, 350),
            ("Dino Hunter Character", "شخصية صائد الديناصورات", GiftType.Character, 340),
            ("Robot Buddy Character", "شخصية الروبوت الصديق", GiftType.Character, 400),
            ("Pirate Captain Character", "شخصية قبطان القراصنة", GiftType.Character, 380),
            ("Magic Wizard Character", "شخصية الساحر", GiftType.Character, 420),
            ("Superhero Cape Avatar", "أفاتار عباءة البطل الخارق", GiftType.Character, 500),
            ("Extra Screen Time (30 min)", "وقت إضافي للشاشة (30 دقيقة)", GiftType.Other, 250),
            ("Choose Weekend Movie", "اختيار فيلم نهاية الأسبوع", GiftType.Other, 220),
            ("Stay Up 30 Minutes Later", "السهر 30 دقيقة إضافية", GiftType.Other, 260),
            ("Favorite Meal Night", "ليلة الوجبة المفضلة", GiftType.Other, 300),
            ("Ice Cream Trip", "رحلة آيس كريم", GiftType.Other, 350),
            ("New Coloring Book", "كتاب تلوين جديد", GiftType.Other, 400),
            ("Toy Store Voucher", "قسيمة متجر ألعاب", GiftType.Other, 1200),
            ("Family Game Night", "ليلة الألعاب العائلية", GiftType.Other, 600),
            ("Amusement Park Trip", "رحلة إلى مدينة الألعاب", GiftType.Other, 2200),
            ("New Bicycle", "دراجة جديدة", GiftType.Other, 3000),
            ("Legendary Adventurer Badge", "شارة المغامر الأسطوري", GiftType.Badge, 2500),
        };

        private static readonly (string En, string Ar)[] RewardGoals =
        {
            ("Complete a Perfect Week", "إتمام أسبوع مثالي"),
            ("Master a New Skill", "إتقان مهارة جديدة"),
            ("Help Around the House All Month", "المساعدة في المنزل طوال الشهر"),
            ("Finish an Entire Adventure", "إنهاء مغامرة كاملة"),
            ("Read 10 Books", "قراءة 10 كتب"),
            ("Keep Room Clean for 2 Weeks", "الحفاظ على نظافة الغرفة لأسبوعين"),
            ("Excellent Report Card", "شهادة تفوق ممتازة"),
            ("Be Kind Every Day for a Month", "التحلي باللطف كل يوم لمدة شهر"),
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            if (!await context.Gifts.IgnoreQueryFilters().AnyAsync())
            {
                var gifts = Catalog.Select(g => new Gift
                {
                    NameEn = g.En,
                    NameAr = g.Ar,
                    DescriptionEn = $"Redeem your points for {g.En}.",
                    DescriptionAr = $"استبدل نقاطك للحصول على {g.Ar}.",
                    ImageUrl = DemoDataContext.Placeholder(g.En, "200x200"),
                    PointsCost = g.Cost,
                    Type = g.Type,
                    Status = GiftStatus.Active,
                    CreatedBy = "DemoSeeder"
                }).ToList();

                await context.Gifts.AddRangeAsync(gifts);
                await context.SaveChangesAsync();
            }

            if (await context.Rewards.AnyAsync())
                return;

            var children = await context.Childrens
                .Where(c => c.InstitutionId != null && c.Institution!.Code.StartsWith("SCH-DEMO") && c.ParentId != null)
                .OrderBy(c => Guid.NewGuid())
                .Take(40)
                .ToListAsync();

            var rewards = new List<Reward>();
            foreach (var child in children)
            {
                var goal = DemoDataContext.PickRandom(RewardGoals);
                rewards.Add(new Reward
                {
                    NameEn = goal.En,
                    NameAr = goal.Ar,
                    DescriptionEn = $"A special reward for {child.Name} once the goal is reached.",
                    DescriptionAr = $"مكافأة خاصة لـ {child.Name} عند تحقيق الهدف.",
                    TargetPoints = DemoDataContext.Rng.Next(150, 2500),
                    ParentId = child.ParentId!,
                    ChildId = child.Id,
                    Status = RewardStatus.Pending,
                    CreatedAt = child.CreatedAt.AddDays(DemoDataContext.Rng.Next(1, 20)),
                    CreatedBy = "DemoSeeder"
                });
            }

            await context.Rewards.AddRangeAsync(rewards);
            await context.SaveChangesAsync();
        }
    }
}
