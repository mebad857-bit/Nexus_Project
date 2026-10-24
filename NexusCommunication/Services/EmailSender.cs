using System.Net;
using System.Net.Mail;

namespace NexusCommunication.Services;

/// <summary>
/// Sends the "reset your password" e-mail through SMTP (appsettings.json -> "Smtp").
/// While Smtp:Host is empty the project is in demo mode: nothing is sent and the reset link is shown on screen instead.
/// </summary>
public class EmailSender
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailSender> _log;
    public EmailSender(IConfiguration cfg, ILogger<EmailSender> log) { _cfg = cfg; _log = log; }

    /// <summary>true when a real SMTP server has been set up</summary>
    public bool Configured => !string.IsNullOrWhiteSpace(_cfg["Smtp:Host"]);

    /// <summary>returns true when the mail was handed to the SMTP server. Never throws.</summary>
    public async Task<bool> SendAsync(string to, string subject, string htmlBody)
    {
        if (!Configured) return false;
        try
        {
            var from = _cfg["Smtp:From"] is { Length: > 0 } f ? f : "no-reply@nexuscomm.com";
            using var msg = new MailMessage { From = new MailAddress(from, _cfg["Smtp:FromName"] ?? "Nexus"), Subject = subject, Body = htmlBody, IsBodyHtml = true };
            msg.To.Add(to);
            using var client = new SmtpClient(_cfg["Smtp:Host"], _cfg.GetValue("Smtp:Port", 587)) { EnableSsl = _cfg.GetValue("Smtp:EnableSsl", true) };
            var user = _cfg["Smtp:User"];
            if (!string.IsNullOrWhiteSpace(user)) client.Credentials = new NetworkCredential(user, _cfg["Smtp:Password"]);
            await client.SendMailAsync(msg);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Password reset e-mail could not be sent");
            return false;
        }
    }
}
