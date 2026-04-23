using System.Text.Json;
using System.Text.Json.Serialization;
using Vortex.Bot.Events;

namespace Vortex.Bot.Configuration;

public abstract class JsonConfigBase<T> where T : JsonConfigBase<T>, new()
{
    private static T? _instance;
    private static readonly object _lock = new();

    [JsonIgnore]
    public virtual string FileName => typeof(T).Name;

    [JsonIgnore]
    public virtual string Directory => Path.Combine(VortexContext.Path, "Configs");

    [JsonIgnore]
    private string FullPath => Path.Combine(Directory, $"{FileName}.json");

    public virtual void SetDefault()
    {
    }

    public virtual void OnReloaded(ReloadEventArgs args)
    {
    }

    public void Save(string? path = null)
    {
        var filePath = path ?? FullPath;
        var dir = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize((T)this, options);
        File.WriteAllText(filePath, json);
    }

    private static T LoadFromFile()
    {
        var temp = new T();
        var fullPath = temp.FullPath;

        if (!File.Exists(fullPath))
        {
            temp.SetDefault();
            temp.Save();
            return temp;
        }

        var json = File.ReadAllText(fullPath);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var config = JsonSerializer.Deserialize<T>(json, options);
        if (config == null)
        {
            temp.SetDefault();
            temp.Save();
            return temp;
        }

        return config;
    }

    private async Task OnReloadAsync(ReloadEventArgs args)
    {
        lock (_lock)
        {
            _instance = LoadFromFile();
        }
        _instance.OnReloaded(args);
    }

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= LoadFromFile();
                }
            }
            return _instance;
        }
    }

    public static string Load()
    {
        ReloadEvents.OnReload += Instance.OnReloadAsync;
        return Instance.FileName;
    }

    public static string Unload()
    {
        ReloadEvents.OnReload -= Instance.OnReloadAsync;
        return Instance.FileName;
    }

    public static void SaveInstance()
    {
        _instance?.Save();
    }

    public static void ReloadInstance()
    {
        lock (_lock)
        {
            _instance = LoadFromFile();
        }
    }
}
