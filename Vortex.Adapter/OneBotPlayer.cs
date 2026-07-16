using Microsoft.Xna.Framework;
using TShockAPI;

namespace Vortex.Adapter;

public class OneBotPlayer : TSPlayer
{
    public List<string> CommandOutput = new();
    public bool HasCommandError { get; private set; }

    public OneBotPlayer(string playerName) : base(playerName)
    {
        this.Account = new() { Name = playerName };
        this.Group = new SuperAdminGroup();
        this.AwaitingResponse = new Dictionary<string, Action<object>>();
    }

    public override void SendMessage(string msg, Color color)
    {
        this.SendMessage(msg, color.R, color.G, color.B);
    }

    public override void SendMessage(string msg, byte red, byte green, byte blue)
    {
        if (red >= 180 && red > green + 40 && red > blue + 40)
            HasCommandError = true;
        this.CommandOutput.Add(string.Join("", Terraria.UI.Chat.ChatManager.ParseMessage(msg, Color.White).Select(x => x.Text)));
    }

    public override void SendInfoMessage(string msg)
    {
        this.SendMessage(msg, Color.Yellow);
    }

    public override void SendSuccessMessage(string msg)
    {
        this.SendMessage(msg, Color.Green);
    }

    public override void SendWarningMessage(string msg)
    {
        HasCommandError = true;
        this.SendMessage(msg, Color.OrangeRed);
    }

    public override void SendErrorMessage(string msg)
    {
        HasCommandError = true;
        this.SendMessage(msg, Color.Red);
    }

    public List<string> GetCommandOutput()
    {
        return this.CommandOutput;
    }
}
