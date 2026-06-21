using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Classes.Requests
{
    public class UpdateClassRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = null!;
    }
}
