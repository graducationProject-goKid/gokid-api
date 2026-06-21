using GoKidAPI.Data;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder
{
    public class UserSeeder
    {
        public static async Task SeedAsync(
            UserManager<AppUser> _userManager)
        {
            var usersCount = await _userManager.Users.CountAsync();
            if (usersCount <= 0)
            {
                //-----------------------------------------
                // PLATFORM ADMIN USER
                //-----------------------------------------
                var platformAdmin = new AppUser()
                {
                    UserName = "platformadmin",
                    Email = "admin@gokid.com",
                    DisplayName = "Platform Admin",
                    EmailConfirmed = true,
                    UserType = UserType.PlatformAdmin
                };

                await _userManager.CreateAsync(platformAdmin, "Admin@123");
                await _userManager.AddToRoleAsync(platformAdmin, "PlatformAdmin");
            }
        }
    }
}
