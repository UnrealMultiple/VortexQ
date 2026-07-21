namespace Music.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class SongFormat(string format) : Attribute
{
    public string Format { get; } = format;
}
