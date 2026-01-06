using System.ComponentModel.DataAnnotations;

using GoKidAPI.Enums;

namespace GoKidAPI.DTO.Account.Auth.Requests
{
    public class CreateChildRequest
    {
        [Required] 
        public string Name { get; set; } = null!;
        public string? NickName { get; set; }
        [Range(3, 16)] 
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public Relationship RelationshipToParent { get; set; } = Relationship.Father;
        public IFormFile? Avatar { get; set; }
    }
}
