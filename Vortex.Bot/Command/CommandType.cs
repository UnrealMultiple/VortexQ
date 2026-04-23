namespace Vortex.Bot.Command;

[Flags]
public enum CommandType
{

    Group = 1 << 0,

    Friend = 1 << 1,

    Server = 1 << 2,

    All = Group | Friend | Server
}
