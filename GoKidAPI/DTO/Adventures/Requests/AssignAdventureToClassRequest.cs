using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Adventures.Requests
{
    public class AssignAdventureToClassRequest
    {
        [Required]
        public string AdventureId { get; set; } = null!;

        [Required]
        public string ClassId { get; set; } = null!;

        [Required]
        public DateTime StartDate { get; set; }
    }
}
