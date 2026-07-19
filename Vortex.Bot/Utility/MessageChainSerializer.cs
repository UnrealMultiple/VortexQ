using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;

namespace Vortex.Bot.Utility;

public static class MessageChainSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { ConfigurePolymorphism, ConfigurePropertyExclusions, EnsureNonPublicSetters }
        }
    };

    private static void ConfigurePolymorphism(JsonTypeInfo info)
    {
        if (info.Type != typeof(IMessageEntity)) return;

        var options = new JsonPolymorphismOptions
        {
            IgnoreUnrecognizedTypeDiscriminators = true,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };

        foreach (var type in typeof(IMessageEntity).Assembly.GetTypes())
        {
            if (type is { IsAbstract: false, IsInterface: false } && typeof(IMessageEntity).IsAssignableFrom(type))
            {
                options.DerivedTypes.Add(new JsonDerivedType(type, type.Name));
            }
        }

        info.PolymorphismOptions = options;
    }

    private static void ConfigurePropertyExclusions(JsonTypeInfo info)
    {
        if (info.Type == typeof(ReplyEntity))
        {
            var sourceProp = info.Properties.FirstOrDefault(p => p.Name == nameof(ReplyEntity.Source));
            if (sourceProp is not null)
            {
                sourceProp.ShouldSerialize = static (_, _) => false;
            }

            var elemsProp = info.Properties.FirstOrDefault(p => p.Name == nameof(ReplyEntity.Elems));
            if (elemsProp is not null)
            {
                elemsProp.ShouldSerialize = static (_, _) => false;
            }
        }
    }

    /// <summary>
    /// IMessageEntity 实现类常使用 <c>internal init</c> / <c>internal set</c> 声明属性，
    /// System.Text.Json 默认不会生成 Set 委托，导致反序列化后属性全为默认值。
    /// 利用 Lagrange.Core 的 InternalsVisibleTo("Vortex.Bot")，通过反射为这些属性赋予 Set 能力。
    /// </summary>
    private static void EnsureNonPublicSetters(JsonTypeInfo info)
    {
        if (info.Type.Assembly != typeof(IMessageEntity).Assembly) return;
        if (!typeof(IMessageEntity).IsAssignableFrom(info.Type) || info.Type.IsAbstract) return;

        foreach (var prop in info.Properties)
        {
            if (prop.Set is not null) continue;

            var propInfo = info.Type.GetProperty(prop.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var setMethod = propInfo?.SetMethod;
            if (setMethod is null || setMethod.IsPrivate) continue;

            var setter = setMethod;
            prop.Set = (obj, value) => setter.Invoke(obj, [value]);
        }
    }

    public static byte[] SerializeToUtf8Bytes(MessageChain chain)
    {
        return JsonSerializer.SerializeToUtf8Bytes(chain, Options);
    }

    public static MessageChain? Deserialize(byte[] utf8Bytes)
    {
        return JsonSerializer.Deserialize<MessageChain>(utf8Bytes, Options);
    }

    public static string Serialize(MessageChain chain)
    {
        return JsonSerializer.Serialize(chain, Options);
    }

    public static MessageChain? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<MessageChain>(json, Options);
    }
}
