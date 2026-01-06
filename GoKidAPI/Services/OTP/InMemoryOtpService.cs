using System.Security.Cryptography;

using Microsoft.Extensions.Caching.Memory;

namespace GoKidAPI.Services.OTP
{
    public class InMemoryOtpService : IOtpService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<InMemoryOtpService> _logger;

        public InMemoryOtpService(IMemoryCache memoryCache, ILogger<InMemoryOtpService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public Task<string> GenerateAndStoreOtpAsync(string userId, string operationType)
        {
            var otp = GenerateOtp();

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            };

            _memoryCache.Set($"otp:{userId}:{operationType.ToLower()}", otp, cacheEntryOptions);
            _logger.LogInformation("OTP generated and stored in memory for UserId: {UserId}", userId);

            return Task.FromResult(otp);
        }


        public Task<bool> ValidateOtpAsync(string userId, string otp, string operationType)
        {
            var key = $"otp:{userId}:{operationType.ToLower()}";

            if (_memoryCache.TryGetValue(key, out string? storedOtp))
            {
                if (storedOtp == otp)
                {
                    _memoryCache.Remove($"otp:{userId}");
                    _logger.LogInformation("OTP validated successfully for UserId: {UserId}", userId);
                    return Task.FromResult(true);
                }

                _logger.LogWarning("OTP validation failed: Invalid OTP for UserId: {UserId}", userId);
                return Task.FromResult(false);
            }

            _logger.LogWarning("OTP validation failed: No OTP found or expired for UserId: {UserId}", userId);
            return Task.FromResult(false);
        }


        private string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            uint raw = BitConverter.ToUInt32(bytes, 0);
            uint otp = raw % 1_000_000;

            return otp.ToString("D6");
        }
    }
}
