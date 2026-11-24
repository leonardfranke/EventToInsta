using SkiaSharp;

namespace EventToInsta.Rendering;

/// <summary>
/// Generischer Hintergrund der Jugend-Stories (Entwurf A „Spielfeld“ aus jugend-hintergrund.html):
/// radialer Verlauf in Vereinsblau mit dezenten weißen Linien eines Basketballfelds. Kein Foto, kein Zufall.
/// Koordinaten aus dem Entwurf (300 × 533), auf das Zielrechteck skaliert.
/// </summary>
public static class CourtBackground
{
    private const float MockWidth = 300;
    private const float MockHeight = 533;

    private static readonly SKColor Inner = SKColor.Parse("#1663C7");
    private static readonly SKColor Outer = SKColor.Parse("#062A5E");
    private static readonly SKColor Line = SKColors.White.WithAlpha(33); // 13 %
    private const float LineWidth = 2.2f;

    public static void Draw(SKCanvas c, SKRect bounds)
    {
        DrawGradient(c, bounds);

        c.Save();
        c.Translate(bounds.Left, bounds.Top);
        c.Scale(bounds.Width / MockWidth, bounds.Height / MockHeight);
        using var paint = new SKPaint
        {
            Color = Line, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = LineWidth,
        };
        using var court = CourtPath();
        c.DrawPath(court, paint);
        c.Restore();
    }

    /// <summary>
    /// Wie SVG radialGradient cx=50 % cy=38 % r=75 % in Objektkoordinaten: eine Ellipse mit 75 % der Breite
    /// bzw. Höhe als Halbachsen.
    /// </summary>
    private static void DrawGradient(SKCanvas c, SKRect bounds)
    {
        var centre = new SKPoint(bounds.Left + bounds.Width * 0.5f, bounds.Top + bounds.Height * 0.38f);
        var rx = bounds.Width * 0.75f;
        var ry = bounds.Height * 0.75f;
        var squash = SKMatrix.CreateScale(1, ry / rx, centre.X, centre.Y);
        using var shader = SKShader.CreateRadialGradient(centre, rx, [Inner, Outer], [0, 1], SKShaderTileMode.Clamp, squash);
        using var paint = new SKPaint { Shader = shader, IsAntialias = true, IsDither = true };
        c.DrawRect(bounds, paint);
    }

    private static SKPath CourtPath()
    {
        var p = new SKPath();
        p.AddRoundRect(SKRect.Create(14, 14, 272, 505), 4, 4);
        p.MoveTo(14, 266);
        p.LineTo(286, 266);
        p.AddCircle(150, 266, 42);
        p.AddCircle(150, 266, 12);

        // oben
        p.AddRect(SKRect.Create(102, 14, 96, 120));
        p.AddCircle(150, 134, 38);
        p.MoveTo(40, 14);
        p.LineTo(40, 60);
        p.ArcTo(new SKPoint(110, 110), 0, SKPathArcSize.Small, SKPathDirection.CounterClockwise, new SKPoint(260, 60));
        p.LineTo(260, 14);

        // unten, gespiegelt
        p.AddRect(SKRect.Create(102, 399, 96, 120));
        p.AddCircle(150, 399, 38);
        p.MoveTo(40, 519);
        p.LineTo(40, 473);
        p.ArcTo(new SKPoint(110, 110), 0, SKPathArcSize.Small, SKPathDirection.Clockwise, new SKPoint(260, 473));
        p.LineTo(260, 519);
        return p;
    }
}
