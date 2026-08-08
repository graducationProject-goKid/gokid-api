using GoKidAPI.Data;
using GoKidAPI.DTO.Institution.Requests;
using GoKidAPI.DTO.Institution.Responses;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Enums;
using GoKidAPI.Services.Email;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.Institution.Interface;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Institution.Implmentation
{
    public class InstitutionService : IInstitutionService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IFileUploader _fileUploader;
        private readonly ResponseHandler _response;
        private readonly ILogger<InstitutionService> _logger;

        public InstitutionService(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IEmailService emailService,
            IFileUploader fileUploader,
            ResponseHandler response,
            ILogger<InstitutionService> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _fileUploader = fileUploader;
            _response = response;
            _logger = logger;
        }

        public async Task<Response<InstitutionDetailsResponse>> CreateInstitutionAsync(
            CreateInstitutionRequest request, string platformAdminId)
        {
            _logger.LogInformation("CreateInstitution started. AdminEmail: {Email}", request.AdminEmail);

            var existing = await _userManager.FindByEmailAsync(request.AdminEmail);
            if (existing != null)
                return _response.BadRequest<InstitutionDetailsResponse>("Email is already in use.");

            var password = GeneratePassword();
            var username = request.AdminEmail.Split('@')[0].ToLowerInvariant().Trim();

            var user = new AppUser
            {
                UserName = username,
                Email = request.AdminEmail,
                DisplayName = request.AdminFullName,
                UserType = UserType.InstitutionAdmin,
                EmailConfirmed = true,
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("CreateInstitution failed during user creation. Errors: {Errors}", errors);
                return _response.BadRequest<InstitutionDetailsResponse>(errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, UserType.InstitutionAdmin.ToString());
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return _response.BadRequest<InstitutionDetailsResponse>("Failed to assign role.");
            }

            var institutionAdmin = new InstitutionAdmin
            {
                Id = user.Id,
                InstitutionId = null,
                CreatedBy = platformAdminId
            };
            _context.InstitutionAdmins.Add(institutionAdmin);
            await _context.SaveChangesAsync();

            string? logoUrl = null;
            string? logoPublicId = null;
            if (request.Logo != null)
            {
                try
                {
                    var upload = await _fileUploader.UploadAsync(request.Logo);
                    logoUrl = upload.Url;
                    logoPublicId = upload.PublicId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Logo upload failed during institution creation.");
                }
            }

            var code = "SCH-" + Random.Shared.Next(10000, 99999).ToString();

            var institution = new Entity.Institiution.Institution
            {
                Name = request.Name,
                Code = code,
                PhoneNumber = request.PhoneNumber,
                Email = request.AdminEmail,
                Address = request.Address,
                City = request.City,
                Country = request.Country,
                Website = request.Website,
                Description = request.Description,
                LogoUrl = logoUrl,
                LogoPublicId = logoPublicId,
                InstitutionAdminId = institutionAdmin.Id,
                CreatedBy = platformAdminId
            };

            _context.Institutions.Add(institution);
            await _context.SaveChangesAsync();

            institutionAdmin.InstitutionId = institution.Id;
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendInstitutionAdminCredentialsAsync(
                    request.AdminEmail,
                    request.AdminFullName,
                    request.Name,
                    request.AdminEmail,
                    password,
                    "https://go-kid.com/login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send credentials email to {Email}", request.AdminEmail);
            }

            _logger.LogInformation("Institution created. Id: {Id}", institution.Id);

            return _response.Created(MapToDetails(institution, institutionAdmin, user), "Institution created successfully.");
        }

        public async Task<Response<InstitutionDetailsResponse>> UpdateInstitutionAsync(
            string institutionId, UpdateInstitutionRequest request, string platformAdminId)
        {
            var institution = await _context.Institutions
                .Include(i => i.Admin).ThenInclude(a => a.AppUser)
                .Include(i => i.Supervisors).ThenInclude(s => s.AppUser)
                .Include(i => i.Supervisors).ThenInclude(s => s.SupervisedClasses)
                .FirstOrDefaultAsync(i => i.Id == institutionId && !i.IsDeleted);

            if (institution == null)
                return _response.NotFound<InstitutionDetailsResponse>("Institution not found.");

            if (!string.IsNullOrWhiteSpace(request.Name)) institution.Name = request.Name;
            if (request.PhoneNumber != null) institution.PhoneNumber = request.PhoneNumber;
            if (request.Address != null) institution.Address = request.Address;
            if (request.City != null) institution.City = request.City;
            if (request.Country != null) institution.Country = request.Country;
            if (request.Website != null) institution.Website = request.Website;
            if (request.Description != null) institution.Description = request.Description;

            if (request.Logo != null)
            {
                try
                {
                    var upload = await _fileUploader.UploadAsync(request.Logo);
                    institution.LogoUrl = upload.Url;
                    institution.LogoPublicId = upload.PublicId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Logo upload failed during institution update.");
                }
            }

            institution.UpdatedAt = DateTime.UtcNow;
            institution.UpdatedBy = platformAdminId;
            await _context.SaveChangesAsync();

            var admin = institution.Admin;
            var adminUser = admin.AppUser;

            return _response.Success(MapToDetails(institution, admin, adminUser), "Institution updated successfully.");
        }

        public async Task<Response<object>> DeleteInstitutionAsync(string institutionId, string platformAdminId)
        {
            var institution = await _context.Institutions
                .Include(i => i.Admin).ThenInclude(a => a.AppUser)
                .FirstOrDefaultAsync(i => i.Id == institutionId && !i.IsDeleted);

            if (institution == null)
                return _response.NotFound<object>("Institution not found.");

            institution.IsDeleted = true;
            institution.UpdatedAt = DateTime.UtcNow;
            institution.UpdatedBy = platformAdminId;

            var admin = institution.Admin;
            if (admin != null)
            {
                admin.IsDeleted = true;
                admin.UpdatedAt = DateTime.UtcNow;
                admin.UpdatedBy = platformAdminId;

                if (admin.AppUser != null)
                {
                    admin.AppUser.LockoutEnd = DateTimeOffset.MaxValue;
                }
            }

            await _context.SaveChangesAsync();
            return _response.Deleted<object>("Institution deleted successfully.");
        }

        public async Task<Response<PaginatedList<InstitutionListItemResponse>>> GetAllInstitutionsAsync(
            int pageNumber, int pageSize, string? search)
        {
            var query = _context.Institutions
                .Include(i => i.Admin).ThenInclude(a => a.AppUser)
                .Include(i => i.Classes)
                .Include(i => i.EnrolledChildren)
                .Include(i => i.Supervisors)
                .Where(i => !i.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(term) ||
                    i.Code.ToLower().Contains(term) ||
                    (i.City != null && i.City.ToLower().Contains(term)) ||
                    (i.Country != null && i.Country.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync();

            var institutions = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new InstitutionListItemResponse
                {
                    Id = i.Id,
                    Name = i.Name,
                    Code = i.Code,
                    City = i.City,
                    Country = i.Country,
                    LogoUrl = i.LogoUrl,
                    AdminName = i.Admin.AppUser.DisplayName ?? i.Admin.AppUser.Email ?? "",
                    AdminEmail = i.Email ?? "",
                    ClassCount = i.Classes.Count(c => !c.IsDeleted),
                    StudentCount = i.EnrolledChildren.Count(c => !c.IsDeleted),
                    SupervisorCount = i.Supervisors.Count(s => !s.IsDeleted),
                    CreatedAt = i.CreatedAt,
                    AdminPhoneNumber = i.PhoneNumber ?? "",
                    Website = i.Website

                })
                .ToListAsync();

            return _response.Success(
                new PaginatedList<InstitutionListItemResponse>(institutions, pageNumber, pageSize, totalCount),
                "Institutions retrieved successfully.");
        }

        public async Task<Response<InstitutionDetailsResponse>> GetInstitutionDetailsAsync(string institutionId)
        {
            var details = await _context.Institutions
                .AsNoTracking()
                .Where(i => i.Id == institutionId && !i.IsDeleted)
                .Select(i => new InstitutionDetailsResponse
                {
                    Id = i.Id,
                    Name = i.Name,
                    Code = i.Code,
                    PhoneNumber = i.PhoneNumber,
                    Email = i.Email,
                    Address = i.Address,
                    City = i.City,
                    Country = i.Country,
                    LogoUrl = i.LogoUrl,
                    Website = i.Website,
                    Description = i.Description,

                    AdminId = i.Admin.Id,
                    AdminName = i.Admin.AppUser.DisplayName ?? i.Admin.AppUser.Email ?? "",
                    AdminEmail = i.Admin.AppUser.Email ?? "",

                    ClassCount = i.Classes.Count(c => !c.IsDeleted),
                    StudentCount = i.EnrolledChildren.Count(c => !c.IsDeleted),
                    SupervisorCount = i.Supervisors.Count(s => !s.IsDeleted),

                    CreatedAt = i.CreatedAt,

                    Supervisors = i.Supervisors
                        .Where(s => !s.IsDeleted)
                        .Select(s => new SupervisorSummary
                        {
                            Id = s.Id,
                            FullName = s.AppUser.DisplayName ?? s.AppUser.Email ?? "",
                            Email = s.AppUser.Email ?? "",
                            AvatarUrl = s.AppUser.AvatarUrl,
                            AssignedClassesCount = s.SupervisedClasses.Count(sc => !sc.IsDeleted)
                        })
                        .ToList(),

                    Classes = i.Classes
                        .Where(c => !c.IsDeleted)
                        .Select(c => new ClassSummary
                        {
                            Id = c.Id,
                            Name = c.Name,
                            ChildrenCount = c.Children.Count(ch => !ch.IsDeleted),
                            SupervisorsCount = c.Supervisors.Count(s => !s.IsDeleted),
                            CreatedAt = c.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (details == null)
                return _response.NotFound<InstitutionDetailsResponse>("Institution not found.");

            return _response.Success(details, "Institution details retrieved successfully.");
        }
        public async Task<Response<SupervisorProfileResponse>> GetSupervisorProfileAsync(
            string institutionId, string supervisorId)
        {
            var supervisor = await _context.Supervisors
                .Include(s => s.AppUser)
                .Include(s => s.Institution)
                .Include(s => s.SupervisedClasses)
                    .ThenInclude(sc => sc.Class)
                        .ThenInclude(c => c.Children)
                .FirstOrDefaultAsync(s =>
                    s.Id == supervisorId &&
                    s.InstitutionId == institutionId &&
                    !s.IsDeleted);

            if (supervisor == null)
                return _response.NotFound<SupervisorProfileResponse>("Supervisor not found in this institution.");

            var profile = new SupervisorProfileResponse
            {
                Id = supervisor.Id,
                FullName = supervisor.AppUser.DisplayName ?? supervisor.AppUser.Email ?? "",
                Email = supervisor.AppUser.Email ?? "",
                PhoneNumber = supervisor.AppUser.PhoneNumber,
                AvatarUrl = supervisor.AppUser.AvatarUrl,
                InstitutionId = supervisor.InstitutionId,
                InstitutionName = supervisor.Institution.Name,
                CreatedAt = supervisor.CreatedAt,
                AssignedClasses = supervisor.SupervisedClasses?
                    .Where(sc => !sc.IsDeleted)
                    .Select(sc => new AssignedClassInfo
                    {
                        ClassId = sc.ClassId,
                        ClassName = sc.Class.Name,
                        ChildrenCount = sc.Class.Children?.Count(c => !c.IsDeleted) ?? 0
                    })
                    .ToList() ?? new List<AssignedClassInfo>()
            };

            return _response.Success(profile, "Supervisor profile retrieved successfully.");
        }

        private static InstitutionDetailsResponse MapToDetails(
            Entity.Institiution.Institution institution,
            InstitutionAdmin admin,
            AppUser adminUser) => new()
        {
            Id = institution.Id,
            Name = institution.Name,
            Code = institution.Code,
            PhoneNumber = institution.PhoneNumber,
            Email = institution.Email,
            Address = institution.Address,
            City = institution.City,
            Country = institution.Country,
            LogoUrl = institution.LogoUrl,
            Website = institution.Website,
            Description = institution.Description,
            AdminId = admin.Id,
            AdminName = adminUser.DisplayName ?? adminUser.Email ?? "",
            AdminEmail = adminUser.Email ?? "",
            ClassCount = institution.Classes?.Count(c => !c.IsDeleted) ?? 0,
            StudentCount = institution.EnrolledChildren?.Count(c => !c.IsDeleted) ?? 0,
            SupervisorCount = institution.Supervisors?.Count(s => !s.IsDeleted) ?? 0,
            CreatedAt = institution.CreatedAt,
        };

        private static string GeneratePassword()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%&*";

            var rng = Random.Shared;
            var chars = new List<char>
            {
                upper[rng.Next(upper.Length)],
                upper[rng.Next(upper.Length)],
                lower[rng.Next(lower.Length)],
                lower[rng.Next(lower.Length)],
                digits[rng.Next(digits.Length)],
                digits[rng.Next(digits.Length)],
                special[rng.Next(special.Length)],
                special[rng.Next(special.Length)],
            };

            return new string(chars.OrderBy(_ => rng.Next()).ToArray());
        }
    }
}
