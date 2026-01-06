using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

using FluentValidation;

using GoKidAPI.Data;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.InfrastructreManage.Options;
using GoKidAPI.Services.Auth;
using GoKidAPI.Services.Category;
using GoKidAPI.Services.Email;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.OTP;
using GoKidAPI.Services.SubCategory;
using GoKidAPI.Services.TaskTemplate;
using GoKidAPI.Services.TaskTemplate.Interfaces;
using GoKidAPI.Services.TokenStore;
using GoKidAPI.Shared;
using GoKidAPI.Validators;
using GoKidAPI.Validators.Category;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Serilog;

namespace GoKidAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IHostBuilder UseSerilogLogging(this IHostBuilder hostBuilder)
        {
            return hostBuilder.UseSerilog((context, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName();
            });
        }
        public static IServiceCollection AddFluentValidation(this IServiceCollection services)
        {
            // Register generic validators
            //services.AddScoped(typeof(IValidator<>), typeof(RequestFiltersValidator<>));
            services.AddScoped<IValidator<RequestFilters<TaskSortingColumn>>,RequestFiltersValidator<TaskSortingColumn>>();

            // Register All validators from the application's assemblies automatically
            services.AddValidatorsFromAssemblies(new[]
            {
                Assembly.GetExecutingAssembly(), // GoKidAPI
                Assembly.GetAssembly(typeof(CreateCategoryRequestValidator))!
            });
            // Enable FluentValidation integration
            //services.AddFluentValidationAutoValidation()
            //        .AddFluentValidationClientsideAdapters();

            return services;
        }
        public static IServiceCollection AddResendOtpRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("SendOtpPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetClientIp(context),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 3,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

            });
            return services;
        }
        public static IServiceCollection AddAppMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(Program));
            return services;
        }
        public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var conMode = configuration["ConnectionMode"] ?? "Default";
            var conString = configuration.GetConnectionString(conMode) ??
                            throw new InvalidOperationException($"Connection string '{conMode}' not found.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(conString));

            return services;
        }
        public static IServiceCollection AddAppIdentity(this IServiceCollection services)
        {
            // Work with default identity
            //services.AddDefaultIdentity<AppUser>(opt =>
            //{
            //    opt.SignIn.RequireConfirmedEmail = false;
            //    opt.User.RequireUniqueEmail = true;
            //}).AddEntityFrameworkStores<AppDbContext>();

            //builder.Services.Configure<IdentityOptions>(options =>
            //{
            //    // Password settings.
            //    options.Password.RequireDigit = true;
            //    options.Password.RequireLowercase = true;
            //    options.Password.RequireNonAlphanumeric = true;
            //    options.Password.RequireUppercase = true;
            //    options.Password.RequiredLength = 6;
            //    options.Password.RequiredUniqueChars = 1;
            //
            //    // Lockout settings.
            //    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            //    options.Lockout.MaxFailedAccessAttempts = 5;
            //    options.Lockout.AllowedForNewUsers = true;
            //
            //    // User settings.
            //    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            //    options.User.RequireUniqueEmail = true;
            //});

            return services;
        }
        public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentity<AppUser, AppRole>(opt =>
            {
                opt.Password.RequireLowercase = true;
                opt.Password.RequireUppercase = true;
                opt.Password.RequiredLength = 8;
                opt.Password.RequireDigit = true;
                opt.Password.RequireNonAlphanumeric = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddRoleManager<RoleManager<AppRole>>()
            .AddUserManager<UserManager<AppUser>>()
            .AddDefaultTokenProviders();


            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies[".AspNetCore.Identity.Application"];
                        return Task.CompletedTask;
                    }
                };

                options.SaveToken = true;
                var jwtSettings = configuration.GetSection("JWT").Get<JwtOptions>();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(jwtSettings.ValidIssuer),
                    ValidIssuer = jwtSettings.ValidIssuer,
                    ValidateAudience = !string.IsNullOrEmpty(jwtSettings.ValidAudience),
                    ValidAudience = jwtSettings.ValidAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                };
            });

            //builder.Services.AddAuthentication()
            //    .AddIdentityServerJwt();

            //.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            // {
            //     // Cookie settings
            //     options.Cookie.HttpOnly = true;
            //     options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            //
            //     options.LoginPath = "/Identity/Account/Login";
            //     options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            //     options.SlidingExpiration = true;
            // })
            //builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            return services;
        }
        //public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
        //{
        //    services.AddAuthorization();
        //    services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        //    services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        //    return services;
        //}
        public static IServiceCollection AddAppDependencies(this IServiceCollection services)
        {
            services.AddTransient<IEmailService, EmailService>();
            services.AddSingleton<IFileUploader, CloudinaryImageUploadService>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenStoreService, TokenStoreService>();
            services.AddScoped<IAuthService, AuthService>();
            //services.AddScoped<GoogleAuthService>();
            //services.AddScoped<FacebookAuthService>();
            //services.AddHttpClient<FacebookAuthService>();
            //services.AddScoped<IOAuthService, GoogleAuthService>();  // Not mendatory to register for each provider only for defaults

            //services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IOtpService, InMemoryOtpService>();

            services.AddScoped<RedirectLinksSettings>();
            services.AddScoped<ResponseHandler>();

            services.AddScoped<ITaskCategoryService, TaskCategoryService>();
            services.AddScoped<ITaskSubCategoryService, TaskSubCategoryService>();

            // Register individual services
            services.AddScoped<IInstantRewardTaskService,InstantRewardTaskService>();
            services.AddScoped<ITextQuestionTaskService,TextQuestionTaskService>();
            services.AddScoped<IVoiceQuestionTaskService,VoiceQuestionTaskService>();
            services.AddScoped<IEvidenceSubmissionTaskService,EvidenceSubmissionTaskService>();
            services.AddScoped<ITaskTemplateQueryService, TaskTemplateQueryService>();

            return services;
        }
        public static IServiceCollection AddAppSwagger(this IServiceCollection services)
        {
            // Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(setup =>
            {
                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    BearerFormat = "JWT",
                    Name = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
                setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });

                // Comment for the API Key
                //setup.AddSecurityDefinition("IWWApiKey", new OpenApiSecurityScheme()
                //{
                //    In = ParameterLocation.Header,
                //    Name = "IWWApiKey", //header with api key
                //    Type = SecuritySchemeType.ApiKey,
                //});
                // Include XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                setup.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            });

            return services;
        }
        public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
        {
            var email = configuration.GetSection("EmailSettings").Get<EmailOptions>();

            // default sender => NoReply
            services.AddFluentEmail(email.FromEmail, email.FromName)
                .AddSmtpSender(new SmtpClient(email.SmtpServer)
                {
                    Port = email.SmtpPort,
                    Credentials = new NetworkCredential(email.Username, email.Password),
                    EnableSsl = email.EnableSsl
                });


            return services;
        }

        private static string GetClientIp(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.ToString().Split(',')[0];
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

}
