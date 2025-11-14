namespace NotificationDelivery.Configuration
{
    public class RabbitMQSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string QueueName { get; set; } = "notifications.delivery";
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "localhost";
        public int SmtpPort { get; set; } = 1025;
        public string From { get; set; } = "noreply@app.com";
        public string FromName { get; set; } = "App";
    }

    public class AppSettings
    {
        public string Name { get; set; } = "App";
        public string BackendUrl { get; set; } = "http://localhost:8080";
        public string SupportEmail { get; set; } = "support@app.com";
    }
}