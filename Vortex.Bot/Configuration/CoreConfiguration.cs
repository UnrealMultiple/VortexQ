namespace Vortex.Bot.Configuration;

public class CoreConfiguration
{
    public ServerConfiguration Server { get; set; } = new();

    public SignerConfiguration Signer { get; set; } = new();

    public LoginConfiguration Login { get; set; } = new();

    public DatabaseConfiguration Database { get; set; } = new();

    public CommandConfiguration Command { get; set; } = new();

    public MailConfiguration Mail { get; set; } = new();

    public MiscellaneousConfiguration Miscellaneous { get; set; } = new();

    public List<long> SuperAdmins { get; set; } = [523321293];
}
