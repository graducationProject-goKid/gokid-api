namespace GoKidAPI.Enums.Tasks
{
    public enum TaskSource : byte
    {
        // That means who assigned the task to the child (we said that all users use an predefined set of tasks)
        // So, tasks can be assigned either by the system (general tasks), by the parent or by the institution (adventure tasks)
        SystemGeneral = 0,
        Parent = 1,
        InstitutionAdventure = 2
    }
}
