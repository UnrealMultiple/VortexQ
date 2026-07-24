using System.Text.Json;
using KuGou.Net.Protocol.Session;
using Vortex.Bot;

namespace Music.Kugou;

internal sealed class FileSessionPersistence : ISessionPersistence
{
    private static readonly string FilePath =
        Path.Combine(VortexContext.Path, "Configs", "kugou_session.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KgSession? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<KgSession>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Save(KgSession session)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(session, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // best effort
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
        catch { }
    }
}
