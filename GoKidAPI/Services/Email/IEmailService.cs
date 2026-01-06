using GoKidAPI.Entity.Account.Identity;

namespace GoKidAPI.Services.Email
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(AppUser user, string otp);
        Task SendResetPasswordEmailAsync(string recipientEmail, string subject, string userName, string otpOrLink, bool isOtp);
        Task SendConfirmationEmailAsync(string recipientEmail, string subject, string userName, string otpOrLink, bool isOtp);
        Task SendPasswordChangedEmailAsync(string recipientEmail, string userName);

    }
}
