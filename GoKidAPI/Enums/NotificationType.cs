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
    }
}
