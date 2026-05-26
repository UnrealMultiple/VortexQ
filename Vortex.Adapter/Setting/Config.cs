using Vortex.Adapter.Setting.Configs;
using Newtonsoft.Json;
using System.Reflection;
using TShockAPI;
using TShockAPI.Hooks;

namespace Vortex.Adapter.Setting;

public class Config
{
    [JsonProperty("阻止未注册进入")]
    public bool LimitJoin { get; set; }

    [JsonProperty("阻止语句")]
    public string[] DisConnentFormat { get; set; } = ["未注册禁止进入服务器！"];

    [JsonProperty("Socket")]
    public SocketConfig SocketConfig { get; set; } = new();

    [JsonProperty("重置设置")]
    public ResetConfig ResetConfig { get; set; } = new();

    private static string PATH => Path.Combine(TShock.SavePath, "Vortex.Adapter.json");

    private static Config? _instance;

    public static Config Instance => _instance ??= Read();

    public static void Write(Config? config = null)
    {
        var str = JsonConvert.SerializeObject(config ?? _instance, Formatting.Indented);
        File.WriteAllText(PATH, str);
    }

    public static Config Read()
    {
        var c = new Config();
        if (!File.Exists(PATH))
        {
            Write(c);
            return c;
        }
        var str = File.ReadAllText(PATH);
        var ret = JsonConvert.DeserializeObject<Config>(str) ?? new();
        Write(ret);
        return ret;
    }

    public static void Reload(ReloadEventArgs e)
    {
        Plugin.RemoveAssemblyCommands(Assembly.GetExecutingAssembly(), _instance?.SocketConfig.EmptyCommand);
        _instance = Read();
        Plugin.RegisterEmptyCommands(Instance.SocketConfig.EmptyCommand);
    }
}
