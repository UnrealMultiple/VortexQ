using System.Text.Json;
using System.Text.Json.Serialization;
using Vortex.Bot.Events;

namespace Vortex.Bot.Configuration;

public abstract class JsonConfigBase<T> where T : JsonConfigBase<T>, new()
{
    private static T? _instance;
    private static readonly Lock _lock = new();

    [JsonIgnore]
    public virtual string FileName => typeof(T).Name;

    [JsonIgnore]
    public virtual string Directory => Path.Combine(VortexContext.Path, "Configs");

    [JsonIgnore]
    private string FullPath => Path.Combine(Directory, $"{FileName}.json");

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public virtual void SetDefault()
    {
    }

    public virtual void OnReloaded(ReloadEventArgs args)
    {
    }

    public void Save(string? path = null)
    {
        string filePath = path ?? FullPath;
        string? dir = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }
        string json = JsonSerializer.Serialize((T)this, _serializerOptions);
        File.WriteAllText(filePath, json);
    }

    private static T LoadFromFile()
    {
        T temp = new T();
        string fullPath = temp.FullPath;
        Console.WriteLine(fullPath);
        if (!File.Exists(fullPath))
        {
            temp.SetDefault();
            temp.Save();
            return temp;
        }

        string json = File.ReadAllText(fullPath);
        var config = JsonSerializer.Deserialize<T>(json, _serializerOptions);
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
