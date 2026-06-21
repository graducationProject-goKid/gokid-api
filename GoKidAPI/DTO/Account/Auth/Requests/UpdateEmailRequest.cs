namespace GoKidAPI.DTO.Account.Auth.Requests
{
    public class UpdateEmailRequest
    {
        public string UserId { get; set; }

        public string NewEmail { get; set; }

        public string Otp { get; set; }

    }
}
