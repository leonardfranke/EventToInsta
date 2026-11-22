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
        block.AddText(text, style);
        return block;
    }
}
