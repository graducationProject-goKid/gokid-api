namespace GoKidAPI.DTO.Account.Auth.Requests
{
    public class ResetPasswordRequest
    {
        public string UserId { get; set; }
        public string Otp { get; set; }
        public string Token { get; set; } // Used for link-based verification
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
