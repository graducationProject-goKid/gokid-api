using GoKidAPI.Shared;

namespace GoKidAPI.Enums.Tasks
{
    public class GetChildTasksFilters : RequestFilters<TaskSortingColumn>
    {
        /// <summary>
        /// Optional: Filter by specific status (null = all statuses)
        /// </summary>
        public TaskStatus? Status { get; set; }
    }
}
