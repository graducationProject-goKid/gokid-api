using GoKidAPI.Data;
using GoKidAPI.Entity.Levels;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder
{
    public static class LevelSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (context.Levels.IgnoreQueryFilters().Any())
                return;

            var levels = new List<Level>
            {
                new Level { Order = 1,  Name = "Starter",       MinPoints = 0,      BadgeUrl = "https://placehold.co/128x128?text=L1" },
                new Level { Order = 2,  Name = "Explorer",      MinPoints = 100,    BadgeUrl = "https://placehold.co/128x128?text=L2" },
                new Level { Order = 3,  Name = "Adventurer",    MinPoints = 250,    BadgeUrl = "https://placehold.co/128x128?text=L3" },
                new Level { Order = 4,  Name = "Trailblazer",   MinPoints = 500,    BadgeUrl = "https://placehold.co/128x128?text=L4" },
                new Level { Order = 5,  Name = "Guardian",      MinPoints = 800,    BadgeUrl = "https://placehold.co/128x128?text=L5" },
                new Level { Order = 6,  Name = "Hero",          MinPoints = 1200,   BadgeUrl = "https://placehold.co/128x128?text=L6" },
                new Level { Order = 7,  Name = "Champion",      MinPoints = 1800,   BadgeUrl = "https://placehold.co/128x128?text=L7" },
                new Level { Order = 8,  Name = "Legend",        MinPoints = 2500,   BadgeUrl = "https://placehold.co/128x128?text=L8" },
                new Level { Order = 9,  Name = "Master",        MinPoints = 3500,   BadgeUrl = "https://placehold.co/128x128?text=L9" },
                new Level { Order = 10, Name = "Grand Master",  MinPoints = 5000,   BadgeUrl = "https://placehold.co/128x128?text=L10" },
                new Level { Order = 11, Name = "Superstar",     MinPoints = 7000,   BadgeUrl = "https://placehold.co/128x128?text=L11" },
                new Level { Order = 12, Name = "Elite",         MinPoints = 9500,   BadgeUrl = "https://placehold.co/128x128?text=L12" },
                new Level { Order = 13, Name = "Ultimate Hero", MinPoints = 13000,  BadgeUrl = "https://placehold.co/128x128?text=L13" },
                new Level { Order = 14, Name = "Mythic",        MinPoints = 17000,  BadgeUrl = "https://placehold.co/128x128?text=L14" },
                new Level { Order = 15, Name = "GoKid Legend",  MinPoints = 22000,  BadgeUrl = "https://placehold.co/128x128?text=L15" },
            };

            context.Levels.AddRange(levels);
            await context.SaveChangesAsync();
        }
    }
}
