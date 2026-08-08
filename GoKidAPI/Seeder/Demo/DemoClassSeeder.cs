using GoKidAPI.Data;
using GoKidAPI.Entity.Classes;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    public static class DemoClassSeeder
    {
        private static readonly string[] Grades =
        {
            "KG1", "KG2", "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6"
        };
        private static readonly string[] Sections = { "A", "B", "C" };

        public static async Task SeedAsync(AppDbContext context)
        {
            var institutions = await context.Institutions
                .Where(i => i.Code.StartsWith("SCH-DEMO"))
                .OrderBy(i => i.Code)
                .Include(i => i.Classes)
                .ToListAsync();

            for (int i = 0; i < institutions.Count; i++)
            {
                var institution = institutions[i];
                if (institution.Classes.Any()) continue; // already seeded for this institution

                var def = DemoDataContext.InstitutionDefs[i];
                var classCount = DemoDataContext.ClassesFor(def.Size);

                for (int c = 0; c < classCount; c++)
                {
                    var grade = Grades[c % Grades.Length];
                    var section = Sections[(c / Grades.Length) % Sections.Length];

                    context.Classes.Add(new Class
                    {
                        Name = $"{grade} - {section}",
                        InstitutionId = institution.Id,
                        CreatedAt = institution.CreatedAt.AddDays(DemoDataContext.Rng.Next(1, 10)),
                        CreatedBy = "DemoSeeder"
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
