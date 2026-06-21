using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

using FluentEmail.Core;

using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.InfrastructreManage.Options;

using Microsoft.Extensions.Options;

namespace GoKidAPI.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IFluentEmail _fluentEmail;
        private readonly EmailOptions _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> settings, IFluentEmail fluentEmail, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _fluentEmail = fluentEmail;
            _logger = logger;
        }

        /// Helper method to build a pre-configured FluentEmail instance.
        private IFluentEmail BuildFluentEmail(string to, string subject, string bodyHtml)
        {
            // Build a new SMTP client with the config we get based on the coming type
            var smtpClient = new SmtpClient(_settings.SmtpServer)
            {
                Port = _settings.SmtpPort,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };

            var sender = new FluentEmail.Smtp.SmtpSender(() => smtpClient);

            FluentEmail.Core.Email.DefaultSender = sender; // Set the default sender globally

            return _fluentEmail
                .SetFrom(_settings.FromEmail, _settings.FromName)
                .To(to)
                .Subject($"{subject} | GO-KID.com")
                .Body(bodyHtml, true);
        }

        /// This method responsible for email sending, rest of emails methods responsible for => build the body + subject, then call SendEmailAsync.
        public async Task SendEmailAsync(string[] recipientsEmails, string subject, string htmlMessage)
        {
            foreach (var recipientEmail in recipientsEmails)
            {
                const int maxRetries = 3;
                int attempt = 0;
                bool sent = false;

                while (attempt < maxRetries && !sent)
                {
                    attempt++;

                    try
                    {
                        var email = BuildFluentEmail(recipientEmail, subject, htmlMessage);
                        var response = await email.SendAsync();

                        if (response.Successful)
                        {
                            _logger.LogInformation("Email delivered successfully to {Email} on attempt {Attempt}", recipientEmail, attempt);
                            sent = true;
                        }
                        else
                        {
                            _logger.LogWarning("Attempt {Attempt} to send email to {Email} failed. Errors: {Errors}", attempt, recipientEmail, string.Join(", ", response.ErrorMessages));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error while sending email to {Email} on attempt {Attempt}", recipientEmail, attempt);
                    }

                    if (!sent && attempt < maxRetries)
                    {
                        // wait 5 seconds before retry
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }

                if (!sent)
                {
                    _logger.LogError("Failed to deliver email to {Email} after {MaxRetries} attempts", recipientEmail, maxRetries);
                }
            }
        }
        public async Task SendChangeEmailEmailAsync(string recipientEmail, string userName, string otpOrLink, bool isOtp)
        {
            // Load template
            var template = LoadTemplate("change-email.html");

            // Determine method text
            var methodText = isOtp ? "OTP code" : "link";

            // Replace placeholders
            template = template.Replace("{{UserName}}", userName)
                               .Replace("{{Method}}", methodText)
                               .Replace("{{Value}}", otpOrLink);
            // Send using NoReply type
            await SendEmailAsync(new[] { recipientEmail }, "Change Your GO-KID Email", template);
        }
        public async Task SendResetPasswordEmailAsync(string recipientEmail, string subject, string userName, string otpOrLink, bool isOtp)
        {
            // Load template
            var template = LoadTemplate("reset-password.html");

            // Determine method text
            var methodText = isOtp ? "OTP code" : "link";

            // Replace placeholders
            template = template.Replace("{{UserName}}", userName)
                               .Replace("{{Method}}", methodText)
                               .Replace("{{Value}}", otpOrLink);

            // Send using NoReply type (or create a new type for Reset if needed)
            await SendEmailAsync(new[] { recipientEmail }, subject, template);
        }
        public async Task SendPasswordChangedEmailAsync(string recipientEmail, string userName)
        {
            var template = LoadTemplate("change-password.html");

            template = template.Replace("{{UserName}}", userName)
                               .Replace("{{Year}}", DateTime.Now.Year.ToString());

            await SendEmailAsync(new[] { recipientEmail }, "Your GO-KID Password Has Been Changed", template);
        }
        public async Task SendOtpEmailAsync(AppUser user, string otp)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Attempted to send OTP email but user or email is invalid.");
                return;
            }

            try
            {
                var template = LoadTemplate("resend-otp.html");

                template = template.Replace("{{UserName}}", user.UserName ?? user.Email ?? "User")
                                   .Replace("{{Otp}}", otp)
                                   .Replace("{{CurrentYear}}",
                                   DateTime.UtcNow.Year.ToString());

                await SendEmailAsync(new[] { user.Email }, "OTP Verification Code", template);

                _logger.LogInformation("OTP email sent successfully to {Email}", user.Email);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "OTP email template not found while sending email to {Email}", user.Email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending OTP email to {Email}", user.Email);
                throw;
            }
        }
        public async Task SendConfirmationEmailAsync(string recipientEmail, string subject, string userName, string otpOrLink, bool isOtp)
        {
            var template = LoadTemplate("account-confirmation.html");
            var methodText = isOtp ? "OTP code" : "link";

            template = template.Replace("{{UserName}}", userName)
                               .Replace("{{Method}}", methodText)
                               .Replace("{{Value}}", otpOrLink);

            await SendEmailAsync(new[] { recipientEmail }, subject, template);
        }


        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                return false;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public async Task SendSupervisorCredentialsAsync(string recipientEmail, string fullName, string email, string password, string loginUrl)
        {
            var template = LoadTemplate("supervisor-credentials.html");

            template = template
                .Replace("{{FullName}}", fullName)
                .Replace("{{Email}}", email)
                .Replace("{{Password}}", password)
                .Replace("{{LoginUrl}}", loginUrl);

            await SendEmailAsync(new[] { recipientEmail }, "Welcome to GO-KID – Your Supervisor Access Details", template);
        }

        










        private string LoadTemplate(string templateName)
        {
            var rootPath = Directory.GetCurrentDirectory();
            var templatePath = Path.Combine(rootPath, "wwwroot", "EmailTemplates", templateName);

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template {templateName} not found at {templatePath}");

            return File.ReadAllText(templatePath);
        }
    }
}
