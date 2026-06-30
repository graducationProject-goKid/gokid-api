namespace GoKidAPI.Enums.Adventures
{
    public enum WeeklyAdventureStatus
    {
        Active = 1,
        Completed = 2,
        Expired = 3,
        Inactive = 4 // Inactive means the adventure is not currently available for the child, possibly due to being deactivated by the institution or not yet started.
    }
}
