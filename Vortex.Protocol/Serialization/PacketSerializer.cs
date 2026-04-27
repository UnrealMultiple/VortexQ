using System.Reflection;
using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Serialization.Serializers;

namespace Vortex.Protocol.Serialization;

public class PacketSerializer
{
    private readonly Dictionary<Type, Action<BinaryWriter, object>> _serializers = new();
    private readonly Dictionary<PacketType, Func<BinaryReader, object>> _deserializers = new();

    private static readonly Dictionary<Type, IFieldSerializer> FieldSerializers = new()
    {
        [typeof(string)] = new StringSerializer(),
        [typeof(Guid)] = new GuidSerializer(),
        [typeof(byte[])] = new ByteArraySerializer(),
        [typeof(DateTime)] = new DateTimeSerializer(),
        [typeof(TimeSpan)] = new TimeSpanSerializer()
    };

    public PacketSerializer()
    {
        LoadPackets();
    }

    private void LoadPackets()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            RegisterPacket(type);
        }
    }

    public void RegisterPacket<T>() where T : INetPacket
    {
        RegisterPacket(typeof(T));
    }

    private void RegisterPacket(Type type)
    {
        if (type.IsAbstract || !typeof(INetPacket).IsAssignableFrom(type)) return;

        var serializers = new List<Action<BinaryWriter, object>>();
        var deserializers = new List<Action<object, BinaryReader>>();

        foreach (var prop in type.GetProperties())
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            if (prop.IsDefined(typeof(IgnoreAttribute))) continue;
            if (prop.Name == "PacketID") continue;

            var (serializeAction, deserializeAction) = CreatePropertyActions(prop);
            serializers.Add(serializeAction);
            deserializers.Add(deserializeAction);
        }

        var packetId = (Activator.CreateInstance(type) as INetPacket)?.PacketID ?? 0;

        if (serializers.Count > 0)
        {
            _serializers[type] = (bw, o) =>
            {
                foreach (var s in serializers)
                    s(bw, o);
            };
        }

        if (deserializers.Count > 0)
        {
            _deserializers[packetId] = br =>
            {
                var result = Activator.CreateInstance(type)!;
                foreach (var d in deserializers)
                    d(result, br);
                return result;
            };
        }
    }

    private (Action<BinaryWriter, object> serialize, Action<object, BinaryReader> deserialize) CreatePropertyActions(PropertyInfo prop)
    {
        var serializer = RequestFieldSerializer(prop.PropertyType, prop);

        if (IsCollectionType(prop.PropertyType))
        {
            void serialize(BinaryWriter bw, object o)
            {
                var value = prop.GetValue(o);
                if (value == null)
                {
                    bw.Write(0);
                    return;
                }
                var valueSerializer = RequestFieldSerializer(value.GetType(), null);
                valueSerializer.Write(bw, value);
            }

            void deserialize(object o, BinaryReader br)
            {
                prop.SetValue(o, serializer.Read(br));
            }

            return (serialize, deserialize);
        }

        return (
            (bw, o) => serializer.Write(bw, prop.GetValue(o)),
            (o, br) => prop.SetValue(o, serializer.Read(br))
        );
    }

    public static IFieldSerializer RequestFieldSerializer(Type type, PropertyInfo? prop)
    {
        if (prop?.GetCustomAttribute<SerializerAttribute>() is { } serializerAttr)
            return (IFieldSerializer)Activator.CreateInstance(serializerAttr.SerializerType)!;

        if (type.GetCustomAttribute<DefaultSerializerAttribute>() is { } defaultSerializerAttr)
            return (IFieldSerializer)Activator.CreateInstance(defaultSerializerAttr.SerializerType)!;

        if (FieldSerializers.TryGetValue(type, out var cachedSerializer))
            return cachedSerializer;

        var serializer = CreateSerializer(type);
        FieldSerializers[type] = serializer;
        return serializer;
    }

    private static IFieldSerializer CreateSerializer(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            return CreateGenericSerializer(typeof(NullableSerializer<>), underlyingType);

        if (type.IsPrimitive || type.IsEnum)
            return CreateGenericSerializer(typeof(PrimitiveFieldSerializer<>), type);

        if (type.IsArray)
            return CreateGenericSerializer(typeof(ArraySerializer<>), type.GetElementType()!);

        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();

            if (IsGenericTypeOf(genericType, typeof(IDictionary<,>)))
                return CreateDictionarySerializer(type.GetGenericArguments());

            if (IsGenericTypeOf(genericType, typeof(ICollection<>)))
                return CreateGenericSerializer(typeof(CollectionSerializer<>), type.GetGenericArguments()[0]);

            if (IsGenericTypeOf(genericType, typeof(IEnumerable<>)))
                return CreateGenericSerializer(typeof(EnumerableSerializer<>), type.GetGenericArguments()[0]);
        }

        if (IsGenericInterfaceImplementation(type, typeof(IDictionary<,>)))
            return CreateDictionarySerializer(type.GetGenericArguments());

        if (IsGenericInterfaceImplementation(type, typeof(ICollection<>)))
            return CreateGenericSerializer(typeof(CollectionSerializer<>), type.GetGenericArguments()[0]);

        if (IsGenericInterfaceImplementation(type, typeof(IEnumerable<>)))
            return CreateGenericSerializer(typeof(EnumerableSerializer<>), type.GetGenericArguments()[0]);

        if (type.IsClass && type != typeof(object))
            return CreateGenericSerializer(typeof(ClassSerializer<>), type);

        throw new NotSupportedException($"Type {type} is not supported for serialization");
    }

    private static IFieldSerializer CreateDictionarySerializer(Type[] genericArgs)
    {
        return CreateGenericSerializer2(typeof(DictionarySerializer<,>), genericArgs[0], genericArgs[1]);
    }

    private static IFieldSerializer CreateGenericSerializer(Type genericSerializerType, Type typeArgument)
    {
        var concreteType = genericSerializerType.MakeGenericType(typeArgument);
        return (IFieldSerializer)Activator.CreateInstance(concreteType)!;
    }

    private static IFieldSerializer CreateGenericSerializer2(Type genericSerializerType, Type typeArgument1, Type typeArgument2)
    {
        var concreteType = genericSerializerType.MakeGenericType(typeArgument1, typeArgument2);
        return (IFieldSerializer)Activator.CreateInstance(concreteType)!;
    }

    private static bool IsGenericTypeOf(Type genericType, Type targetGenericType)
    {
        return genericType == targetGenericType;
    }

    private static bool IsGenericInterfaceImplementation(Type type, Type genericInterface)
    {
        if (!type.IsGenericType) return false;

        if (type.GetGenericTypeDefinition() == genericInterface)
            return true;

        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
    }

    private static bool IsCollectionType(Type type)
    {
        if (!type.IsGenericType) return false;

        var genericType = type.GetGenericTypeDefinition();

        return IsGenericTypeOf(genericType, typeof(IDictionary<,>))
            || IsGenericTypeOf(genericType, typeof(ICollection<>))
            || IsGenericTypeOf(genericType, typeof(IEnumerable<>))
            || type.GetInterfaces().Any(i =>
                i.IsGenericType && (
                    i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                    i.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                    i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    public byte[] Serialize(INetPacket packet)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write((short)0);
        bw.Write((byte)packet.PacketID);

        if (_serializers.TryGetValue(packet.GetType(), out var serializer))
        {
            serializer(bw, packet);
            var length = (short)ms.Position;
            ms.Position = 0;
            bw.Write(length);

            return ms.ToArray();
        }

        return Array.Empty<byte>();
    }

    public INetPacket? Deserialize(BinaryReader br)
    {
        var length = br.ReadInt16();
        var packetType = (PacketType)br.ReadByte();

        if (_deserializers.TryGetValue(packetType, out var deserializer))
        {
            return deserializer(br) as INetPacket;
        }

        return null;
    }
}
