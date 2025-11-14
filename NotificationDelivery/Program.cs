using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using NotificationDelivery.Services;
using NotificationDelivery.Configuration;

// Configuración inicial de Serilog (para capturar logs durante el arranque)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

Log.Information("🚀 Iniciando NotificationDelivery Service...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Configuración de Serilog ---
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    // --- Configuración de Kestrel ---
    // Añadimos esta sección para escuchar en el puerto 8089 para health/metrics
    // además del puerto 8080 (definido por ENV ASPNETCORE_URLS en Dockerfile)
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(8089);
        Log.Information("👂 Escuchando en puerto adicional 8089 para Health/Metrics");
    });

    // --- Inyección de Dependencias (DI) ---

    // 1. Configuración de AppSettings
    builder.Services.Configure<RabbitMQSettings>(
        builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.Configure<EmailSettings>(
        builder.Configuration.GetSection("Smtp"));
    builder.Services.Configure<AppSettings>(
        builder.Configuration.GetSection("AppSettings"));

    // 2. Servicios
    builder.Services.AddSingleton<IEmailService, EmailService>();
    builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

    // 3. Hosted Services (Background Workers)
    builder.Services.AddHostedService<RabbitMQBackgroundService>();

    // 4. Health Checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();
    app.UseHttpMetrics();

    // --- Middlewares ---
    app.UseSerilogRequestLogging();

    // Mapeamos el Health Check para que responda en el puerto 8089
    app.MapHealthChecks("/health").RequireHost($"*:8089");

    app.MapMetrics("/metrics").RequireHost($"*:8089");

    app.MapGet("/", () =>
    {
        Log.Warning("Acceso a / (root) detectado");
        return Results.Ok(new { Status = "OK", Service = "NotificationDelivery" });
    });


    // --- Ejecutar la aplicación ---
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Host de NotificationDelivery terminado inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

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