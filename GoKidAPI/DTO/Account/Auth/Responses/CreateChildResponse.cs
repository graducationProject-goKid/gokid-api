namespace GoKidAPI.DTO.Account.Auth.Responses
{
    public class CreateChildResponse
    {
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string RegistrationCode { get; set; } = null!;
        public string QrCodeBase64 { get; set; } = null!; // صورة QR كـ base64
        public string DeepLink => $"gokid://register-child?code={RegistrationCode}"; // للـ mobile apps
    }
}
