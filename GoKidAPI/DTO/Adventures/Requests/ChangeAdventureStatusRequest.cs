using System.ComponentModel.DataAnnotations;

using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.DTO.Adventures.Requests
{
    public class ChangeAdventureStatusRequest
    {
        [Required]
        public AdventureStatus Status { get; set; }
    }
}
