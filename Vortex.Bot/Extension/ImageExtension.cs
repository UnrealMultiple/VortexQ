using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Vortex.Bot.Extension;

public static class ImageExtension
{
    public static Image<Rgba32> Crop(this Image<Rgba32> image, int width, int height)
    {
        var option = new GraphicsOptions
        {
            Antialias = true,
            AntialiasSubpixelDepth = 16,
            BlendPercentage = 1,
            AlphaCompositionMode = PixelAlphaCompositionMode.Src
        };
        var background = new Image<Rgba32>(width, height);
        background.Mutate(x => x.SetGraphicsOptions(option));
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Crop
        }));
        background.Mutate(x => x.DrawImage(image, new Point(0, 0), 1f));
        return background;
    }

    public static Image<Rgba32> CutCircles(this Image<Rgba32> image, int diameter, int BorderSize = 5)
    {
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(diameter, diameter),
            Mode = ResizeMode.Crop
        }));

        using var circular = new Image<Rgba32>(diameter, diameter);
        circular.Mutate(x =>
        {
            var circle = new EllipsePolygon(diameter / 2, diameter / 2, diameter / 2);
            x.Clear(Color.Transparent);
            x.Fill(Color.White, circle);
            x.DrawImage(image, new Point(0, 0), new GraphicsOptions
            {
                ColorBlendingMode = PixelColorBlendingMode.Multiply,
                AlphaCompositionMode = PixelAlphaCompositionMode.SrcIn
            });
        });

        var finalSize = diameter + (BorderSize * 2);
        var final = new Image<Rgba32>(finalSize, finalSize);
        final.Mutate(x =>
        {
            var borderCircle = new EllipsePolygon(finalSize / 2, finalSize / 2, finalSize / 2);
            x.Fill(Color.White, borderCircle);
            x.DrawImage(circular, new Point(BorderSize, BorderSize), 1f);
        });
        return final;
    }

    public static async Task<byte[]> ToBytesAsync(this Image<Rgba32> image)
    {
        await using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);
        return ms.ToArray();
    }

}
