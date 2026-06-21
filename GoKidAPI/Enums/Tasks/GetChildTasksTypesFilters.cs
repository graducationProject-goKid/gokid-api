using GoKidAPI.Shared;

namespace GoKidAPI.Enums.Tasks
{
    // This is the filter used in the child portal to get tasks based on their source (General, Parent, Institution) and optionally filter by date (default = today).
    public class GetChildTasksTypesFilters : RequestFilters<TaskSortingColumn>
    {
        public TaskSourceTab SourceFilter { get; set; } = TaskSourceTab.All;

        public DateTime? Date { get; set; } // اليوم اللي عايز التاسكات بتاعته (default = today)
    }
}
