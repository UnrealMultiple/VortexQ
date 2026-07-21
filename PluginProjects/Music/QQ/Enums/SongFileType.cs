using Music.Attributes;

namespace Music.QQ.Enums;

public enum SongFileType
{
    /// <summary>
    /// 臻品母带2.0,24Bit 192kHz,size_new[0]
    /// </summary>
    [SongFormat("AI00")]
    [SongExt(".flac")]
    MASTER,

    /// <summary>
    /// 臻品全景声2.0,16Bit 44.1kHz,size_new[1]
    /// </summary>
    [SongFormat("Q000")]
    [SongExt(".flac")]
    ATMOS_2,

    /// <summary>
    /// 臻品音质2.0,16Bit 44.1kHz,size_new[2]
    /// </summary>
    [SongFormat("Q001")]
    [SongExt(".flac")]
    ATMOS_51,

    /// <summary>
    /// flac 格式,16Bit 44.1kHz~24Bit 48kHz,size_flac
    /// </summary>
    [SongFormat("F000")]
    [SongExt(".flac")]
    FLAC,

    /// <summary>
    /// ogg 格式,640kbps,size_new[5]
    /// </summary>
    [SongFormat("O801")]
    [SongExt(".ogg")]
    OGG_640,

    /// <summary>
    /// ogg 格式,320kbps,size_new[3]
    /// </summary>
    [SongFormat("O800")]
    [SongExt(".ogg")]
    OGG_320,

    /// <summary>
    /// ogg 格式,192kbps,size_192ogg
    /// </summary>
    [SongFormat("O600")]
    [SongExt(".ogg")]
    OGG_192,

    /// <summary>
    /// ogg 格式,96kbps,size_96ogg
    /// </summary>
    [SongFormat("O400")]
    [SongExt(".ogg")]
    OGG_96,

    /// <summary>
    /// mp3 格式,320kbps,size_320mp3
    /// </summary>
    [SongFormat("M800")]
    [SongExt(".mp3")]
    MP3_320,

    /// <summary>
    /// mp3 格式,128kbps,size_128mp3
    /// </summary>
    [SongFormat("M500")]
    [SongExt(".mp3")]
    MP3_128,

    /// <summary>
    /// m4a 格式,192kbps,size_192aac
    /// </summary>
    [SongFormat("C600")]
    [SongExt(".m4a")]
    ACC_192,

    /// <summary>
    /// m4a 格式,96kbps,size_96aac
    /// </summary>
    [SongFormat("C400")]
    [SongExt(".m4a")]
    ACC_96,

    /// <summary>
    /// m4a 格式,48kbps,size_48aac
    /// </summary>
    [SongFormat("C200")]
    [SongExt(".m4a")]
    ACC_48,
}

// 辅助方法类
public static class EnumExtensions
{
    public static T? GetAttribute<T>(this SongFileType value) where T : Attribute
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value)!;
        return type.GetField(name)
                   ?.GetCustomAttributes(false)
                   .OfType<T>()
                   .FirstOrDefault();
    }

    public static string GetSongFormat(this SongFileType fileType)
    {
        var attr = fileType.GetAttribute<SongFormat>();
        return attr?.Format ?? "M800";
    }

    public static string GetSongExtension(this SongFileType fileType)
    {
        var attr = fileType.GetAttribute<SongExt>();
        return attr?.Ext ?? ".mp3";
    }
}