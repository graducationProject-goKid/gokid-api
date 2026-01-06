using System.ComponentModel.DataAnnotations;

using GoKidAPI.Enums;

namespace GoKidAPI.DTO.Account.Auth.Requests
{
    public class LoginRequest
    {
        public string Identifier { get; set; } = null!;     // Email or child registration code
        public string? Password { get; set; }               // required only for Parent/Institution Staff
        public UserType LoginAs { get; set; }
    }
}
