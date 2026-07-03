using System.Security.Cryptography;

using QRCoder;

namespace GoKidAPI.Helpers
{
    public class IWWHelper
    {
        public static string Random(int count, string? chars = null)
        {
            chars ??= "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new char[count];
            for (int i = 0; i < count; i++)
                result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            return new string(result);
        }
        public static string GenerateQrCodeBase64(string code)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
            var pngQrCode = new PngByteQRCode(qrData);
            var qrBytes = pngQrCode.GetGraphic(20);

            return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
        }
        
        public static Guid ParseStringToGuid (string expr)
        {
            Guid.TryParse(expr, out var id);
            return id;
        }
    }
}
