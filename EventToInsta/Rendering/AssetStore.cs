using SkiaSharp;

namespace EventToInsta.Rendering;

public sealed class AssetStore(
    string imagesDirectory,
    IReadOnlyDictionary<string, string> sponsorByAge,
    Random random,
    Action<string>? log = null)
{
    private static readonly string[] PhotoExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private readonly HashSet<string> _warnedMissing = new(StringComparer.Ordinal);

    public SKBitmap RandomHerrenPhoto() => RandomPhoto("Herren")
        ?? throw new InvalidOperationException($"Keine Fotos in {Path.Combine(imagesDirectory, "Herren")}");

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

    private SKBitmap? RandomPhoto(string folder)
    {
        var dir = Path.Combine(imagesDirectory, folder);
        if (!Directory.Exists(dir)) return null;
        var files = Directory.EnumerateFiles(dir)
            .Where(f => PhotoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Order(StringComparer.Ordinal)
            .ToList();
        return files.Count == 0 ? null : Decode(files[random.Next(files.Count)]);
    }

    private static SKBitmap Decode(string path) =>
        SKBitmap.Decode(path) ?? throw new InvalidOperationException($"Bild nicht lesbar: {path}");
}
