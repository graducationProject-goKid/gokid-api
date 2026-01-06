using Duende.IdentityServer.EntityFramework.Options;

using GoKidAPI.Entity;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Account.UserTokens;
using GoKidAPI.Entity.Classes;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoKidAPI.Data
{
    public class AppDbContext : KeyApiAuthorizationDbContext<AppUser, AppRole, string>
    {
        public AppDbContext(DbContextOptions options, IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Convert enums to strings
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType
                    .GetProperties()
                    .Where(p => p.PropertyType.IsEnum);

                foreach (var property in properties)
                {
                    modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion<string>();
                }
            }

            // Soft Delete Global Query Filter
            modelBuilder.Entity<Parent>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Child>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<TaskCategory>().HasQueryFilter(tc => !tc.IsDeleted);
            modelBuilder.Entity<TaskTemplateBase>().HasQueryFilter(tt => !tt.IsDeleted);
            modelBuilder.Entity<ChildTask>().HasQueryFilter(ct => !ct.IsDeleted);
            modelBuilder.Entity<PointsTransaction>().HasQueryFilter(pt => !pt.IsDeleted);



            #region Third tables keys [ClassSupervisor - AdventureTask - ChildAdventureProgress]
            // ClassSupervisor Composite Key
            modelBuilder.Entity<ClassSupervisor>()
                .HasKey(cs => new { cs.ClassId, cs.SupervisorId });

            // AdventureTask Composite Key
            modelBuilder.Entity<AdventureTask>()
                .HasKey(cs => new { cs.AdventureId, cs.TaskTemplateId });

            // ChildAdventureProgress Composite Key
            modelBuilder.Entity<ChildAdventureProgress>()
                .HasKey(cs => new { cs.WeeklyAdventureId, cs.ChildId }); 
            #endregion

            modelBuilder.Entity<Child>()
                .HasOne(c => c.Parent)           // Child -> Parent
                .WithMany()                     // Parent -> Childs
                .HasForeignKey(c => c.ParentId) 
                .OnDelete(DeleteBehavior.Restrict);

            // Unique Email + PhoneNumber (AppUser already has it from Identity)
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Child Registration Code Unique
            modelBuilder.Entity<Child>()
                .HasIndex(c => c.RegistrationCode)
                .IsUnique()
                .HasFilter("[RegistrationCode] IS NOT NULL");

            modelBuilder.Entity<Parent>()
                .HasOne(p => p.ActiveChild)
                .WithOne()
                .HasForeignKey<Parent>(p => p.ActiveChildId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tasks <-> subCategory
            modelBuilder.Entity<TaskTemplateBase>()
                .HasOne(t => t.SubCategory)
                .WithMany()
                .HasForeignKey(t => t.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

        }

        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        // Account & Auth
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Child> Childrens { get; set; }

        // Tasks System
        public DbSet<TaskCategory> TaskCategories { get; set; }
        public DbSet<TaskSubCategory> SubCategories { get; set; }
        public DbSet<TaskTemplateBase> TaskTemplates { get; set; }
        public DbSet<ChildTask> ChildTasks { get; set; }

        public DbSet<PointsTransaction> PointsTransactions { get; set; }

    }
}
