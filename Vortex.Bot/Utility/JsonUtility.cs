using Lagrange.Core.Common;
using Lagrange.Core.Events.EventArgs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vortex.Bot.Utility;

public static partial class JsonUtility
{
    [JsonSourceGenerationOptions(AllowOutOfOrderMetadataProperties = true)]

    // BotContext
    [JsonSerializable(typeof(BotKeystore))]
    [JsonSerializable(typeof(BotAppInfo))]

    // Signer
    [JsonSerializable(typeof(SecSignRequest))]
    [JsonSerializable(typeof(SignerResponse<SecSignResponse>))]
    [JsonSerializable(typeof(BotOfflineEvent))]
    private partial class JsonContext : JsonSerializerContext;

    public static string Serialize<T>(T value) where T : class
    {
        return JsonSerializer.Serialize(value, typeof(T), JsonContext.Default);
    }

    public static byte[] SerializeToUtf8Bytes<T>(T value) where T : class
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), JsonContext.Default);
    }

    public static byte[] SerializeToUtf8Bytes(Type type, object? value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, type, JsonContext.Default);
    }

    public static T? Deserialize<T>(byte[] json) where T : class
    {
        return JsonSerializer.Deserialize(json, typeof(T), JsonContext.Default) as T;
    }
    public static object? Deserialize(Type type, byte[] json)
    {
        return JsonSerializer.Deserialize(json, type, JsonContext.Default);
    }

    public static T? Deserialize<T>(Stream json) where T : class
    {
        return JsonSerializer.Deserialize(json, typeof(T), JsonContext.Default) as T;
    }
}
