using System.Text;
using Topten.RichTextKit;

namespace EventToInsta.Rendering;

public static class TextFit
{
    /// <summary>Einzeilig, bei Überlänge mit „…“ gekürzt.</summary>
    public static TextBlock Line(string text, Style style, float maxWidth) =>
        Build(text, style, maxWidth, TextAlignment.Left);

    /// <summary>Wie <see cref="Line"/>, aber zentriert innerhalb von <paramref name="maxWidth"/>.</summary>
    public static TextBlock Centered(string text, Style style, float maxWidth) =>
        Build(text, style, maxWidth, TextAlignment.Center);

    private static TextBlock Build(string text, Style style, float maxWidth, TextAlignment alignment)
    {
        var block = new TextBlock
        {
            MaxWidth = maxWidth,
            MaxLines = 1,
            EllipsisEnabled = true,
            Alignment = alignment,
        };
        block.AddText(Sanitize(text, style), style);
        return block;
    }

    /// <summary>
    /// RichTextKit fällt für Zeichen ohne Glyphe selbst auf Systemschriften zurück (am FontMapper vorbei).
    /// Damit nie Systemschriften gerendert werden, wird jedes Zeichen, das die gebündelte Schrift nicht
    /// enthält, durch „?“ ersetzt.
    /// </summary>
    private static string Sanitize(string text, Style style)
    {
        var face = FontMapper.Default.TypefaceFromStyle(style, false);
        var sb = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
            sb.Append(char.IsWhiteSpace((char)Math.Min(rune.Value, 0xFFFF)) || face.ContainsGlyph(rune.Value)
                ? rune.ToString()
                : "?");
        return sb.ToString();
    }
}
