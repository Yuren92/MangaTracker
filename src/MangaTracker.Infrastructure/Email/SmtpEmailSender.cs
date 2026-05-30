using System.Net;
using System.Net.Mail;
using MangaTracker.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MangaTracker.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpEmailOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendEmailConfirmationAsync(
        string to,
        string confirmationUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "Confirma tu cuenta de Manga Tracker";

        var body = $"""
        <h1>Confirma tu cuenta</h1>
        <p>Gracias por registrarte en Manga Tracker.</p>
        <p>Para activar tu cuenta, pulsa en el siguiente enlace:</p>
        <p>
            <a href="{WebUtility.HtmlEncode(confirmationUrl)}">
                Confirmar mi cuenta
            </a>
        </p>
        <p>Si no has creado esta cuenta, puedes ignorar este correo.</p>
        """;

        return SendAsync(to, subject, body, cancellationToken);
    }

    public Task SendPasswordResetAsync(
        string to,
        string resetPasswordUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "Restablece tu contraseña de Manga Tracker";

        var body = $"""
        <h1>Restablece tu contraseña</h1>
        <p>Hemos recibido una solicitud para cambiar tu contraseña.</p>
        <p>Pulsa en el siguiente enlace para crear una contraseña nueva:</p>
        <p>
            <a href="{WebUtility.HtmlEncode(resetPasswordUrl)}">
                Restablecer contraseña
            </a>
        </p>
        <p>Si no has solicitado este cambio, puedes ignorar este correo.</p>
        """;

        return SendAsync(to, subject, body, cancellationToken);
    }

    public Task SendWelcomeAsync(
        string to,
        CancellationToken cancellationToken = default)
    {
        var subject = "Bienvenido a Manga Tracker";

        var body = """
        <h1>Bienvenido a Manga Tracker</h1>
        <p>Tu cuenta ya está confirmada.</p>
        <p>Ya puedes empezar a buscar series, añadir colecciones y controlar tus tomos pendientes.</p>
        """;

        return SendAsync(to, subject, body, cancellationToken);
    }

    private async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        ValidateOptions();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(to);

        using var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(
                _options.Username,
                _options.Password)
        };

        _logger.LogInformation(
            "Sending email '{Subject}' to {Email}",
            subject,
            to);

        await smtpClient.SendMailAsync(message, cancellationToken);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Username))
        {
            throw new InvalidOperationException("SMTP username is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("SMTP password is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("SMTP from email is not configured.");
        }
    }
}