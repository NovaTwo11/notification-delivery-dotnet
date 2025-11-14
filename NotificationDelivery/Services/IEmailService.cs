namespace NotificationDelivery.Services
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string userName, string? activationToken = null);
        Task SendLoginNotificationAsync(string toEmail, string userName, Dictionary<string, object>? additionalData);
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
        Task SendPasswordUpdatedConfirmationAsync(string toEmail, string userName);
    }
}