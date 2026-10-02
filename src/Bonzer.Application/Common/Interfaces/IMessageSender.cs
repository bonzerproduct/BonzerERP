namespace Bonzer.Application.Common.Interfaces;

/// <summary>
/// Outgoing email / SMS. The default implementation sends email over SMTP when configured and otherwise
/// logs the message (content is logged only in Development). Plug in an SMS gateway (MSG91, Twilio...) here.
/// </summary>
public interface IMessageSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
