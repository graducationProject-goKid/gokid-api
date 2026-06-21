using GoKidAPI.DTO.Adventures.Responses.GoKidAPI.Enums.Adventures;

namespace GoKidAPI.DTO.Adventures.Responses
{
    public class ChildWeeklyAdventureResponse
    {
        public string WeeklyAdventureId { get; set; } = null!;
        public string AdventureId { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string? DescriptionVoiceUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public int CurrentDay { get; set; }
        public List<ChildAdventureTaskItemResponse> Tasks { get; set; } = new();
    }
    public class ChildAdventureTaskItemResponse
    {
        public string AdventureTaskId { get; set; } = null!;
        public string TaskTemplateId { get; set; } = null!;
        public string? Type { get; set; }
        public int DayNumber { get; set; }
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public string? StoryText { get; set; }
        public string? StoryVoiceUrl { get; set; }
        public int Stars { get; set; }
        public ChildAdventureTaskAccessStatus AccessStatus { get; set; }

        // بيبقى فاضي لو Locked
        public ChildAdventureTaskSubmissionStatus? SubmissionStatus { get; set; }
        public string? EvidenceUrl { get; set; }
        public int EarnedStars { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }

    namespace GoKidAPI.Enums.Adventures
    {
        public enum ChildAdventureTaskAccessStatus
        {
            Locked = 1,       // اليوم لسه ما جاش
            Unlocked = 2,     // اليوم ده وينفع يعمل Submit
            Done = 3          // اتعمل Submit قبل كده
        }
    }

    // Enums/Adventures/ChildAdventureTaskSubmissionStatus.cs
    namespace GoKidAPI.Enums.Adventures
    {
        public enum ChildAdventureTaskSubmissionStatus
        {
            NotSubmitted = 1,   // اليوم فات ومعملش Submit
            Pending = 2,        // عمل Submit وفي انتظار Review
            Approved = 3,       // اتقبل
            Missed = 4          // اتعمل Missed بالـ Job
        }
    }

}
