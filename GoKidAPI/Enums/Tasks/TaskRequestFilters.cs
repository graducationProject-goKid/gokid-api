using GoKidAPI.Shared;

namespace GoKidAPI.Enums.Tasks
{
    public class TaskRequestFilters :RequestFilters<TaskSortingColumn>
    {
        public TaskTemplateType? TemplateType { get; set; }

        public DifficultyLevel? Difficulty { get; set; }
        public Guid? SubCategoryId { get; set; }
    }
}
