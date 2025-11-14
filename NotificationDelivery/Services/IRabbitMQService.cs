namespace NotificationDelivery.Services
{
    public interface IRabbitMQService
    {
        Task StartConsumingAsync(CancellationToken cancellationToken);
        void Dispose();
    }
}