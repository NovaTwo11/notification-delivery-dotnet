using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NotificationDelivery.Configuration;
using NotificationDelivery.Services;

namespace NotificationDelivery.Services
{
    public class RabbitMQService : IRabbitMQService
    {
        private readonly ILogger<RabbitMQService> _logger;
        private readonly RabbitMQSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IModel? _channel;
        private const int MAX_RETRIES = 20;
        private const int RETRY_DELAY_SECONDS = 3;

        public RabbitMQService(
            ILogger<RabbitMQService> logger,
            IOptions<RabbitMQSettings> settings,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private void InitializeRabbitMQ()
        {
            var attempt = 1;

            while (attempt <= MAX_RETRIES)
            {
                try
                {
                    _logger.LogInformation($"🔄 Intentando conectar a RabbitMQ (intento {attempt}/{MAX_RETRIES})...");
                    _logger.LogInformation($"📍 Host: {_settings.Host}:{_settings.Port}");

                    var factory = new ConnectionFactory
                    {
                        HostName = _settings.Host,
                        Port = _settings.Port,
                        UserName = _settings.Username,
                        Password = _settings.Password,
                        AutomaticRecoveryEnabled = true,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                        RequestedHeartbeat = TimeSpan.FromSeconds(60)
                    };

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    // Declarar cola durable
                    _channel.QueueDeclare(
                        queue: _settings.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );

                    _logger.LogInformation("✅ Conexión a RabbitMQ establecida exitosamente");
                    _logger.LogInformation($"👂 Escuchando en cola: {_settings.QueueName}");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error conectando a RabbitMQ (intento {attempt}/{MAX_RETRIES}): {ex.Message}");

                    if (attempt == MAX_RETRIES)
                    {
                        _logger.LogCritical("🔥 No se pudo conectar después de múltiples intentos. Abortando.");
                        throw new Exception($"Failed to connect to RabbitMQ after {MAX_RETRIES} attempts", ex);
                    }

                    _logger.LogInformation($"⏳ Reintentando en {RETRY_DELAY_SECONDS} segundos...");
                    Thread.Sleep(TimeSpan.FromSeconds(RETRY_DELAY_SECONDS));
                    attempt++;
                }
            }
        }

        public async Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            // Inicializar conexión con reintentos
            InitializeRabbitMQ();

            if (_channel == null)
            {
                throw new InvalidOperationException("Channel no inicializado");
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = System.Text.Encoding.UTF8.GetString(body);

                    _logger.LogInformation($"📩 Mensaje recibido: {message}");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        await ProcessMessageAsync(message, emailService);
                    }

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogDebug("✅ Mensaje procesado y confirmado");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error procesando mensaje: {ex.Message}");
                    _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("✅ Consumidor iniciado correctamente");

            // Mantener vivo hasta que se cancele
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("⚠️ Consumo cancelado");
            }
        }

        private async Task ProcessMessageAsync(string message, IEmailService emailService)
        {
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
            try
            {
                _channel?.Close();
                _connection?.Close();
                _logger.LogInformation("👋 Conexión RabbitMQ cerrada");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cerrando conexión: {ex.Message}");
            }
        }
    }
}