using MailKit.Net.Smtp;
using MimeKit;
using System.Text;

namespace Vortex.Bot.Utility;

public class MailUtility(string host, int port, string password, bool enableSsl) : IDisposable
{
    public string Host { get; } = host;
    public int Port { get; } = port;
    public string Password { get; } = password;
    public bool EnableSsl { get; } = enableSsl;
    public string Username { get; private set; } = "";

    private readonly SmtpClient _client = new SmtpClient();
    private readonly MimeMessage _mail = new();
    private string _subject = "";
    private string _body = "";
    private bool _isHtml = false;
    private readonly List<string> _attachments = [];

    public static MailUtility Builder(string host, int port, string password, bool enableSsl) => new(host, port, password, enableSsl);

    public MailUtility SetTitle(string title)
    {
        _subject = title;
        return this;
    }

    public MailUtility SetBody(string body)
    {
        _body = body;
        return this;
    }

    public MailUtility AddTarget(string target)
    {
        _mail.To.Add(MailboxAddress.Parse(target));
        return this;
    }

    public MailUtility AddAttachment(string path)
    {
        _attachments.Add(path);
        return this;
    }

    public MailUtility SetSender(string sender, string? senderName = null)
    {
        var name = senderName ?? sender;
        _mail.From.Add(new MailboxAddress(Encoding.UTF8, name, sender));
        Username = sender;
        return this;
    }

    public MailUtility EnableHtmlBody(bool enable)
    {
        _isHtml = enable;
        return this;
    }

    private void BuildMessage()
    {
        _mail.Subject = _subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = _isHtml ? null : _body,
            HtmlBody = _isHtml ? _body : null
        };

        foreach (var attachment in _attachments)
        {
            bodyBuilder.Attachments.Add(attachment);
        }

        _mail.Body = bodyBuilder.ToMessageBody();
    }

    public void Send()
    {
        if (_mail.From.Count == 0)
            throw new InvalidOperationException("发件人未设置");

        if (_mail.To.Count == 0)
            throw new InvalidOperationException("收件人未设置");

        BuildMessage();

        var credentialUser = string.IsNullOrEmpty(Username) ? ((MailboxAddress)_mail.From[0]).Address : Username;

        _client.Connect(Host, Port, EnableSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
        _client.Authenticate(credentialUser, Password);
        _client.Send(_mail);
        _client.Disconnect(true);
    }

    public async Task SendAsync(CancellationToken cancellationToken = default)
    {
        if (_mail.From.Count == 0)
            throw new InvalidOperationException("发件人未设置");

        if (_mail.To.Count == 0)
            throw new InvalidOperationException("收件人未设置");

        BuildMessage();

        var credentialUser = string.IsNullOrEmpty(Username) ? ((MailboxAddress)_mail.From[0]).Address : Username;

        await _client.ConnectAsync(Host, Port, EnableSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
        await _client.AuthenticateAsync(credentialUser, Password, cancellationToken);
        await _client.SendAsync(_mail, cancellationToken);
        await _client.DisconnectAsync(true, cancellationToken);
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
