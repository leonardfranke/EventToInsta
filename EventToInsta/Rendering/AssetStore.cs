using SkiaSharp;

namespace EventToInsta.Rendering;

public sealed class AssetStore(
    string imagesDirectory,
    IReadOnlyDictionary<string, string> sponsorByAge,
    Random random)
{
    private static readonly string[] PhotoExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public SKBitmap RandomHerrenPhoto() => RandomPhoto("Herren")
        ?? throw new InvalidOperationException($"Keine Fotos in {Path.Combine(imagesDirectory, "Herren")}");

    public SKBitmap RandomJugendPhoto() => RandomPhoto("Jugend") ?? RandomHerrenPhoto();

    public SKBitmap ClubLogo() => Decode(Path.Combine(imagesDirectory, "Logos", "Dachse.png"));

    public SKBitmap? SponsorLogo(string ageClass)
    {
        if (!sponsorByAge.TryGetValue(ageClass, out var file)) return null;
        var path = Path.Combine(imagesDirectory, "Sponsoren", file);
        return File.Exists(path) ? Decode(path) : null;
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
