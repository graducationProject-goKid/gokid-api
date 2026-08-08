using GoKidAPI.Shared;

namespace GoKidAPI.Enums.Tasks
{
    public class TaskRequestFilters :RequestFilters<TaskSortingColumn>
    {
        public TaskTemplateType? TemplateType { get; set; }

        public DifficultyLevel? Difficulty { get; set; }
        public Guid? SubCategoryId { get; set; }

        // Manually supplied by the caller (e.g. an Institution Admin building an Adventure)
        // to filter tasks whose recommended age range covers this age.
        public int? RecommendedAge { get; set; }
    }
}
