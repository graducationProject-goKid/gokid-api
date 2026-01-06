namespace GoKidAPI.Enums.Tasks
{
    public enum TaskStatus : byte
    {
        Pending = 0,
        InProgress = 1, // When child starts the task
        ReviewRequested = 2, // When child marks task as done and requests review
        Completed = 3, // When parent/institution approves the task
        Rejected = 4 // When parent/institution rejects the task
    }
}
