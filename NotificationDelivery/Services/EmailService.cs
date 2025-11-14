using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using NotificationDelivery.Configuration;

namespace NotificationDelivery.Services
{
    /// <summary>
    /// Servicio para envío de correos electrónicos usando MailKit.
    /// Soporta diferentes tipos de notificaciones: bienvenida, login, password reset, etc.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;
        private readonly AppSettings _appSettings;

        public EmailService(
            ILogger<EmailService> logger,
            IOptions<EmailSettings> emailSettings,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailSettings = emailSettings?.Value ?? throw new ArgumentNullException(nameof(emailSettings));
            _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Envía email de bienvenida al usuario.
        /// </summary>
        public async Task SendWelcomeEmailAsync(string toEmail, string userName, string? activationToken = null)
        {
            var subject = $"¡Bienvenido a {_appSettings.Name}! 🎉";
            
            var body = activationToken != null
                ? BuildWelcomeEmailWithActivation(userName, activationToken)
                : BuildSimpleWelcomeEmail(userName);

            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Envía notificación de inicio de sesión.
        /// </summary>
        public async Task SendLoginNotificationAsync(
            string toEmail, 
            string userName, 
            Dictionary<string, object>? additionalData)
        {
            var subject = $"🔐 Nuevo inicio de sesión - {_appSettings.Name}";
            
            var ipAddress = additionalData?.GetValueOrDefault("ipAddress")?.ToString() ?? "Desconocida";
            var deviceInfo = additionalData?.GetValueOrDefault("deviceInfo")?.ToString() ?? "Desconocido";
            var userAgent = additionalData?.GetValueOrDefault("userAgent")?.ToString() ?? "Desconocido";
            var location = additionalData?.GetValueOrDefault("location")?.ToString() ?? "Desconocida";
            var timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            var body = $@"Hola {userName},

Se ha detectado un nuevo inicio de sesión en tu cuenta de {_appSettings.Name}.

📊 Detalles del inicio de sesión:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Fecha y hora: {timestamp}
- Dirección IP: {ipAddress}
- Ubicación: {location}
- Dispositivo: {deviceInfo}
- Navegador: {userAgent}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️ IMPORTANTE:
Si NO fuiste tú quien inició sesión:
1. Cambia tu contraseña INMEDIATAMENTE
2. Revisa la actividad reciente de tu cuenta
3. Contacta a soporte de inmediato

Puedes cambiar tu contraseña aquí:
{_appSettings.BackendUrl}/api/password/forgot

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Equipo de Seguridad de {_appSettings.Name}
📧 Soporte: {_appSettings.SupportEmail}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Este es un correo automático, por favor no respondas a este mensaje.";

            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Envía email con link para restablecer contraseña.
        /// </summary>
        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
        {
            if (string.IsNullOrEmpty(resetToken))
            {
                _logger.LogWarning($"⚠️ Token de reset vacío para {toEmail}");
                throw new ArgumentException("Reset token no puede estar vacío", nameof(resetToken));
            }

            var subject = $"🔑 Restablecimiento de contraseña - {_appSettings.Name}";
            var resetUrl = $"{_appSettings.BackendUrl}/api/password/reset?token={resetToken}";
            
            var body = $@"Hola {userName},

Hemos recibido una solicitud para restablecer tu contraseña en {_appSettings.Name}.

🔐 Para restablecer tu contraseña, haz clic en el siguiente enlace:

{resetUrl}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⏰ IMPORTANTE: Este enlace expirará en 1 hora
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Si NO solicitaste este cambio:
- Ignora este correo
- Tu contraseña permanecerá sin cambios
- Considera cambiar tu contraseña por seguridad

Por tu seguridad, asegúrate de:
✓ Usar una contraseña única y fuerte
✓ No compartir tu contraseña con nadie
✓ Habilitar autenticación de dos factores

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Equipo de Seguridad de {_appSettings.Name}
📧 Soporte: {_appSettings.SupportEmail}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Este es un correo automático, por favor no respondas a este mensaje.";

            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Envía confirmación de que la contraseña fue actualizada.
        /// </summary>
        public async Task SendPasswordUpdatedConfirmationAsync(string toEmail, string userName)
        {
            var subject = $"✅ Contraseña actualizada - {_appSettings.Name}";
            var timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            
            var body = $@"Hola {userName},

✅ Tu contraseña en {_appSettings.Name} ha sido actualizada exitosamente.

📊 Detalles del cambio:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Fecha y hora: {timestamp}
- Acción: Contraseña actualizada
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️ IMPORTANTE:
Si NO realizaste este cambio, tu cuenta puede estar comprometida.

🚨 Acciones inmediatas:
1. Contacta a soporte AHORA: {_appSettings.SupportEmail}
2. Verifica la actividad reciente de tu cuenta
3. Cambia tu contraseña nuevamente

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Puedes iniciar sesión con tu nueva contraseña en:
{_appSettings.BackendUrl}/auth/login

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Equipo de Seguridad de {_appSettings.Name}
📧 Soporte: {_appSettings.SupportEmail}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Este es un correo automático, por favor no respondas a este mensaje.";

            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Método privado para envío real del correo usando MailKit.
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                _logger.LogInformation($"📧 Preparando envío de email a: {toEmail}");
                
                // Crear mensaje
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.From));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                
                // Cuerpo del mensaje en texto plano
                message.Body = new TextPart("plain")
                {
                    Text = body
                };

                // Conectar y enviar
                using var client = new SmtpClient();
                
                // Conectar al servidor SMTP (sin SSL para MailDev)
                await client.ConnectAsync(
                    _emailSettings.SmtpHost, 
                    _emailSettings.SmtpPort, 
                    false
                );

                // MailDev no requiere autenticación
                // Si usaras un servidor SMTP real, aquí irían las credenciales:
                // await client.AuthenticateAsync(username, password);

                // Enviar mensaje
                await client.SendAsync(message);
                
                // Desconectar
                await client.DisconnectAsync(true);

                _logger.LogInformation($"✅ Email enviado exitosamente a: {toEmail} - Asunto: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error enviando email a: {toEmail}");
                throw new InvalidOperationException($"Error enviando email a {toEmail}", ex);
            }
        }

        /// <summary>
        /// Construye el cuerpo del email de bienvenida con activación.
        /// </summary>
        private string BuildWelcomeEmailWithActivation(string userName, string activationToken)
        {
            var activationUrl = $"{_appSettings.BackendUrl}/api/auth/activate?token={activationToken}";
            
            return $@"¡Hola {userName}! 👋

¡Bienvenido a {_appSettings.Name}!

Tu cuenta ha sido creada exitosamente. Para comenzar a usar todos nuestros servicios, 
necesitas activar tu cuenta.

🔐 Activa tu cuenta:
Haz clic en el siguiente enlace para activar tu cuenta:

{activationUrl}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⏰ Este enlace expirará en 24 horas
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Una vez activada tu cuenta, podrás:
✓ Acceder a todas las funcionalidades
✓ Personalizar tu perfil
✓ Conectar con otros usuarios
✓ Y mucho más...

Si tienes alguna pregunta o necesitas ayuda, no dudes en contactarnos.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Equipo de {_appSettings.Name}
📧 Soporte: {_appSettings.SupportEmail}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Este es un correo automático, por favor no respondas a este mensaje.";
        }

        /// <summary>
        /// Construye el cuerpo del email de bienvenida simple.
        /// </summary>
        private string BuildSimpleWelcomeEmail(string userName)
        {
            return $@"¡Hola {userName}! 👋

¡Bienvenido a {_appSettings.Name}!

Tu cuenta ha sido creada exitosamente y ya puedes comenzar a usar todos nuestros servicios.

🚀 ¿Qué puedes hacer ahora?
- Personaliza tu perfil
- Explora las funcionalidades
- Conecta con otros usuarios
- Configura tus preferencias

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Puedes iniciar sesión aquí:
{_appSettings.BackendUrl}/auth/login

Si tienes alguna pregunta o necesitas ayuda, no dudes en contactarnos.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Equipo de {_appSettings.Name}
📧 Soporte: {_appSettings.SupportEmail}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Este es un correo automático, por favor no respondas a este mensaje.";
        }
    }
}