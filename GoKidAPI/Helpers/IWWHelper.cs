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
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new BitmapByteQRCode(qrCodeData);
            var bitmapBytes = qrCode.GetGraphic(20);

            return $"data:image/png;base64,{Convert.ToBase64String(bitmapBytes)}";
        }
        
        public static Guid ParseStringToGuid (string expr)
        {
            Guid.TryParse(expr, out var id);
            return id;
        }
    }
}
