namespace GoKidAPI.DTO.Account.Auth.Requests
{
    public class ChangeEmailRequest
    {
        public string UserId { get; set; }
        public string OldPassword { get; set; }
        public string NewEmail { get; set; }
        public string ConfirmEmail { get; set; }
    }
}
