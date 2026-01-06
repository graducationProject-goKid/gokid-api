using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Account.Auth.Requests
{
    public class VerifyOtpRequest
    {
        public string Email { get; set; } = null!;
        public string Otp { get; set; } = null!;
    }
}
