using GoKidAPI.Data;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Classes;
using GoKidAPI.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    public static class DemoSupervisorSeeder
    {
        public static async Task SeedAsync(AppDbContext context, UserManager<AppUser> userManager)
        {
            var institutions = await context.Institutions
                .Where(i => i.Code.StartsWith("SCH-DEMO"))
                .OrderBy(i => i.Code)
                .Include(i => i.Supervisors)
                .Include(i => i.Classes)
                .ToListAsync();

            for (int i = 0; i < institutions.Count; i++)
            {
                var institution = institutions[i];
                if (institution.Supervisors.Any()) continue;
                if (!institution.Classes.Any()) continue; // classes must exist first

                var def = DemoDataContext.InstitutionDefs[i];
                var supervisorCount = DemoDataContext.SupervisorsFor(def.Size);
                var classes = institution.Classes.ToList();

                for (int s = 0; s < supervisorCount; s++)
                {
                    var isMale = DemoDataContext.Rng.NextDouble() < 0.4;
                    var name = DemoDataContext.FullName(isMale);
                    var slugName = name.ToLowerInvariant().Replace(" ", ".").Replace("-", "");
                    var joinedAt = institution.CreatedAt.AddDays(DemoDataContext.Rng.Next(5, 30));

                    var appUser = new AppUser
                    {
                        UserName = $"{slugName}.{i}.{s}",
                        Email = $"{slugName}.{i}{s}@gokidmail.com",
                        DisplayName = name,
                        EmailConfirmed = true,
                        UserType = UserType.Supervisor,
                        CreatedAt = joinedAt
                    };

                    var result = await userManager.CreateAsync(appUser, DemoInstitutionSeeder.DemoPassword);
                    if (!result.Succeeded) continue;
                    await userManager.AddToRoleAsync(appUser, "Supervisor");

                    var supervisor = new Supervisor
                    {
                        Id = appUser.Id,
                        InstitutionId = institution.Id,
                        CreatedAt = joinedAt,
                        CreatedBy = "DemoSeeder"
                    };
                    context.Supervisors.Add(supervisor);
                    await context.SaveChangesAsync();

                    // Assign this supervisor to 1-2 classes of the same institution
                    var assignedClasses = classes
                        .OrderBy(_ => DemoDataContext.Rng.Next())
                        .Take(DemoDataContext.Rng.Next(1, 3))
                        .ToList();

                    foreach (var cls in assignedClasses)
                    {
                        context.ClassSupervisors.Add(new ClassSupervisor
                        {
                            ClassId = cls.Id,
                            SupervisorId = supervisor.Id,
                            CreatedAt = joinedAt,
                            CreatedBy = "DemoSeeder"
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
