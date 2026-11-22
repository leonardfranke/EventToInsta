using SkiaSharp;
using Topten.RichTextKit;

namespace EventToInsta.Rendering;

public static class Fonts
{
    private static readonly object Gate = new();
    private static string? _registeredDirectory;

    /// <summary>Registriert die gebündelten TTFs als Quelle für RichTextKit (Schriftwahl per FontMapper; Glyph-Fallback siehe <see cref="TextFit"/>).</summary>
    public static void Register(string fontsDirectory)
    {
        lock (Gate)
        {
            var full = Path.GetFullPath(fontsDirectory);
            if (_registeredDirectory == full) return;
            var previous = FontMapper.Default as BundledFontMapper;
            FontMapper.Default = new BundledFontMapper(full);
            _registeredDirectory = full;
            previous?.Dispose();
        }
    }

    private sealed class BundledFontMapper : FontMapper, IDisposable
    {
        private readonly SKTypeface _anton;
        private readonly (int Weight, SKTypeface Face)[] _inter;

        public BundledFontMapper(string dir)
        {
            _anton = Load(dir, "Anton-Regular.ttf");
            _inter =
            [
                (500, Load(dir, "Inter-Medium.ttf")),
                (700, Load(dir, "Inter-Bold.ttf")),
                (800, Load(dir, "Inter-ExtraBold.ttf")),
            ];
        }

        public void Dispose()
        {
            _anton.Dispose();
            foreach (var (_, face) in _inter) face.Dispose();
        }

        private static SKTypeface Load(string dir, string file)
        {
            var path = Path.Combine(dir, file);
            return SKTypeface.FromFile(path)
                ?? throw new InvalidOperationException($"Schrift konnte nicht geladen werden: {path}");
        }

        public override SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants)
        {
            if (style.FontFamily == Theme.Display) return _anton;
            if (style.FontFamily == Theme.Body)
                return _inter.OrderBy(i => Math.Abs(i.Weight - style.FontWeight)).First().Face;
            throw new InvalidOperationException($"Unbekannte Schriftfamilie: {style.FontFamily}");
        }
    }
}
