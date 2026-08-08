using GoKidAPI.Data;
using GoKidAPI.DTO.InstitutionAdmin.Supervisor.Requests;
using GoKidAPI.DTO.InstitutionAdmin.Supervisor.Responses;
using GoKidAPI.DTO.Supervisor.Requests;
using GoKidAPI.DTO.Supervisor.Responses;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Shared;
using GoKidAPI.Services.Email;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.Institution.Interface;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Institution.Implmentation
{
    public class InstitutionSupervisorService : IInstitutionSupervisorService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ResponseHandler _response;
        private readonly ILogger<InstitutionSupervisorService> _logger;
        private readonly IFileUploader _cloudinaryService;

        public InstitutionSupervisorService(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            AppDbContext context,
            IEmailService emailService,
            ResponseHandler response,
            ILogger<InstitutionSupervisorService> logger,
            IFileUploader cloudinaryService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
            _response = response;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
        }


        public async Task<Response<PaginatedList<SupervisorListItemResponse>>> GetAllSupervisorsAsync(
     string currentUserId,
     GetSupervisorsFilters filters)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
                return _response.Unauthorized<PaginatedList<SupervisorListItemResponse>>("User not found");

            IQueryable<Entity.Account.Users.Supervisor> query;

            // Base Query حسب نوع اليوزر
            if (user.UserType == UserType.PlatformAdmin)
            {
                query = _context.Supervisors
                    .Include(s => s.AppUser)
                    .Include(s => s.Institution)
                    .Include(s => s.SupervisedClasses)
                    .Where(s => !s.IsDeleted);
            }
            else if (user.UserType == UserType.InstitutionAdmin)
            {
                var institution = await _context.Institutions
                    .FirstOrDefaultAsync(i => i.InstitutionAdminId == user.Id);

                if (institution == null)
                    return _response.NotFound<PaginatedList<SupervisorListItemResponse>>(
                        "No institution found for this admin");

                query = _context.Supervisors
                    .Include(s => s.AppUser)
                    .Include(s => s.Institution)
                    .Include(s => s.SupervisedClasses)
                    .Where(s => s.InstitutionId == institution.Id && !s.IsDeleted);
            }
            else
            {
                return _response.Forbidden<PaginatedList<SupervisorListItemResponse>>(
                    "You are not authorized to view supervisors");
            }

            // Search
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var term = filters.SearchTerm.Trim().ToLower();

                query = query.Where(s =>
                    s.AppUser.DisplayName.ToLower().Contains(term) ||
                    s.AppUser.Email.ToLower().Contains(term) ||
                    s.AppUser.UserName.ToLower().Contains(term));
            }

            // Sorting
            query = filters.SortColumn switch
            {
                SupervisorSortingColumn.FullName => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(s => s.AppUser.DisplayName)
                    : query.OrderByDescending(s => s.AppUser.DisplayName),

                SupervisorSortingColumn.Email => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(s => s.AppUser.Email)
                    : query.OrderByDescending(s => s.AppUser.Email),

                SupervisorSortingColumn.CreatedAt => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(s => s.CreatedAt)
                    : query.OrderByDescending(s => s.CreatedAt),

                SupervisorSortingColumn.PhoneNumber => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(s => s.AppUser.PhoneNumber)
                    : query.OrderByDescending(s => s.AppUser.PhoneNumber),

                _ => query.OrderByDescending(s => s.CreatedAt)
            };

            // Pagination
            var totalCount = await query.CountAsync();

            var supervisors = await query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(s => new SupervisorListItemResponse
                {
                    Id = s.Id,
                    FullName = s.AppUser.DisplayName ?? "N/A",
                    Email = s.AppUser.Email ?? "N/A",
                    Username = s.AppUser.UserName ?? "N/A",
                    PhoneNumber = s.AppUser.PhoneNumber,
                    CreatedAt = s.CreatedAt,
                    InstitutionName = s.Institution.Name,
                    AvatarUrl = s.AppUser.AvatarUrl ?? "N/A",

                    SupervisedClassesCount = s.SupervisedClasses.Count,

                    // NEW
                    IsAssignedToClass =
    !string.IsNullOrEmpty(filters.ClassId)
    && s.SupervisedClasses.Any(sc => sc.ClassId == filters.ClassId && !sc.IsDeleted)
                })
                .ToListAsync();

            var paginatedList = new PaginatedList<SupervisorListItemResponse>(
                supervisors,
                filters.PageNumber,
                filters.PageSize,
                totalCount);

            return _response.Success(
                paginatedList,
                "Supervisors retrieved successfully");
        }
        public async Task<Response<SupervisorCreatedResponse>> CreateSupervisorAsync(
            string currentAdminUserId,
            CreateSupervisorRequest request)
        {
            _logger.LogInformation(
                "CreateSupervisor started. AdminId: {AdminId}, Email: {Email}",
                currentAdminUserId, request.Email);

            // 1. Get Institution Admin
            var institutionAdmin = await _context.InstitutionAdmins
                .Include(a => a.Institution)
                .FirstOrDefaultAsync(a => a.Id == currentAdminUserId);

            if (institutionAdmin == null)
            {
                _logger.LogWarning(
                    "CreateSupervisor failed. InstitutionAdmin not found. AdminId: {AdminId}",
                    currentAdminUserId);

                return _response.NotFound<SupervisorCreatedResponse>(
                    "Institution administrator was not found.");
            }

            if (institutionAdmin.Institution == null)
            {
                _logger.LogWarning(
                    "CreateSupervisor failed. No institution linked to admin. AdminId: {AdminId}",
                    currentAdminUserId);

                return _response.NotFound<SupervisorCreatedResponse>(
                    "No institution is linked to this administrator.");
            }

            var institution = institutionAdmin.Institution;

            // 2. Check email existence
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning(
                    "CreateSupervisor failed. Email already exists. Email: {Email}, InstitutionId: {InstitutionId}",
                    request.Email, institution.Id);

                return _response.BadRequest<SupervisorCreatedResponse>(
                    "Email address is already in use.");
            }

            // 3. Generate username
            var username = request.Email.Split('@')[0].ToLowerInvariant().Trim();

            // 4. Upload avatar (optional)
            string avatarUrl = null;

            try
            {
                if (request.AvatarFile != null)
                {
                    var uploadResult = await _cloudinaryService.UploadAsync(request.AvatarFile);
                    avatarUrl = uploadResult.Url;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CreateSupervisor failed during avatar upload. Email: {Email}",
                    request.Email);

                return _response.BadRequest<SupervisorCreatedResponse>(
                    "Failed to upload avatar image.");
            }

            // 5. Create AppUser
            var user = new AppUser
            {
                UserName = username,
                Email = request.Email,
                DisplayName = request.FullName,
                UserType = UserType.Supervisor,
                EmailConfirmed = true,
                PhoneNumber = request.PhoneNumber,
                TwoFactorEnabled = false,
                AvatarUrl = avatarUrl
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));

                _logger.LogError(
                    "CreateSupervisor failed while creating AppUser. Email: {Email}, Errors: {Errors}",
                    request.Email, errors);

                return _response.BadRequest<SupervisorCreatedResponse>(
                    errors);
            }

            // 6. Assign role
            var roleResult = await _userManager.AddToRoleAsync(user, UserType.Supervisor.ToString());
            if (!roleResult.Succeeded)
            {
                _logger.LogError(
                    "CreateSupervisor failed while assigning role. UserId: {UserId}, Email: {Email}",
                    user.Id, request.Email);

                await _userManager.DeleteAsync(user); // rollback

                return _response.BadRequest<SupervisorCreatedResponse>(
                    "Failed to assign supervisor role.");
            }

            // 7. Create Supervisor entity
            var supervisor = new Entity.Account.Users.Supervisor
            {
                Id = user.Id,
                InstitutionId = institution.Id,
                CreatedBy = currentAdminUserId
            };

            _context.Supervisors.Add(supervisor);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Supervisor entity created. SupervisorId: {SupervisorId}, InstitutionId: {InstitutionId}",
                supervisor.Id, institution.Id);

            // 8. Send email
            try
            {
                var loginUrl = "https://gokiddashboard.vercel.app/login";

                await _emailService.SendSupervisorCredentialsAsync(
                    request.Email,
                    request.FullName,
                    request.Email,
                    request.Password,
                    loginUrl);

                _logger.LogInformation(
                    "Supervisor credentials email sent successfully. Email: {Email}",
                    request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email for Supervisor. Email: {Email}",
                    request.Email);

                // مش بنفشل العملية كلها عشان الإيميل
            }

            // 9. Prepare response
            var responseData = new SupervisorCreatedResponse
            {
                SupervisorId = supervisor.Id,
                AppUserId = user.Id,
                Email = request.Email,
                Username = username,
                InstitutionName = institution.Name,
                AvatarUrl = avatarUrl
            };

            _logger.LogInformation(
                "CreateSupervisor completed successfully. SupervisorId: {SupervisorId}, AdminId: {AdminId}",
                supervisor.Id, currentAdminUserId);

            return _response.Created(
                responseData,
                "Supervisor account has been created successfully and login credentials have been sent via email.");
        }


        public async Task<Response<SupervisorUpdatedResponse>> UpdateSupervisorAsync(
    string currentAdminUserId,
    string supervisorId,
    UpdateSupervisorRequest request)
        {
            _logger.LogInformation(
                "UpdateSupervisor started. AdminId: {AdminId}, SupervisorId: {SupervisorId}",
                currentAdminUserId, supervisorId);

            // جيب الـ InstitutionAdmin
            var institutionAdmin = await _context.InstitutionAdmins
                .Include(a => a.Institution)
                .FirstOrDefaultAsync(a => a.Id == currentAdminUserId);

            if (institutionAdmin?.Institution == null)
                return _response.NotFound<SupervisorUpdatedResponse>(
                    "Institution administrator was not found.");

            // جيب الـ Supervisor وتحقق إنه تابع لنفس المؤسسة
            var supervisor = await _context.Supervisors
                .Include(s => s.AppUser)
                .FirstOrDefaultAsync(s => s.Id == supervisorId
                                       && s.InstitutionId == institutionAdmin.Institution.Id
                                       && !s.IsDeleted);

            if (supervisor == null)
                return _response.NotFound<SupervisorUpdatedResponse>(
                    "Supervisor not found or does not belong to your institution.");

            var user = supervisor.AppUser;

            // Update الـ fields
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.DisplayName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber;

            // Upload Avatar لو موجود
            if (request.AvatarFile != null)
            {
                try
                {
                    var uploadResult = await _cloudinaryService.UploadAsync(request.AvatarFile);
                    user.AvatarUrl = uploadResult.Url;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UpdateSupervisor failed during avatar upload. SupervisorId: {Id}", supervisorId);
                    return _response.BadRequest<SupervisorUpdatedResponse>("Failed to upload avatar image.");
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                _logger.LogError("UpdateSupervisor failed. Errors: {Errors}", errors);
                return _response.BadRequest<SupervisorUpdatedResponse>(errors);
            }

            supervisor.UpdatedAt = DateTime.UtcNow;
            supervisor.UpdatedBy = currentAdminUserId;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "UpdateSupervisor completed. SupervisorId: {SupervisorId}",
                supervisorId);

            return _response.Success(new SupervisorUpdatedResponse
            {
                SupervisorId = supervisor.Id,
                AppUserId = user.Id,
                FullName = user.DisplayName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                InstitutionName = institutionAdmin.Institution.Name
            }, "Supervisor updated successfully.");
        }

        public async Task<Response<object>> DeleteSupervisorAsync(
            string currentAdminUserId,
            string supervisorId)
        {
            _logger.LogInformation(
                "DeleteSupervisor started. AdminId: {AdminId}, SupervisorId: {SupervisorId}",
                currentAdminUserId, supervisorId);

            var institutionAdmin = await _context.InstitutionAdmins
                .Include(a => a.Institution)
                .FirstOrDefaultAsync(a => a.Id == currentAdminUserId);

            if (institutionAdmin?.Institution == null)
                return _response.NotFound<object>("Institution administrator was not found.");

            var supervisor = await _context.Supervisors
                .Include(s => s.AppUser)
                .Include(s => s.SupervisedClasses)
                .FirstOrDefaultAsync(s => s.Id == supervisorId
                                       && s.InstitutionId == institutionAdmin.Institution.Id
                                       && !s.IsDeleted);

            if (supervisor == null)
                return _response.NotFound<object>(
                    "Supervisor not found or does not belong to your institution.");

            // شيله من كل الكلاسات الأول
            if (supervisor.SupervisedClasses != null && supervisor.SupervisedClasses.Any())
            {
                foreach (var classSupervisor in supervisor.SupervisedClasses.Where(cs => !cs.IsDeleted))
                {
                    classSupervisor.IsDeleted = true;
                    classSupervisor.UpdatedAt = DateTime.UtcNow;
                    classSupervisor.UpdatedBy = currentAdminUserId;
                }
            }

            // Soft delete الـ Supervisor entity
            supervisor.IsDeleted = true;
            supervisor.UpdatedAt = DateTime.UtcNow;
            supervisor.UpdatedBy = currentAdminUserId;

            // Lock الـ AppUser عشان ما يقدرش يسجل دخول
            await _userManager.SetLockoutEnabledAsync(supervisor.AppUser, true);
            await _userManager.SetLockoutEndDateAsync(supervisor.AppUser, DateTimeOffset.MaxValue);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "DeleteSupervisor completed. SupervisorId: {SupervisorId}, AdminId: {AdminId}",
                supervisorId, currentAdminUserId);

            return _response.Deleted<object>("Supervisor deleted successfully.");
        }

    }
}

