namespace Vortex.Protocol.Serialization.Serializers;

internal class DictionarySerializer<TKey, TValue> : FieldSerializer<IDictionary<TKey, TValue>> where TKey : notnull
{
    protected override IDictionary<TKey, TValue> ReadOverride(BinaryReader br)
    {
        var count = br.ReadInt32();
        var dictionary = new Dictionary<TKey, TValue>(count);
        var keySerializer = PacketSerializer.RequestFieldSerializer(typeof(TKey), null);
        var valueSerializer = PacketSerializer.RequestFieldSerializer(typeof(TValue), null);

        for (int i = 0; i < count; i++)
        {
            var key = (TKey)keySerializer.Read(br);
            var value = (TValue)valueSerializer.Read(br);
            dictionary[key] = value;
        }

        return dictionary;
    }

    protected override void WriteOverride(BinaryWriter bw, IDictionary<TKey, TValue> value)
    {
        bw.Write(value.Count);
        var keySerializer = PacketSerializer.RequestFieldSerializer(typeof(TKey), null);
        var valueSerializer = PacketSerializer.RequestFieldSerializer(typeof(TValue), null);

        foreach (var kvp in value)
        {
            keySerializer.Write(bw, kvp.Key!);
            valueSerializer.Write(bw, kvp.Value!);
        }
    }
}
