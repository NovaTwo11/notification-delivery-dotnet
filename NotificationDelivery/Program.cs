using NotificationDelivery.Configuration;
using NotificationDelivery.Services;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// Configurar servicios
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));

builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Hosted Service para iniciar el consumidor
builder.Services.AddHostedService<RabbitMQBackgroundService>();

var app = builder.Build();

app.MapControllers();

app.Run();

// Background Service para RabbitMQ
public class RabbitMQBackgroundService : BackgroundService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<RabbitMQBackgroundService> _logger;

    public RabbitMQBackgroundService(
        IRabbitMQService rabbitMQService,
        ILogger<RabbitMQBackgroundService> logger)
    {
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Iniciando consumidor RabbitMQ...");
        await _rabbitMQService.StartConsumingAsync(stoppingToken);
    }
}