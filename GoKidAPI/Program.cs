
using System.Text.Json.Serialization;

using DocumentFormat.OpenXml.InkML;

using GoKidAPI.Data;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Extensions;
using GoKidAPI.InfrastructreManage.Options;
using GoKidAPI.Seeder;

using Google;

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;

using Serilog;

using StackExchange.Redis;

namespace GoKidAPI
{
    public class Program
    {
        static readonly string CorsPolicy = "_corsPolicy";
        public static async Task Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
            // Use Serilog

            builder.Host.UseSerilogLogging();

            // Register Auth Mapper 
            builder.Services.AddAppMapper();

            // Register FluentValidation 
            builder.Services.AddFluentValidation();

            // Add EmailService 
            builder.Services.AddEmailServices(builder.Configuration);

            // Add In-memory cahce 
            builder.Services.AddMemoryCache();

            builder.Services.AddHttpContextAccessor();

            // Here we will register IOptions <JWT>, <CloudinarySettings>,....
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JWT"));
            builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
            builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.Configure<RedirectLinksSettings>(builder.Configuration.GetSection("RedirectLinksSettings"));

            // Register Redis
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis"));
                configuration.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(configuration);
            });

            // Cors
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: CorsPolicy,
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                    });
            });

            builder.Services.AddAppDatabase(builder.Configuration);

            // Work with default identity
            builder.Services.AddAppIdentity();

            #region Cookie Settings doesnot extension
            //builder.Services.ConfigureApplicationCookie(options =>
            //{
            //    // Cookie settings
            //    options.Cookie.HttpOnly = true;
            //    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

            //    options.LoginPath = "/Identity/Account/Login";
            //    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            //    options.SlidingExpiration = true;
            //}).AddAuthentication(opt =>
            //{
            //    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //    opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            //}).AddJwtBearer(); 
            #endregion

            builder.Services.AddAppAuthentication(builder.Configuration);
            //builder.Services.AddAppAuthorization();

            builder.Services.AddAppDependencies();

            // Swagger
            builder.Services.AddAppSwagger();

            var app = builder.Build();

            #region Seed User,Role Data
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<AppUser>>();
                var roleManager = services.GetRequiredService<RoleManager<AppRole>>();

                var context = services.GetRequiredService<AppDbContext>();

                await RoleSeeder.SeedAsync(roleManager);
                await CategoriesSeeder.SeedAsync(context);

                //await UserSeeder.SeedAsync(userManager);
            }
            #endregion
            //app.UseResponseCaching();

            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(op =>
                {
                    op.EnablePersistAuthorization();
                    op.SwaggerEndpoint("/swagger/v1/swagger.json", "API GO-KID V1");
                });
            }

            //app.UseMiddleware<ApiKeyMiddleware>();

            app.UseRouting();
            app.UseSerilogRequestLogging();
            app.UseCors(CorsPolicy);

            app.UseAuthentication();
            /// Create fake identity for testing only 
            /*app.Use(async (context, next) =>
            {
                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "Manager")
                };

                var identity = new ClaimsIdentity(claims, "TestAuth");
                context.User = new ClaimsPrincipal(identity);

                await next.Invoke();
            });*/
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
