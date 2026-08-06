using Microsoft.AspNetCore.Identity.UI.Services;

namespace DevTrack.Web.Services
{
    public class DevTrackEmailSender : IEmailSender
    {
        private readonly ILogger<DevTrackEmailSender> _logger;

        public DevTrackEmailSender(ILogger<DevTrackEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation("EMAIL to {Email} | {Subject}\n{Message}", email, subject, htmlMessage);
            return Task.CompletedTask;
        }

        //public Task SendEmailAsync(string email, string subject, string htmlMessage)
        //{
        //    // No real email provider configured yet — this is a placeholder
        //    // so Identity's scaffolded Register/ForgotPassword pages don't crash.
        //    // Swap this out for a real provider (SendGrid, SMTP, etc.) later.

        //    // No operation performed. This is a placeholder implementation.
        //    return Task.CompletedTask;
        //}
    }
}
