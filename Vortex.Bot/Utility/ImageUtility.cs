using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortex.Bot.Extension;

namespace Vortex.Bot.Utility;

internal class ImageUtility
{
    public FontFamily FontFamily { get; }

    public static readonly ImageUtility Instance = new();
    private ImageUtility()
    {
        var fc = new FontCollection();
        FontFamily = fc.Add("Resources/Font/simhei.ttf");
    }

    public static FontFamily GetFontFamily()
    {
        return Instance.FontFamily;
    }

    public static Image<Rgba32> GetAvatar(long uin, int size)
    {
        var buffer = HttpUtility.GetByteAsync($"http://q.qlogo.cn/headimg_dl?dst_uin={uin}&spec=640&img_type=png").Result;
        using var image = Image.Load<Rgba32>(buffer);
        var avatar = image.CutCircles(size);
        return avatar;
    }

    public static string GetRandOneBotBackground() => Directory.GetFiles("Resources/OneBotImage").Rand();
}
