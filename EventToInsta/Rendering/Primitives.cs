using SkiaSharp;
using Topten.RichTextKit;

namespace EventToInsta.Rendering;

public static class Primitives
{
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    /// <summary>Skaliert das Foto auf Füllgröße, zentriert es und schneidet es auf das Ziel zu.</summary>
    public static void DrawCover(SKCanvas c, SKBitmap photo, SKRect target)
    {
        var scale = Math.Max(target.Width / photo.Width, target.Height / photo.Height);
        var cropW = target.Width / scale;
        var cropH = target.Height / scale;
        var src = SKRect.Create((photo.Width - cropW) / 2, (photo.Height - cropH) / 2, cropW, cropH);
        using var image = SKImage.FromBitmap(photo);
        c.DrawImage(image, src, target, Sampling);
    }

    /// <summary>Passt das Bild seitenverhältnistreu in die Box ein (zentriert).</summary>
    public static void DrawContained(SKCanvas c, SKBitmap bmp, SKRect box)
    {
        var scale = Math.Min(box.Width / bmp.Width, box.Height / bmp.Height);
        var w = bmp.Width * scale;
        var h = bmp.Height * scale;
        var dest = SKRect.Create(box.MidX - w / 2, box.MidY - h / 2, w, h);
        using var image = SKImage.FromBitmap(bmp);
        c.DrawImage(image, dest, Sampling);
    }

    /// <summary>Zeichnet das Logo; ohne Logo ein runder Platzhalter mit dem Kurznamen.</summary>
    public static void DrawLogoOrBadge(
        SKCanvas c, SKBitmap? logo, string shortName, SKRect box, SKColor badgeFill, SKColor badgeText)
    {
        if (logo != null)
        {
            DrawContained(c, logo, box);
            return;
        }

        var d = Math.Min(box.Width, box.Height);
        using var fill = new SKPaint { Color = badgeFill, IsAntialias = true };
        c.DrawCircle(box.MidX, box.MidY, d / 2, fill);

        var style = new Style
        {
            FontFamily = Theme.Display,
            FontSize = d * 0.42f,
            TextColor = badgeText,
        };
        var text = TextFit.Centered(shortName, style, d * 0.8f);
        text.Paint(c, new SKPoint(box.MidX - d * 0.4f, box.MidY - text.MeasuredHeight / 2));
    }
}
