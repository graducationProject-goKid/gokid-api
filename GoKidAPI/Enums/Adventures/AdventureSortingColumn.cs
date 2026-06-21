namespace GoKidAPI.Enums.Adventures
{
    public enum AdventureSortingColumn
    {
        /// <summary>
        /// Sort by Adventure Title (A-Z or Z-A)
        /// </summary>
        Title = 1,

        /// <summary>
        /// Sort by Bonus Points (Low to High or High to Low)
        /// </summary>
        BonusPoints = 2,

        /// <summary>
        /// Sort by Week Duration (Short to Long or Long to Short)
        /// </summary>
        WeekDuration = 3,

        /// <summary>
        /// Sort by Creation Date (Oldest to Newest or Newest to Oldest)
        /// </summary>
        CreatedAt = 4,

        /// <summary>
        /// Sort by Status (Active first or Inactive first)
        /// </summary>
        Status = 5,

        /// <summary>
        /// Sort by Number of Tasks in the Adventure
        /// </summary>
        TasksCount = 6
    }
}
