namespace GoKidAPI.Enums
{
    public enum NotificationType : byte
    {
        TaskAssigned = 0,
        TaskStarted = 1,
        ReviewRequested = 2,
        TaskApproved = 3,
        TaskRejected = 4,
        PointsEarned = 5,
        AdventureStarted = 6,
        WeekBonus = 7,
        ChildLinked = 8,

        GiftPurchased  = 9,
        RewardGiven    = 10,
        AdventureNewDay = 11,

        SupervisorAssignedToClass = 12,
        SupervisorUnassignedFromClass = 13,
        ChildEnrolledToClass = 14, // Type send to supervisor when a child is enrolled to their class
        ChildRemovedFromClass = 15, // Type send to supervisor when a child is removed from their class

        WeeklyAdventureStarted = 16,
        DailyAdventureTasksAssigned = 17, // Send to supervisor when daily adventure tasks are assigned to children
        AdventureDayCompleted = 18, // Send to supervisor when a childs(adventureDay) completes a day in the adventure

        LevelUp = 19, // Sent to child, parent, and class supervisors when the child reaches a new level
        
        ChildSubmittedTask = 20,
    }
}
