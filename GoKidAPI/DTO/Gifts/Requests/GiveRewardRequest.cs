using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Gifts.Requests
{
    public class GiveRewardRequest
    {
        [StringLength(500, ErrorMessage = "Message can't exceed 500 characters.")]
        public string? MessageToChild { get; set; }
    }
}
