using EventToInsta.Rendering;
using SkiaSharp;

namespace EventToInsta.Tests.Rendering;

public sealed class AssetStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "assets-" + Guid.NewGuid());

    public AssetStoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, true);

    private void WritePng(string relative)
    {
        var path = Path.Combine(_dir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var bmp = new SKBitmap(10, 10);
        using var data = SKImage.FromBitmap(bmp).Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    private AssetStore Store(Dictionary<string, string>? sponsors = null) =>
        new(_dir, sponsors ?? new Dictionary<string, string>(), new Random(1));

    [Fact]
    public void AssetStore_EmptyJugendFolder_FallsBackToHerren()
    {
        WritePng("Herren/x.png");
        Directory.CreateDirectory(Path.Combine(_dir, "Jugend"));

        using var photo = Store().RandomJugendPhoto();

        Assert.Equal(10, photo.Width);
    }

    [Fact]
    public void AssetStore_MissingJugendFolder_FallsBackToHerren()
    {
        WritePng("Herren/x.png");

        using var photo = Store().RandomJugendPhoto();

        Assert.Equal(10, photo.Width);
    }

    [Fact]
    public void AssetStore_UnknownAge_NoSponsor()
    {
        WritePng("Sponsoren/a.png");

        Assert.Null(Store(new() { ["U12"] = "a.png" }).SponsorLogo("U99"));
    }

    [Fact]
    public void AssetStore_ConfiguredSponsorFileMissing_NullAndWarningLogged()
    {
        var log = new List<string>();
        var store = new AssetStore(_dir, new Dictionary<string, string> { ["U12"] = "fehlt.png" }, new Random(1), log.Add);

        Assert.Null(store.SponsorLogo("U12"));
        Assert.Contains(log, l => l.Contains("fehlt.png") && l.Contains("U12"));
    }

    [Fact]
    public void AssetStore_KnownAge_LoadsSponsor()
    {
        WritePng("Sponsoren/a.png");

        using var logo = Store(new() { ["U12"] = "a.png" }).SponsorLogo("U12");

        Assert.NotNull(logo);
    }
}
