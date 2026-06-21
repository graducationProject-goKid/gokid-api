using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Classes.Requests
{
    public class AssignSupervisorToClassRequest
    {
        [Required]
        public string SupervisorId { get; set; } = null!;
    }
}
