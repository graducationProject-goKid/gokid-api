using GoKidAPI.Data;
using GoKidAPI.DTO.Classes.Requests;
using GoKidAPI.DTO.Classes.Responses;
using GoKidAPI.DTO.Levels.Responses;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Classes;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Shared;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Classes
{
    public class ClassService : IClassService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _response;
        private readonly ILogger<ClassService> _logger;
        private readonly INotificationService _notificationService;
        public ClassService(
            AppDbContext context,
            ResponseHandler response,
            ILogger<ClassService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _response = response;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<Response<ClassDetailsResponse>> CreateClassAsync(string adminUserId, CreateClassRequest request)
        {
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == adminUserId);

            if (institution == null)
                return _response.NotFound<ClassDetailsResponse>("No institution found for this admin");

            var newClass = new Class
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                InstitutionId = institution.Id,
                CreatedBy = adminUserId
            };

            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();

            var responseData = new ClassDetailsResponse
            {
                Id = newClass.Id,
                Name = newClass.Name,
                InstitutionName = institution.Name,
                ChildrenCount = 0,
                SupervisorsCount = 0,
                CreatedAt = newClass.CreatedAt
            };

            return _response.Created(responseData, "Class created successfully");
        }

        public async Task<Response<ClassDetailsResponse>> UpdateClassAsync(string adminUserId, string classId, UpdateClassRequest request)
        {
            var classEntity = await _context.Classes
                .Include(c => c.Institution)
                .Include(c=>c.Supervisors)
                .FirstOrDefaultAsync(c => c.Id == classId && c.Institution.InstitutionAdminId == adminUserId);

            if (classEntity == null)
                return _response.NotFound<ClassDetailsResponse>("Class not found or you do not have permission");

            classEntity.Name = request.Name;
            classEntity.UpdatedAt = DateTime.UtcNow;
            classEntity.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();

            var responseData = new ClassDetailsResponse
            {
                Id = classEntity.Id,
                Name = classEntity.Name,
                InstitutionName = classEntity.Institution.Name,
                ChildrenCount = await _context.Childrens.CountAsync(ch => ch.ClassId == classId),
                SupervisorsCount = classEntity.Supervisors.Count(),
                CreatedAt = classEntity.CreatedAt,
                UpdatedAt = classEntity.UpdatedAt
            };

            return _response.Success(responseData, "Class updated successfully");
        }

        public async Task<Shared.Response<string>> DeleteClassAsync(string adminUserId, string classId)
        {
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == classId && c.Institution.InstitutionAdminId == adminUserId);

            if (classEntity == null)
                return _response.NotFound<string>("Class not found or you do not have permission");

            classEntity.IsDeleted = true;
            classEntity.UpdatedAt = DateTime.UtcNow;
            classEntity.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();

            return _response.Deleted<string>("Class deleted successfully (soft delete)");
        }

        public async Task<Response<ClassDetailsResponse>> GetClassByIdAsync(string adminUserId, string classId)
        {
            var classEntity = await _context.Classes
                .Include(c => c.Institution)
                .Include(c=>c.Supervisors)
                .FirstOrDefaultAsync(c => c.Id == classId && c.Institution.InstitutionAdminId == adminUserId && !c.IsDeleted);

            if (classEntity == null)
                return _response.NotFound<ClassDetailsResponse>("Class not found or you do not have permission");

            var responseData = new ClassDetailsResponse
            {
                Id = classEntity.Id,
                Name = classEntity.Name,
                InstitutionName = classEntity.Institution.Name,
                ChildrenCount = await _context.Childrens.CountAsync(ch => ch.ClassId == classId),
                SupervisorsCount = classEntity.Supervisors.Count(),
                AdventuresCount = await _context.WeeklyAdventures
        .CountAsync(a => a.ClassId == classId && !a.IsDeleted),
                CreatedAt = classEntity.CreatedAt,
                UpdatedAt = classEntity.UpdatedAt
            };

            return _response.Success(responseData, "Class retrieved successfully");
        }

        public async Task<Response<PaginatedList<ClassListItemResponse>>> GetAllClassesAsync(string adminUserId, GetClassesFilters filters)
        {
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == adminUserId);

            if (institution == null)
                return _response.NotFound<PaginatedList<ClassListItemResponse>>("No institution found for this admin");

            var query = _context.Classes
                .Where(c => c.InstitutionId == institution.Id && !c.IsDeleted);

            // Search by name
            if (!string.IsNullOrWhiteSpace(filters.SearchName))
            {
                var term = filters.SearchName.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(term));
            }

            // Sorting
            query = filters.SortColumn switch
            {
                ClassSortingColumn.Name => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(c => c.Name)
                    : query.OrderByDescending(c => c.Name),

                ClassSortingColumn.ChildrenCount => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(c => c.Children.Count)
                    : query.OrderByDescending(c => c.Children.Count),

                ClassSortingColumn.SupervisorsCount => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(c => c.Supervisors.Count)
                    : query.OrderByDescending(c => c.Supervisors.Count),

                ClassSortingColumn.CreatedAt => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(c => c.CreatedAt)
                    : query.OrderByDescending(c => c.CreatedAt),

                _ => query.OrderBy(c => c.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var classes = await query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(c => new ClassListItemResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    ChildrenCount = c.Children.Count,
                    SupervisorsCount = c.Supervisors.Count,
                    AdventuresCount = c.WeeklyAdventures.Count(a => !a.IsDeleted),
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            var paginated = new PaginatedList<ClassListItemResponse>(
                classes,
                filters.PageNumber,
                filters.PageSize,
                totalCount);

            return _response.Success(paginated, "Classes retrieved successfully");
        }
        public async Task<Response<SupervisorAssignmentResponse>> AssignSupervisorToClassAsync(
            string adminUserId,
            string classId,
            AssignSupervisorToClassRequest request)
        {
            var institution = await _context.InstitutionAdmins
                .FirstOrDefaultAsync(i => i.Id == adminUserId);

            if (institution == null)
                return _response.NotFound<SupervisorAssignmentResponse>("No institution found for this admin");

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institution.Id && !c.IsDeleted);

            if (classEntity == null)
                return _response.NotFound<SupervisorAssignmentResponse>("Class not found or does not belong to your institution");

            var supervisor = await _context.Supervisors
                .Include(s => s.AppUser)
                .FirstOrDefaultAsync(s => s.Id == request.SupervisorId && s.InstitutionId == institution.Id && !s.IsDeleted);

            if (supervisor == null)
                return _response.NotFound<SupervisorAssignmentResponse>("Supervisor not found or does not belong to your institution");

            var existingAssignment = await _context.ClassSupervisors
                .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.SupervisorId == request.SupervisorId);

            if (existingAssignment != null)
            {
                if (!existingAssignment.IsDeleted)
                    return _response.Conflict<SupervisorAssignmentResponse>("Supervisor is already assigned to this class");

                // إعادة تفعيل الـ assignment القديم
                existingAssignment.IsDeleted = false;
                existingAssignment.UpdatedAt = DateTime.UtcNow;
                existingAssignment.UpdatedBy = adminUserId;
            }
            else
            {
                var assignment = new ClassSupervisor
                {
                    ClassId = classId,
                    SupervisorId = request.SupervisorId,
                    CreatedBy = adminUserId
                };

                _context.ClassSupervisors.Add(assignment);
            }

            await _context.SaveChangesAsync();

            // Send notification to the supervisor about the assignment
            await _notificationService.SendAsync(
                supervisor.AppUserId,
                NotificationType.SupervisorAssignedToClass,
                "Class Assigned",
                $"You have been assigned as the supervisor of {classEntity.Name}.",
                classEntity.Id
            );
            var responseData = new SupervisorAssignmentResponse
            {
                ClassId = classEntity.Id,
                ClassName = classEntity.Name,
                SupervisorId = supervisor.Id,
                SupervisorName = supervisor.AppUser.DisplayName ?? "N/A",
                Message = "Supervisor assigned to class successfully"
            };

            _logger.LogInformation("Supervisor {SupervisorId} assigned to Class {ClassId} by {AdminId}",
                request.SupervisorId, classId, adminUserId);

            return _response.Success(responseData, "Supervisor assigned successfully");
        }

        public async Task<Response<SupervisorAssignmentResponse>> RemoveSupervisorFromClassAsync(
            string adminUserId,
            string classId,
            string supervisorId)
        {
            // 1. Retrieve the institution associated with the current admin
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == adminUserId);

            if (institution == null)
                return _response.NotFound<SupervisorAssignmentResponse>("No institution found for this admin");

            // 2. Retrieve the assignment between the supervisor and the class
            var assignment = await _context.ClassSupervisors
                .Include(cs => cs.Class)
                .Include(cs => cs.Supervisor)
                .ThenInclude(s => s.AppUser)
                .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.SupervisorId == supervisorId);

            if (assignment == null)
                return _response.NotFound<SupervisorAssignmentResponse>("No assignment found between this supervisor and class");

            // 3. Ensure that the class belongs to the admin's institution
            if (assignment.Class.InstitutionId != institution.Id)
                return _response.Forbidden<SupervisorAssignmentResponse>("You do not have permission to modify this class");

            // 4. Soft delete the assignment
            assignment.IsDeleted = true;
            assignment.UpdatedAt = DateTime.UtcNow;
            assignment.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();

            await _notificationService.SendAsync(
                userId: assignment.SupervisorId,
                type: NotificationType.SupervisorUnassignedFromClass,
                title: "Class Unassigned",
                body: $"You have been removed as the supervisor of '{assignment.Class.Name}'.",
                relatedEntityId: assignment.ClassId
            );

            var responseData = new SupervisorAssignmentResponse
            {
                ClassId = assignment.ClassId,
                ClassName = assignment.Class.Name,
                SupervisorId = assignment.SupervisorId,
                SupervisorName = assignment.Supervisor.AppUser.DisplayName ?? "N/A",
                Message = "Supervisor removed from class successfully"
            };

            _logger.LogInformation("Supervisor {SupervisorId} removed from Class {ClassId} by {AdminId}",
                supervisorId, classId, adminUserId);

            return _response.Success(responseData, "Supervisor removed from class");
        }

        // ✅ Enroll في المؤسسة بس من غير كلاس
        public async Task<Response<EnrollChildResponse>> EnrollChildToInstitutionAsync(
            string adminUserId,
            string registrationCode)
        {
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == adminUserId);

            if (institution == null)
                return _response.NotFound<EnrollChildResponse>("No institution found for this admin");

            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.RegistrationCode == registrationCode && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<EnrollChildResponse>("Invalid registration code");

            if (child.InstitutionId != null && child.InstitutionId != institution.Id)
                return _response.Conflict<EnrollChildResponse>(
                    "Child is already enrolled in another institution");

            if (child.InstitutionId == institution.Id)
                return _response.Conflict<EnrollChildResponse>(
                    "Child is already enrolled in this institution");

            child.InstitutionId = institution.Id;
            child.UpdatedAt = DateTime.UtcNow;
            child.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Child {ChildId} enrolled in Institution {InstitutionId} by Admin {AdminId}",
                child.Id, institution.Id, adminUserId);

            return _response.Success(new EnrollChildResponse
            {
                ChildId = child.Id,
                ChildName = child.Name,
                ClassId = "",
                ClassName = "",
                InstitutionId = institution.Id,
                InstitutionName = institution.Name
            }, "Child enrolled in institution successfully");
        }

        // ✅ شيل من المؤسسة (وتلقائياً من الكلاس لو كان فيها)
        public async Task<Response<object>> RemoveChildFromInstitutionAsync(
            string adminUserId,
            string childId)
        {
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == adminUserId);

            if (institution == null)
                return _response.NotFound<object>("No institution found for this admin");

            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId
                                       && c.InstitutionId == institution.Id
                                       && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<object>("Child not found in this institution");

            // شيل من المؤسسة والكلاس تلقائياً
            child.InstitutionId = null;
            child.ClassId = null;
            child.UpdatedAt = DateTime.UtcNow;
            child.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Child {ChildId} removed from Institution {InstitutionId} by Admin {AdminId}",
                childId, institution.Id, adminUserId);

            return _response.Success<object>(null, "Child removed from institution successfully");
        }

        // ✅ Enroll في كلاس (الطفل لازم يكون في المؤسسة الأول)
        public async Task<Response<EnrollChildResponse>> EnrollChildToClassAsync(
            string adminUserId,
            string classId,
            EnrollChildToClassRequest request)
        {
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == adminUserId);

            if (institution == null)
                return _response.NotFound<EnrollChildResponse>("No institution found for this admin");

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == classId
                                       && c.InstitutionId == institution.Id
                                       && !c.IsDeleted);

            if (classEntity == null)
                return _response.NotFound<EnrollChildResponse>(
                    "Class not found or does not belong to your institution");

            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.RegistrationCode == request.RegistrationCode
                                       && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<EnrollChildResponse>("Invalid registration code");

            // لازم يكون في المؤسسة الأول
            if (child.InstitutionId != institution.Id)
                return _response.BadRequest<EnrollChildResponse>(
                    "Child must be enrolled in the institution first");

            if (child.ClassId != null)
                return _response.Conflict<EnrollChildResponse>(
                    "Child is already assigned to a class");

            child.ClassId = classId;
            child.UpdatedAt = DateTime.UtcNow;
            child.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();

            var supervisorUserIds = await _context.ClassSupervisors
                .Where(cs => cs.ClassId == classId && !cs.IsDeleted)
                .Select(cs => cs.Supervisor.AppUserId)
                .ToListAsync();

            foreach (var supervisorUserId in supervisorUserIds)
            {
                await _notificationService.SendAsync(
                    userId: supervisorUserId,
                    type: NotificationType.ChildEnrolledToClass,
                    title: "New Child Joined",
                    body: $"{child.Name} has been enrolled in your class '{classEntity.Name}'.",
                    relatedEntityId: classEntity.Id
                );
            }

            _logger.LogInformation(
                "Child {ChildId} enrolled in Class {ClassId} by Admin {AdminId}",
                child.Id, classId, adminUserId);

            return _response.Success(new EnrollChildResponse
            {
                ChildId = child.Id,
                ChildName = child.Name,
                ClassId = classEntity.Id,
                ClassName = classEntity.Name,
                InstitutionId = institution.Id,
                InstitutionName = institution.Name
            }, "Child enrolled in class successfully");
        }

        // ✅ شيل من الكلاس بس (يفضل في المؤسسة)
        public async Task<Response<object>> RemoveChildFromClassAsync(
            string adminUserId,
            string classId,
            string childId)
        {
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == adminUserId);

            if (institution == null)
                return _response.NotFound<object>("No institution found for this admin");

            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId
                                       && c.ClassId == classId
                                       && c.InstitutionId == institution.Id
                                       && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<object>("Child not found in this class");

            var classEntityForNotification = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == classId && !c.IsDeleted);

            var supervisorUserIds = await _context.ClassSupervisors
                 .Where(cs => cs.ClassId == classId && !cs.IsDeleted)
                 .Select(cs => cs.Supervisor.AppUserId)
                 .ToListAsync();


            // شيل من الكلاس بس، يفضل في المؤسسة
            child.ClassId = null;
            child.UpdatedAt = DateTime.UtcNow;
            child.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();

            foreach (var supervisorUserId in supervisorUserIds)
            {
                await _notificationService.SendAsync(
                    userId: supervisorUserId,
                    type: NotificationType.ChildRemovedFromClass,
                    title: "Child Removed",
                    body: $"{child.Name} has been removed from your class '{classEntityForNotification.Name}'.",
                    relatedEntityId: classEntityForNotification.Id
                );
            }

            _logger.LogInformation(
                "Child {ChildId} removed from Class {ClassId} by Admin {AdminId}",
                childId, classId, adminUserId);

            return _response.Success<object>(null, "Child removed from class successfully");
        }

        public async Task<Response<PaginatedList<InstitutionChildResponse>>> GetInstitutionChildrenAsync(
            string adminUserId,
            int pageNumber,
            int pageSize,
            string? search = null,
            string? classId = null)
        {
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == adminUserId);

            if (institution == null)
                return _response.NotFound<PaginatedList<InstitutionChildResponse>>(
                    "No institution found for this admin");

            var query = _context.Childrens
                .Include(c => c.Class)
                .Include(c => c.Level)
                .Where(c => c.InstitutionId == institution.Id && !c.IsDeleted);

            // فلتر بالاسم
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(term)
                                      || (c.NickName != null && c.NickName.ToLower().Contains(term)));
            }

            // فلتر بالكلاس
            if (!string.IsNullOrWhiteSpace(classId))
                query = query.Where(c => c.ClassId == classId);

            var totalCount = await query.CountAsync();

            var childEntities = await query
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var children = childEntities.Select(c => new InstitutionChildResponse
            {
                ChildId = c.Id,
                ChildName = c.Name,
                NickName = c.NickName,
                AvatarUrl = c.AvatarUrl,
                Age = c.Age,
                TotalPoints = c.TotalPoints,
                ClassId = c.ClassId,
                ClassName = c.Class?.Name,
                Level = c.Level != null ? new LevelInfo
                {
                    Id = c.Level.Id,
                    Name = c.Level.Name,
                    Order = c.Level.Order,
                    BadgeUrl = c.Level.BadgeUrl
                } : null
            }).ToList();

            var paginated = new PaginatedList<InstitutionChildResponse>(
                children, pageNumber, pageSize, totalCount);

            return _response.Success(paginated, "Institution children retrieved successfully");
        }
    }
}
