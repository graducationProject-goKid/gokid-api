namespace GoKidAPI.InfrastructreManage.Options
{
    public class EmailOptions
    {
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "GO-KID";
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
    }
}
