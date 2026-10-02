using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bonzer.Infrastructure.Services;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public SmtpOptions Smtp { get; init; } = new();

    /// <summary>Front-end page that completes the reset, e.g. https://erp.school.com/reset-password.</summary>
    public string PasswordResetUrl { get; init; } = "http://localhost:4200/reset-password";

    public sealed class SmtpOptions
    {
        public string? Host { get; init; }
        public int Port { get; init; } = 587;
        public bool EnableSsl { get; init; } = true;
        public string? UserName { get; init; }
        public string? Password { get; init; }
        public string FromAddress { get; init; } = "no-reply@bonzer.local";
        public string FromName { get; init; } = "Bonzer ERP";
    }
}

/// <summary>
/// Email over SMTP when Messaging:Smtp:Host is set; otherwise (and for SMS, until a gateway is added)
/// the message is logged. Message content is only logged in Development, because it contains reset codes.
/// </summary>
public sealed class MessageSender(IOptions<MessagingOptions> options, IHostEnvironment environment, ILogger<MessageSender> logger)
    : IMessageSender
{
    private readonly MessagingOptions _options = options.Value;

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var smtp = _options.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            LogUndelivered("email", to, $"{subject}\n{htmlBody}");
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(smtp.FromAddress, smtp.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(smtp.Host, smtp.Port) { EnableSsl = smtp.EnableSsl };
        if (!string.IsNullOrEmpty(smtp.UserName))
            client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is SmtpException or IOException or InvalidOperationException)
        {
            // Provider details stay in the logged inner exception; callers get a user-safe message.
            throw new MessageDeliveryException("email", ex);
        }
        logger.LogInformation("Email '{Subject}' sent to {Recipient}", subject, to);
    }

    public Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        // TODO: integrate an SMS / WhatsApp gateway (MSG91, Twilio, Gupshup) here.
        LogUndelivered("SMS", phoneNumber, message);
        return Task.CompletedTask;
    }

    private void LogUndelivered(string channel, string recipient, string content)
    {
        if (environment.IsDevelopment())
            logger.LogWarning("[DEV] No {Channel} provider configured. Message for {Recipient}:\n{Content}", channel, recipient, content);
        else
            logger.LogError("No {Channel} provider configured; message to {Recipient} was not delivered", channel, recipient);
    }
}
