using GoKidAPI.Data;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums.Tasks;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    // Populates TaskTemplateBase rows under the subcategories created by CategoriesSeeder.
    // Must run before every other Demo* seeder (ChildTasks/AdventureTasks depend on templates).
    public static class DemoTaskTemplateSeeder
    {
        private static readonly TaskTemplateType[] TypeRotation =
        {
            TaskTemplateType.InstantReward,
            TaskTemplateType.EvidenceSubmission,
            TaskTemplateType.TextQuestion,
            TaskTemplateType.VoiceQuestion
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            if (context.TaskTemplates.IgnoreQueryFilters().Any())
                return;

            var subCategories = await context.SubCategories
                .Include(sc => sc.Category)
                .ToListAsync();

            var templates = new List<TaskTemplateBase>();

            for (int i = 0; i < subCategories.Count; i++)
            {
                var sub = subCategories[i];

                // Two templates per subcategory, rotating through all 4 template types
                // so every type is well represented across the catalog.
                var typeA = TypeRotation[i % TypeRotation.Length];
                var typeB = TypeRotation[(i + 2) % TypeRotation.Length];

                templates.Add(BuildTemplate(sub, typeA));
                templates.Add(BuildTemplate(sub, typeB));
            }

            await context.TaskTemplates.AddRangeAsync(templates);
            await context.SaveChangesAsync();
        }

        private static TaskTemplateBase BuildTemplate(TaskSubCategory sub, TaskTemplateType type)
        {
            var difficulty = DemoDataContext.PickRandom(new[] { DifficultyLevel.Easy, DifficultyLevel.Easy, DifficultyLevel.Medium, DifficultyLevel.Hard });
            var basePoints = difficulty switch
            {
                DifficultyLevel.Easy => DemoDataContext.Rng.Next(10, 16),
                DifficultyLevel.Medium => DemoDataContext.Rng.Next(18, 26),
                _ => DemoDataContext.Rng.Next(28, 41)
            };

            // Harder/more advanced task types skew toward an older recommended age band;
            // simpler instant-reward tasks stay accessible to the youngest children.
            var ageFrom = difficulty switch
            {
                DifficultyLevel.Easy => DemoDataContext.Rng.Next(4, 7),
                DifficultyLevel.Medium => DemoDataContext.Rng.Next(6, 9),
                _ => DemoDataContext.Rng.Next(9, 12)
            };
            var ageTo = Math.Min(18, ageFrom + DemoDataContext.Rng.Next(2, 5));

            var template = new TaskTemplateBase
            {
                SubCategoryId = sub.Id,
                Difficulty = difficulty,
                TemplateType = type,
                BasePoints = basePoints,
                RecommendedAgeFrom = ageFrom,
                RecommendedAgeTo = ageTo,
                TaskImageUrl = DemoDataContext.Placeholder(sub.NameEn),
                IconUrl = sub.IconUrl,
                DescriptionAr = $"مهمة يومية في مجال {sub.NameAr} تساعد طفلك على التطور والتعلم بطريقة ممتعة.",
                DescriptionEn = $"A daily {sub.NameEn} task that helps your child grow and learn in a fun way."
            };

            switch (type)
            {
                case TaskTemplateType.InstantReward:
                    template.TitleEn = $"Quick Win: {sub.NameEn}";
                    template.TitleAr = $"إنجاز سريع: {sub.NameAr}";
                    template.InstructionsText = $"Do a small {sub.NameEn} activity, then tap Done to claim your points instantly.";
                    break;

                case TaskTemplateType.EvidenceSubmission:
                    template.TitleEn = $"{sub.NameEn} Challenge";
                    template.TitleAr = $"تحدي {sub.NameAr}";
                    template.InstructionsText = $"Finish your {sub.NameEn} activity and upload a photo or short video as proof for your parent to review.";
                    template.EvidenceType = DemoDataContext.Rng.NextDouble() < 0.8 ? EvidenceType.Image : EvidenceType.Video;
                    template.ReviewBy = ReviewAuthority.Parent;
                    break;

                case TaskTemplateType.TextQuestion:
                    template.TitleEn = $"{sub.NameEn} Quiz";
                    template.TitleAr = $"اختبار {sub.NameAr}";
                    template.QuestionText = $"Write one thing you practiced or learned today in {sub.NameEn}.";
                    template.ExpectedCorrectAnswer = sub.NameEn;
                    template.CaseSensitive = false;
                    break;

                case TaskTemplateType.VoiceQuestion:
                    template.TitleEn = $"{sub.NameEn} Voice Challenge";
                    template.TitleAr = $"تحدي {sub.NameAr} الصوتي";
                    template.VoiceQuestionText = $"Say out loud one thing you did today for {sub.NameEn}.";
                    template.VoiceExpectedCorrectAnswer = sub.NameEn;
                    template.VoicePrompt = $"Tell me about your {sub.NameEn} activity today!";
                    template.MaxVoiceAttempts = 3;
                    template.MaxVoiceDurationSeconds = 12;
                    break;
            }

            return template;
        }
    }
}
