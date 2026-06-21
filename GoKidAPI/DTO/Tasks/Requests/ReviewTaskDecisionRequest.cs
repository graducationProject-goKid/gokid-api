using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Tasks.Requests
{
    public class ReviewTaskDecisionRequest
    {
        [Required]
        public string ChildTaskId { get; set; } = null!;

        [Required]
        public bool IsApproved { get; set; }  // true = Accept, false = Reject

        public string? RejectionReason { get; set; }  // مطلوب لو IsApproved = false
        public string? AcceptanceMessage { get; set; }
    }
}
