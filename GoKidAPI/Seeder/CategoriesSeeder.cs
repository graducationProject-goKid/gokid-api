using GoKidAPI.Data;
using GoKidAPI.Entity.Tasks;

using Google;

namespace GoKidAPI.Seeder
{
    public static class CategoriesSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (!context.TaskCategories.Any())
            {
                var categories = new List<TaskCategory>
                {
                    // A. Academic Tasks - مهام دراسية
                    new TaskCategory
                    {
                        NameAr = "المهام الدراسية",
                        NameEn = "Academic Tasks",
                        ColorHex = "#3498db", // أزرق
                        SubCategories = new List<TaskSubCategory>
                        {
                            new TaskSubCategory { NameAr = "رياضيات", NameEn = "Math" },
                            new TaskSubCategory { NameAr = "لغة إنجليزية / قراءة", NameEn = "English / Reading" },
                            new TaskSubCategory { NameAr = "علوم", NameEn = "Science" },
                            new TaskSubCategory { NameAr = "لغة عربية", NameEn = "Arabic" },
                            new TaskSubCategory { NameAr = "القرآن والدراسات الإسلامية", NameEn = "Qur’an & Islamic Studies" },
                            new TaskSubCategory { NameAr = "واجبات مدرسية", NameEn = "Homework" },
                            new TaskSubCategory { NameAr = "مشاريع مدرسية", NameEn = "School Projects" }
                        }
                    },
                    // B. Behavior & Responsibility - السلوك وتحمل المسؤولية
                    new TaskCategory
                    {
                        NameAr = "السلوك وتحمل المسؤولية",
                        NameEn = "Behavior & Responsibility",
                        ColorHex = "#2ecc71", // أخضر
                        SubCategories = new List<TaskSubCategory>
                        {
                            new TaskSubCategory { NameAr = "تنظيف وترتيب", NameEn = "Clean-up / Organizing" },
                            new TaskSubCategory { NameAr = "روتين الصباح", NameEn = "Morning Routine" },
                            new TaskSubCategory { NameAr = "روتين النوم", NameEn = "Bedtime Routine" },
                            new TaskSubCategory { NameAr = "النظافة الشخصية", NameEn = "Personal Hygiene" },
                            new TaskSubCategory { NameAr = "حسن السلوك", NameEn = "Good Manners" },
                            new TaskSubCategory { NameAr = "المساعدة في المنزل", NameEn = "Helping at Home" }
                        }
                    },
                    // C. Creativity & Skills - الإبداع والمهارات
                    new TaskCategory
                    {
                        NameAr = "الإبداع والمهارات",
                        NameEn = "Creativity & Skills",
                        ColorHex = "#f1c40f", // أصفر
                        SubCategories = new List<TaskSubCategory>
                        {
                            new TaskSubCategory { NameAr = "رسم", NameEn = "Drawing" },
                            new TaskSubCategory { NameAr = "حرف يدوية", NameEn = "Crafting" },
                            new TaskSubCategory { NameAr = "تلوين", NameEn = "Coloring" },
                            new TaskSubCategory { NameAr = "موسيقى", NameEn = "Music" },
                            new TaskSubCategory { NameAr = "بناء (ليجو، مكعبات)", NameEn = "Building (LEGO, blocks)" },
                            new TaskSubCategory { NameAr = "كتابة قصص", NameEn = "Writing stories" }
                        }
                    },
                    // D. Health & Physical Activities - الصحة والنشاط البدني
                    new TaskCategory
                    {
                        NameAr = "الصحة والنشاط البدني",
                        NameEn = "Health & Physical Activities",
                        ColorHex = "#e74c3c", // أحمر
                        SubCategories = new List<TaskSubCategory>
                        {
                            new TaskSubCategory { NameAr = "تمرين", NameEn = "Exercise" },
                            new TaskSubCategory { NameAr = "رياضة", NameEn = "Sports" },
                            new TaskSubCategory { NameAr = "أكل صحي", NameEn = "Healthy Eating" },
                            new TaskSubCategory { NameAr = "أنشطة خارجية", NameEn = "Outdoor Activities" }
                        }
                    },
                    // E. Emotional & Social Development - التطور العاطفي والاجتماعي
                    new TaskCategory
                    {
                        NameAr = "التطور العاطفي والاجتماعي",
                        NameEn = "Emotional & Social Development",
                        ColorHex = "#9b59b6", // بنفسجي
                        SubCategories = new List<TaskSubCategory>
                        {
                            new TaskSubCategory { NameAr = "مشاركة", NameEn = "Sharing" },
                            new TaskSubCategory { NameAr = "امتنان", NameEn = "Gratitude" },
                            new TaskSubCategory { NameAr = "مهام اللطف", NameEn = "Kindness Tasks" },
                            new TaskSubCategory { NameAr = "ترابط عائلي", NameEn = "Family Connection" },
                            new TaskSubCategory { NameAr = "تفكير إيجابي", NameEn = "Positive Thinking" }
                        }
                    }
                };

                context.TaskCategories.AddRange(categories);

                await context.SaveChangesAsync();
            }
        }
    }
}
