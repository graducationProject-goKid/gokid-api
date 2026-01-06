using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Account.Auth.Requests
{
    public class RegisterParentRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
        public string FullName { get; set; } = null!;
    }
}
