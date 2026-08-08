using GoKidAPI.Data;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    // Largest demo seeder: creates parents + children distributed across the demo classes.
    // Most parents get 1 child; ~35% of new parents get a 2nd/3rd child (siblings), which is
    // schema-legal even though the CreateChildAsync API restricts this to 1 for the MVP (see plan).
    public static class DemoParentChildSeeder
    {
        private class OpenParent
        {
            public string ParentId = null!;
            public int ChildCount;
            public int MaxChildren;
            public DateTime JoinedAt;
        }

        public static async Task SeedAsync(AppDbContext context, UserManager<AppUser> userManager)
        {
            var institutions = await context.Institutions
                .Where(i => i.Code.StartsWith("SCH-DEMO"))
                .OrderBy(i => i.Code)
                .Include(i => i.Classes)
                .Include(i => i.EnrolledChildren)
                .ToListAsync();

            var usedCodes = new HashSet<string>(
                await context.Childrens.IgnoreQueryFilters()
                    .Where(c => c.RegistrationCode != null)
                    .Select(c => c.RegistrationCode!)
                    .ToListAsync());

            var now = DateTime.UtcNow;
            var demoWindowStart = now.AddMonths(-3);

            for (int i = 0; i < institutions.Count; i++)
            {
                var institution = institutions[i];
                if (institution.EnrolledChildren.Any()) continue; // already seeded

                var def = DemoDataContext.InstitutionDefs[i];
                var childrenPerClass = DemoDataContext.ChildrenPerClassFor(def.Size);
                var openParents = new List<OpenParent>();

                foreach (var cls in institution.Classes)
                {
                    for (int c = 0; c < childrenPerClass; c++)
                    {
                        var reusable = openParents.FirstOrDefault(p => p.ChildCount < p.MaxChildren);
                        string parentId;
                        DateTime parentJoinedAt;

                        if (reusable != null && DemoDataContext.Rng.NextDouble() < 0.35)
                        {
                            parentId = reusable.ParentId;
                            reusable.ChildCount++;
                            parentJoinedAt = reusable.JoinedAt;
                        }
                        else
                        {
                            var parentIsMale = DemoDataContext.Rng.NextDouble() < 0.35;
                            var parentName = DemoDataContext.FullName(parentIsMale);
                            var joinedAt = DemoDataContext.RandomJoinDate(demoWindowStart, now.AddDays(-1));
                            var slug = Slugify(parentName) + Guid.NewGuid().ToString("N")[..6];

                            var parentUser = new AppUser
                            {
                                UserName = slug,
                                Email = $"{slug}@gokidmail.com",
                                DisplayName = parentName,
                                EmailConfirmed = true,
                                UserType = UserType.Parent,
                                CreatedAt = joinedAt
                            };

                            var result = await userManager.CreateAsync(parentUser, DemoInstitutionSeeder.DemoPassword);
                            if (!result.Succeeded) continue;
                            await userManager.AddToRoleAsync(parentUser, "Parent");

                            var parent = new Parent
                            {
                                Id = parentUser.Id,
                                CreatedAt = joinedAt,
                                CreatedBy = "DemoSeeder"
                            };
                            context.Parents.Add(parent);
                            await context.SaveChangesAsync();

                            parentId = parent.Id;
                            parentJoinedAt = joinedAt;

                            var maxChildren = DemoDataContext.Rng.NextDouble() < 0.30
                                ? DemoDataContext.Rng.Next(2, 4)
                                : 1;
                            openParents.Add(new OpenParent { ParentId = parentId, ChildCount = 1, MaxChildren = maxChildren, JoinedAt = joinedAt });
                        }

                        var isMale = DemoDataContext.Rng.NextDouble() < 0.5;
                        var childName = DemoDataContext.FullName(isMale).Split(' ')[0] + " " +
                                         DemoDataContext.FamilyNames[DemoDataContext.Rng.Next(DemoDataContext.FamilyNames.Length)];

                        var childUser = new AppUser
                        {
                            UserName = $"child_{Guid.NewGuid():N}",
                            Email = null,
                            DisplayName = childName,
                            UserType = UserType.Child,
                            CreatedAt = ClampToNow(parentJoinedAt.AddMinutes(DemoDataContext.Rng.Next(0, 60 * 24 * 3)), now)
                        };

                        var childCreateResult = await userManager.CreateAsync(childUser);
                        if (!childCreateResult.Succeeded) continue;
                        await userManager.AddToRoleAsync(childUser, "Child");

                        string registrationCode;
                        do
                        {
                            registrationCode = DemoDataContext.Rng.Next(100000, 999999).ToString();
                        } while (!usedCodes.Add(registrationCode));

                        var child = new Child
                        {
                            Id = childUser.Id,
                            Name = childName,
                            Age = DemoDataContext.Rng.Next(5, 13),
                            Gender = isMale ? Gender.Male : Gender.Female,
                            RelationshipToParent = DemoDataContext.Rng.NextDouble() < 0.5 ? Relationship.Father : Relationship.Mother,
                            AvatarUrl = DemoDataContext.Placeholder(childName, "128x128"),
                            ParentId = parentId,
                            RegistrationCode = registrationCode,
                            CodeGeneratedAt = childUser.CreatedAt,
                            ClassId = cls.Id,
                            InstitutionId = institution.Id,
                            TotalPoints = 0,
                            HighestPoints = 0,
                            CreatedAt = childUser.CreatedAt,
                            CreatedBy = "DemoSeeder"
                        };
                        context.Childrens.Add(child);
                        await context.SaveChangesAsync();

                        // First child created for a parent becomes their MVP "active" child.
                        var parentRow = await context.Parents.FirstAsync(p => p.Id == parentId);
                        if (parentRow.ActiveChildId == null)
                        {
                            parentRow.ActiveChildId = child.Id;
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }
        }

        private static DateTime ClampToNow(DateTime value, DateTime now) => value > now ? now : value;

        private static string Slugify(string name)
        {
            var lower = name.ToLowerInvariant();
            var chars = lower.Select(ch => char.IsLetterOrDigit(ch) ? ch : '.');
            return string.Concat(chars).Trim('.');
        }
    }
}
