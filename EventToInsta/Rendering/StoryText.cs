using SkiaSharp;
using Topten.RichTextKit;

namespace EventToInsta.Rendering;

/// <summary>
/// Gemeinsame Textbausteine der Story-Vorlagen: Zeilen werden über ihre Grundlinie gesetzt,
/// damit Abstände unabhängig von den Zeilenmetriken der Schrift fest bleiben.
/// </summary>
internal static class StoryText
{
    private const float Unlimited = 100_000f;

    public static void DrawLeft(SKCanvas c, string text, Style style, float x, float baseline, float maxWidth, float minScale = 1f)
    {
        var block = Fit(text, style, maxWidth, minScale);
        PaintAtBaseline(c, block, x, baseline);
    }

    public static void DrawCentered(
        SKCanvas c, string text, Style style, float left, float width, float baseline,
        float minScale = 0.75f, bool letterSpacingCorrection = false)
    {
        var block = Fit(text, style, width, minScale, centered: true);
        // Laufweite steht auch hinter dem letzten Zeichen; für echte Zentrierung halb zurücknehmen.
        var shift = letterSpacingCorrection ? style.LetterSpacing / 2 : 0;
        PaintAtBaseline(c, block, left + shift, baseline);
    }

    /// <summary>Verkleinert die Schrift bis <paramref name="minScale"/>, erst danach kürzt TextFit mit „…“.</summary>
    public static TextBlock Fit(string text, Style style, float maxWidth, float minScale, bool centered = false)
    {
        // Breite wächst linear mit Schriftgröße und Laufweite, also reicht eine Messung ohne Begrenzung.
        var natural = NaturalWidth(text, style);
        var scale = natural > maxWidth ? Math.Max(minScale, maxWidth / natural * 0.99f) : 1f;
        var s = With(style, style.TextColor);
        s.FontSize = style.FontSize * scale;
        s.LetterSpacing = style.LetterSpacing * scale;
        return centered ? TextFit.Centered(text, s, maxWidth) : TextFit.Line(text, s, maxWidth);
    }

    /// <summary>Breite der Zeile ohne Begrenzung (inklusive Laufweite hinter dem letzten Zeichen).</summary>
    public static float NaturalWidth(string text, Style style) => TextFit.Line(text, style, Unlimited).MeasuredWidth;

    public static Style With(Style s, SKColor color) => new()
    {
        FontFamily = s.FontFamily,
        FontWeight = s.FontWeight,
        FontSize = s.FontSize,
        LetterSpacing = s.LetterSpacing,
        TextColor = color,
    };

    public static void PaintAtBaseline(SKCanvas c, TextBlock block, float x, float baseline) =>
        block.Paint(c, new SKPoint(x, baseline - block.Lines[0].BaseLine));

    public static float CapHeight(Style style)
    {
        var face = FontMapper.Default.TypefaceFromStyle(style, false);
        using var font = new SKFont(face, style.FontSize);
        return font.Metrics.CapHeight;
    }
}
