using GoKidAPI.Entity.Account.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder
{
    public class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<AppRole> _roleManager)
        {
            var rolesCount = await _roleManager.Roles.CountAsync();
            if (rolesCount <= 0)
            {
                await _roleManager.CreateAsync(new AppRole()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Parent",
                    NormalizedName = "PARENT"
                });
                await _roleManager.CreateAsync(new AppRole()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Child",
                    NormalizedName = "CHILD"
                });
                await _roleManager.CreateAsync(new AppRole()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "InstitutionAdmin",
                    NormalizedName = "INSTITUTIONADMIN"
                });
                await _roleManager.CreateAsync(new AppRole()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Supervisor",
                    NormalizedName = "SUPERVISOR"
                });
                await _roleManager.CreateAsync(new AppRole()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "PlatformAdmin",
                    NormalizedName = "PLATFORMADMIN"
                });
            }
        }
    }
}
