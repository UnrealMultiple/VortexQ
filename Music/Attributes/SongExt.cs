namespace Music.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class SongExt(string ext) : Attribute
{
    public string Ext { get; } = ext;
}
