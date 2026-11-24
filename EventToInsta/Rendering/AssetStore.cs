using SkiaSharp;

namespace EventToInsta.Rendering;

public sealed class AssetStore(
    string imagesDirectory,
    IReadOnlyDictionary<string, string> sponsorByAge,
    Action<string>? log = null)
{
    private readonly HashSet<string> _warnedMissing = new(StringComparer.Ordinal);

    public SKBitmap ClubLogo() => Decode(Path.Combine(imagesDirectory, "Logos", "Dachse.png"));

    public SKBitmap? SponsorLogo(string ageClass)
    {
        if (!sponsorByAge.TryGetValue(ageClass, out var file)) return null;
        var path = Path.Combine(imagesDirectory, "Sponsoren", file);
        if (File.Exists(path)) return Decode(path);
        // Konfigurierter, aber fehlender Sponsor ist ein Konfigurationsfehler: einmal je Datei warnen.
        if (_warnedMissing.Add(path))
            log?.Invoke($"Warnung: Sponsor-Logo für {ageClass} nicht gefunden: {path} (SPONSORS prüfen).");
        return null;
    }

    private static SKBitmap Decode(string path) =>
        SKBitmap.Decode(path) ?? throw new InvalidOperationException($"Bild nicht lesbar: {path}");
}
