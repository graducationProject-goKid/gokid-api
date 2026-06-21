using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Supervisor.Requests
{
    public class ReviewAdventureTaskRequest
    {
        [Required]
        public bool IsApproved { get; set; }

        public string? RejectionReason { get; set; }
    }
}
