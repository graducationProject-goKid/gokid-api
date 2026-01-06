namespace GoKidAPI.Services.OTP
{
    public interface IOtpService
    {
        Task<string> GenerateAndStoreOtpAsync(string userId, string operationType);
        Task<bool> ValidateOtpAsync(string userId, string otp, string operationType);

    }
}
