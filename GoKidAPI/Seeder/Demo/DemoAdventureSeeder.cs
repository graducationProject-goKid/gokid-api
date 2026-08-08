using GoKidAPI.Data;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Enums.Adventures;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    // Seeds the reusable Adventure "templates" (theme + 7-day story arc + linked AdventureTask
    // day-slots). WeeklyAdventure (the per-class assignment) is handled by DemoWeeklyAdventureSeeder.
    public static class DemoAdventureSeeder
    {
        private record Theme(
            string TitleEn, string TitleAr,
            string SettingEn, string SettingAr,
            string HeroEn, string HeroAr,
            string TreasureEn, string TreasureAr);

        private static readonly Theme[] Themes =
        {
            new("Space Mission", "مهمة الفضاء", "the Galaxy of Wonders", "مجرة العجائب", "Star Guardian", "حارس النجوم", "the Crystal of Orion", "بلورة الجبار"),
            new("Jungle Explorer", "مستكشف الأدغال", "the Whispering Jungle", "الأدغال الهامسة", "Jungle Guardian", "حارس الأدغال", "the Golden Vine", "الكرمة الذهبية"),
            new("Ocean Discovery", "اكتشاف المحيط", "the Deep Blue Sea", "أعماق البحر الأزرق", "Ocean Guardian", "حارس المحيط", "the Pearl of Atlantis", "لؤلؤة أتلانتس"),
            new("Dinosaur Island", "جزيرة الديناصورات", "the Lost Dino Island", "جزيرة الديناصورات المفقودة", "Dino Ranger", "حارس الديناصورات", "the Ancient Egg", "البيضة القديمة"),
            new("Magic Kingdom", "المملكة السحرية", "the Enchanted Kingdom", "المملكة المسحورة", "Royal Wizard", "الساحر الملكي", "the Wand of Light", "عصا النور"),
            new("Pirate Treasure Hunt", "بحث كنز القراصنة", "the Seven Seas", "البحار السبعة", "Captain Adventurer", "القبطان المغامر", "the Lost Treasure", "الكنز المفقود"),
            new("Superhero Academy", "أكاديمية الأبطال الخارقين", "Superhero Academy", "أكاديمية الأبطال الخارقين", "Rising Hero", "البطل الصاعد", "the Hero's Medal", "ميدالية البطل"),
            new("Time Traveler", "المسافر عبر الزمن", "the Corridors of Time", "ممرات الزمن", "Time Guardian", "حارس الزمن", "the Golden Hourglass", "الساعة الرملية الذهبية"),
            new("Robot Factory", "مصنع الروبوتات", "the Robot Factory", "مصنع الروبوتات", "Chief Engineer", "كبير المهندسين", "the Master Circuit", "الدائرة الرئيسية"),
            new("Arctic Expedition", "بعثة القطب الشمالي", "the Frozen North", "الشمال المتجمد", "Arctic Guardian", "حارس القطب", "the Aurora Gem", "جوهرة الشفق"),
            new("Desert Caravan", "قافلة الصحراء", "the Golden Dunes", "الكثبان الذهبية", "Desert Wanderer", "متجول الصحراء", "the Oasis Stone", "حجر الواحة"),
            new("Enchanted Forest", "الغابة المسحورة", "the Enchanted Forest", "الغابة المسحورة", "Forest Guardian", "حارس الغابة", "the Heart of the Forest", "قلب الغابة"),
        };

        private static readonly (string TitleEn, string TitleAr, string TextEnFormat, string TextArFormat)[] DayBeats =
        {
            ("Day 1: The Journey Begins", "اليوم 1: بداية الرحلة",
                "Welcome, young {hero}! Your adventure into {setting} begins now. Complete today's mission to take your first brave step.",
                "أهلاً بك أيها {hero} الصغير! تبدأ الآن مغامرتك في {setting}. أكمل مهمة اليوم لتخطو خطوتك الأولى الجريئة."),
            ("Day 2: A New Friend", "اليوم 2: صديق جديد",
                "You've made a new friend along the way! Together you'll need courage and focus to keep exploring {setting}.",
                "لقد كسبت صديقاً جديداً في طريقك! ستحتاجان معاً إلى الشجاعة والتركيز لمواصلة استكشاف {setting}."),
            ("Day 3: The First Challenge", "اليوم 3: التحدي الأول",
                "A tricky challenge blocks your path through {setting}. Only a true {hero} can figure out how to get past it!",
                "يقف تحدٍ صعب في طريقك عبر {setting}. فقط {hero} حقيقي يستطيع إيجاد طريقة لتجاوزه!"),
            ("Day 4: Halfway There", "اليوم 4: منتصف الطريق",
                "You're halfway through your journey! The signs of {treasure} are getting closer. Keep your momentum going.",
                "لقد قطعت نصف رحلتك! علامات {treasure} أصبحت أقرب. حافظ على زخمك."),
            ("Day 5: The Storm", "اليوم 5: العاصفة",
                "A sudden storm shakes {setting}! Stay strong and complete today's mission to protect what you've built so far.",
                "عاصفة مفاجئة تهز {setting}! كن قوياً وأكمل مهمة اليوم لتحمي كل ما بنيته حتى الآن."),
            ("Day 6: The Final Clue", "اليوم 6: الدليل الأخير",
                "You've found the final clue leading straight to {treasure}! One more push and victory will be yours.",
                "لقد وجدت الدليل الأخير الذي يقودك مباشرة إلى {treasure}! دفعة أخيرة وسيكون النصر من نصيبك."),
            ("Day 7: The Grand Victory", "اليوم 7: الانتصار الكبير",
                "This is it, {hero}! Complete your final mission to claim {treasure} and finish your journey through {setting} as a true legend.",
                "ها قد وصلت يا {hero}! أكمل مهمتك الأخيرة لتحصل على {treasure} وتنهي رحلتك عبر {setting} كأسطورة حقيقية."),
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Adventures.IgnoreQueryFilters().AnyAsync(a => a.CreatedBy == "DemoSeeder"))
                return;

            var institutions = await context.Institutions
                .Where(i => i.Code.StartsWith("SCH-DEMO"))
                .OrderBy(i => i.Code)
                .ToListAsync();

            var templates = await context.TaskTemplates.IgnoreQueryFilters().Where(t => !t.IsDeleted).ToListAsync();
            if (institutions.Count == 0 || templates.Count < 7) return;

            var now = DateTime.UtcNow;
            var rng = DemoDataContext.Rng;

            for (int i = 0; i < Themes.Length; i++)
            {
                var theme = Themes[i];
                var institution = institutions[i % institutions.Count];
                var createdAt = now.AddMonths(-DemoDataContext.Rng.Next(2, 4));

                var adventure = new Adventure
                {
                    TitleEn = theme.TitleEn,
                    TitleAr = theme.TitleAr,
                    DescriptionEn = $"An epic journey through {theme.SettingEn}, full of mystery, friendship and daily missions.",
                    DescriptionAr = $"رحلة أسطورية عبر {theme.SettingAr}، مليئة بالغموض والصداقة والمهام اليومية.",
                    GoalEn = $"Help your child become the {theme.HeroEn} of {theme.SettingEn} by completing each daily mission.",
                    GoalAr = $"ساعد طفلك ليصبح {theme.HeroAr} في {theme.SettingAr} من خلال إتمام كل مهمة يومية.",
                    BannerImageUrl = DemoDataContext.Placeholder(theme.TitleEn, "600x300"),
                    IntroTitle = $"Welcome, young {theme.HeroEn}!",
                    IntroStory = $"Welcome, young {theme.HeroEn}!\n\nToday you're entering {theme.SettingEn}, where hidden wonders are waiting for brave adventurers like you.\nYour mission: complete each daily challenge to unlock the path forward, collect stars, and prove yourself worthy of {theme.TreasureEn}.\n\nAre you ready to begin?",
                    OutroTitle = "You Did It!",
                    OutroStory = $"Congratulations, {theme.HeroEn}!\n\nYou've completed your journey through {theme.SettingEn} and claimed {theme.TreasureEn}!\nYour courage, focus and hard work made this victory possible. Every mission you finished made you stronger and wiser.\n\nRest well, hero - a new adventure awaits!",
                    WeekDuration = 7,
                    BonusPoints = rng.Next(80, 151),
                    Status = i < 10 ? AdventureStatus.Active : AdventureStatus.Inactive,
                    InstitutionId = institution.Id,
                    CreatedAt = createdAt,
                    CreatedBy = "DemoSeeder"
                };

                var dayTemplates = templates.OrderBy(_ => rng.Next()).Take(7).ToList();
                for (int day = 0; day < 7; day++)
                {
                    var beat = DayBeats[day];
                    adventure.Tasks.Add(new AdventureTask
                    {
                        TaskTemplateId = dayTemplates[day % dayTemplates.Count].Id,
                        DayNumber = day + 1,
                        Stars = 3,
                        StoryTitle = beat.TitleEn,
                        StoryText = beat.TextEnFormat
                            .Replace("{hero}", theme.HeroEn)
                            .Replace("{setting}", theme.SettingEn)
                            .Replace("{treasure}", theme.TreasureEn),
                        CreatedAt = createdAt,
                        CreatedBy = "DemoSeeder"
                    });
                }

                context.Adventures.Add(adventure);
            }

            await context.SaveChangesAsync();
        }
    }
}
