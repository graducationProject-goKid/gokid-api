namespace GoKidAPI.DTO.Supervisor.Responses
{
    public class SupervisorAdventureListResponse
    {
        public string WeeklyAdventureId { get; set; } = null!;
        public string AdventureId { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string ClassId { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalChildren { get; set; }
        public int PendingReviewsCount { get; set; }
    }
}
