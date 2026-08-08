using GoKidAPI.Data;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    // Creates institutions + their 1-1 InstitutionAdmin.
    // Handles the circular FK (Institution.InstitutionAdminId <-> InstitutionAdmin.InstitutionId)
    // by saving the admin row first (InstitutionId = null), then the institution, then linking back.
    public static class DemoInstitutionSeeder
    {
        public const string DemoPassword = "Demo@1234";

        public static async Task SeedAsync(AppDbContext context, UserManager<AppUser> userManager)
        {
            if (await context.Institutions.IgnoreQueryFilters().AnyAsync(i => i.Code.StartsWith("SCH-DEMO")))
                return;

            var now = DateTime.UtcNow;

            for (int i = 0; i < DemoDataContext.InstitutionDefs.Length; i++)
            {
                var def = DemoDataContext.InstitutionDefs[i];
                var slug = Slugify(def.Name);
                var foundedAt = now.AddMonths(-DemoDataContext.Rng.Next(5, 10));

                var adminUser = new AppUser
                {
                    UserName = $"{slug}.admin",
                    Email = $"admin@{slug}.edu.eg",
                    DisplayName = $"{def.Name} Admin",
                    EmailConfirmed = true,
                    UserType = UserType.InstitutionAdmin,
                    CreatedAt = foundedAt
                };

                var createResult = await userManager.CreateAsync(adminUser, DemoPassword);
                if (!createResult.Succeeded) continue;

                await userManager.AddToRoleAsync(adminUser, "InstitutionAdmin");

                var institutionAdmin = new InstitutionAdmin
                {
                    Id = adminUser.Id,
                    InstitutionId = null,
                    CreatedAt = foundedAt,
                    CreatedBy = "DemoSeeder"
                };
                context.InstitutionAdmins.Add(institutionAdmin);
                await context.SaveChangesAsync();

                var institution = new Institution
                {
                    Name = def.Name,
                    Code = DemoDataContext.InstitutionCode(i),
                    City = def.City,
                    Country = "Egypt",
                    PhoneNumber = $"+2010{DemoDataContext.Rng.Next(10000000, 99999999)}",
                    Email = $"info@{slug}.edu.eg",
                    Address = $"{DemoDataContext.Rng.Next(1, 200)} El-Nasr St., {def.City}",
                    Website = $"https://www.{slug}.edu.eg",
                    Description = $"{def.Name} is a growing educational institution in {def.City} using GoKid to build responsibility and good habits in its students.",
                    LogoUrl = DemoDataContext.Placeholder(slug, "300x300"),
                    InstitutionAdminId = institutionAdmin.Id,
                    CreatedAt = foundedAt,
                    CreatedBy = "DemoSeeder"
                };
                context.Institutions.Add(institution);
                await context.SaveChangesAsync();

                institutionAdmin.InstitutionId = institution.Id;
                await context.SaveChangesAsync();
            }
        }

        private static string Slugify(string name)
        {
            var lower = name.ToLowerInvariant();
            var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : ' ');
            var slug = string.Concat(chars);
            var parts = slug.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2);
            return string.Join("-", parts);
        }
    }
}
