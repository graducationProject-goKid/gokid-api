// Services/Supervisor/ISupervisorService.cs
using GoKidAPI.DTO.Childs.Responses;
using GoKidAPI.DTO.Supervisor.Requests;
using GoKidAPI.DTO.Supervisor.Responses;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Supervisor
{
    public interface ISupervisorService
    {
        /// <summary>
        /// جيب كل الـ Adventures المفعلة على الكلاسات بتاعت الـ Supervisor
        /// </summary>
        Task<Response<List<SupervisorAdventureListResponse>>> GetMyAdventuresAsync(string supervisorUserId);

        /// <summary>
        /// جيب تاسكات الأطفال في adventure معينة (مع فلتر بالـ status)
        /// </summary>
        Task<Response<PaginatedList<ChildAdventureTaskReviewResponse>>> GetAdventureChildTasksAsync(
            string supervisorUserId,
            string weeklyAdventureId,
            AdventureChildTaskStatus? statusFilter,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Approve أو Reject تاسك طفل معين
        /// </summary>
        Task<Response<ChildAdventureTaskReviewResponse>> ReviewChildTaskAsync(
            string supervisorUserId,
            string childAdventureTaskId,
            ReviewAdventureTaskRequest request);

        /// <summary>
        /// جيب كل الأطفال في كلاس معين داخل adventure معينة مع ملخص تقدمهم
        /// </summary>
        Task<Response<List<ClassChildrenListResponse>>> GetClassChildrenProgressAsync(
            string supervisorUserId,
            string weeklyAdventureId,
            string classId);

        /// <summary>
        /// جيب كل تاسكات طفل معين في adventure معينة بالتفاصيل
        /// </summary>
        Task<Response<ChildAdventureHistoryResponse>> GetChildAdventureHistoryAsync(
            string supervisorUserId,
            string weeklyAdventureId,
            string childId);

        Task<Response<List<SupervisorClassResponse>>> GetMyClassesAsync(string supervisorUserId);
    }
}