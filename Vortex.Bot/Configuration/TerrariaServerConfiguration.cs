namespace Vortex.Bot.Configuration;

public class TerrariaServerEnity
{
    public string Name { get; set; } = "服务器1";
    public string IP { get; set; } = "";
    public ushort Port { get; set; } = 7777;
    public ushort DisplayPort { get; set; } = 7777;
    public string Token { get; set; } = "";
    public string DefaultGroup { get; set; } = "default";
    public int RegisterMaxCount { get; set; } = 1;
    public int RegisterNameMax { get; set; } = 10;
    public int MsgMaxLength { get; set; } = 50;
    public bool RegisterNameLimit { get; set; } = true;
    public bool EnabledShop { get; set; }
    public bool EnabledPrize { get; set; }
    public string TShockPath { get; set; } = "";
    public string MapName { get; set; } = "";
    public string Describe { get; set; } = "正常玩法服务器";
    public string Version { get; set; } = "1.4.4.9";
    public List<long> Groups { get; set; } = [];
    public List<long> ForwardGroups { get; set; } = [];
}

public class TerrariaServerCollection
{
    public List<TerrariaServerEnity> Servers { get; set; } = [];
}
