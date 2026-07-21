using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Music.Common;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions _defaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? _defaultOptions);
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(json, options ?? _defaultOptions);
    }

    public static JsonNode? ParseNode(string json)
    {
        return JsonNode.Parse(json);
    }

    public static T? GetValue<T>(this JsonNode? node, string propertyName)
    {
        if (node is null) return default;
        var property = node[propertyName];
        if (property is null) return default;
        return property.GetValue<T>();
    }

    public static string GetStringValue(this JsonNode? node, string propertyName, string defaultValue = "")
    {
        return node?[propertyName]?.ToString() ?? defaultValue;
    }
}
