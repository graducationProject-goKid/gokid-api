namespace GoKidAPI.InfrastructreManage.Options
{
    public class JwtOptions
    {
        public string ValidIssuer { get; set; } = string.Empty;
        public string ValidAudience { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
    }
}
