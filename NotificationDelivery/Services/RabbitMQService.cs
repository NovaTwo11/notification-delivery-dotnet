using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NotificationDelivery.Configuration;
using Microsoft.Extensions.Options;

namespace NotificationDelivery.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly ILogger<RabbitMQService> _logger;
        private readonly RabbitMQSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQService(
            ILogger<RabbitMQService> logger,
            IOptions<RabbitMQSettings> settings,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _logger.LogInformation($"✅ Conectado a RabbitMQ - Cola: {_settings.QueueName}");
        }

        public Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("RabbitMQ channel no inicializado");
            }

            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogInformation($"📩 Mensaje recibido: {message}");

                    await ProcessMessageAsync(message);

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error procesando mensaje");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation($"👂 Escuchando mensajes en cola: {_settings.QueueName}");

            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(string message)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var json = JObject.Parse(message);
            var type = json["type"]?.ToString()?.ToLower();
            var email = json["email"]?.ToString();
            var userName = json["userName"]?.ToString() ?? "Usuario";

            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("⚠️ Mensaje sin type o email");
                return;
            }

            _logger.LogInformation($"🎯 Procesando notificación: {type} para {email}");

            switch (type)
            {
                case "user_welcome":
                    var activationToken = json["additionalData"]?["activationToken"]?.ToString();
                    await emailService.SendWelcomeEmailAsync(email, userName, activationToken);
                    break;

                case "login_notification":
                    var additionalData = json["additionalData"]?.ToObject<Dictionary<string, object>>();
                    await emailService.SendLoginNotificationAsync(email, userName, additionalData);
                    break;

                case "password_reset":
                    var resetToken = json["additionalData"]?["resetToken"]?.ToString() ?? "";
                    await emailService.SendPasswordResetEmailAsync(email, userName, resetToken);
                    break;

                case "password_updated":
                    await emailService.SendPasswordUpdatedConfirmationAsync(email, userName);
                    break;

                default:
                    _logger.LogWarning($"⚠️ Tipo de notificación desconocido: {type}");
                    break;
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("👋 Conexión RabbitMQ cerrada");
        }
    }
}