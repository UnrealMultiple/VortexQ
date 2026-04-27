using System.Net;
using System.Net.Mail;
using System.Text;

namespace Vortex.Bot.Utility;

public class MailUtility(string Host, int Port, string Password, bool EnableSsl) : IDisposable
{
    public string Host { get; } = Host;

    public int Port { get; } = Port;

    public string Password { get; } = Password;

    public bool EnableSsl { get; } = EnableSsl;

    private readonly SmtpClient Client = new(Host, Port);

    private readonly MailMessage Mail = new();

    public static MailUtility Builder(string host, int port, string password, bool enableSsl) => new(host, port, password, enableSsl);

    public MailUtility SetTile(string title)
    {
        Mail.Subject = title;
        Mail.SubjectEncoding = Encoding.UTF8;
        return this;
    }

    public MailUtility SetBody(string body)
    {
        Mail.Body = body;
        Mail.BodyEncoding = Encoding.UTF8;
        return this;
    }

    public MailUtility AddTarget(string target)
    {
        Mail.To.Add(target);
        return this;
    }

    public MailUtility AddAttachment(string path)
    {
        var attach = new Attachment(path);
        var disposition = attach.ContentDisposition!;
        disposition.CreationDate = File.GetCreationTime(path);
        disposition.ModificationDate = File.GetLastWriteTime(path);
        disposition.ReadDate = File.GetLastAccessTime(path);
        Mail.Attachments.Add(attach);
        return this;
    }

    public MailUtility SetSender(string sender)
    {
        Mail.From = new(sender);
        return this;
    }

    public MailUtility EnableHtmlBody(bool enable)
    {
        Mail.IsBodyHtml = enable;
        return this;
    }

    public MailUtility Send()
    {
        Client.DeliveryMethod = SmtpDeliveryMethod.Network;
        Mail.BodyEncoding = Encoding.UTF8;
        Client.UseDefaultCredentials = false;
        Client.EnableSsl = EnableSsl;
        Client.Credentials = new NetworkCredential(Mail.From?.Address, Password);
        Client.Send(Mail);
        return this;
    }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
