using System.Drawing;
using System.Drawing.Imaging;

using QRCoder;

namespace GoKidAPI.Helpers
{
    public class IWWHelper
    {
        public static string Random(int count, String chars = null)
        {
            if (chars == null)
                chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random Random_Holder = new Random();
            return new string(Enumerable.Repeat(chars, count).Select(s => s[Random_Holder.Next(s.Length)]).ToArray());
        }
        public static string GenerateQrCodeBase64(string code)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);
            using Bitmap qrImage = qrCode.GetGraphic(20);

            using var ms = new MemoryStream();
            qrImage.Save(ms, ImageFormat.Png);

            var base64 = Convert.ToBase64String(ms.ToArray());

            return $"data:image/png;base64,{base64}";
        }
        
        public static Guid ParseStringToGuid (string expr)
        {
            Guid.TryParse(expr, out var id);
            return id;
        }
    }
}
