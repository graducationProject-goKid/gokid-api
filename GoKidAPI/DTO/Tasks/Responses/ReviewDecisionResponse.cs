namespace GoKidAPI.DTO.Tasks.Responses
{
    public class ReviewDecisionResponse
    {
        public string ChildTaskId { get; set; } = null!;
        public string Status { get; set; } = null!;  // "Completed" or "Rejected"
        public string Message { get; set; } = null!;
        public int? AwardedPoints { get; set; }  // لو تم قبولها
    }
}
