using EventToInsta.Data;
using EventToInsta.Domain;
using EventToInsta.Planning;
using EventToInsta.Rendering;
using SkiaSharp;

namespace EventToInsta.Tests.Rendering;

using Team = EventToInsta.Domain.Team;

[Collection("Fonts")]
public sealed class HerrenStoryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "herren-" + Guid.NewGuid());

    public HerrenStoryTests()
    {
        Fonts.Register(TextFitTests.FontsDirectory);
        WritePng("Herren/photo.png", 1600, 1200, SKColors.DarkSlateGray);
        WritePng("Logos/Dachse.png", 200, 200, SKColors.Orange);
    }

    public void Dispose() => Directory.Delete(_dir, true);

    private void WritePng(string relative, int w, int h, SKColor color)
    {
        var path = Path.Combine(_dir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var bmp = new SKBitmap(w, h);
        using (var c = new SKCanvas(bmp)) c.Clear(color);
        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    private sealed class FakeLogos(byte[]? logo) : ILogoSource
    {
        public int Calls { get; private set; }

        public Task<byte[]?> LoadTeamLogoAsync(int teamPermanentId, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(logo);
        }
    }

    private static byte[] LogoBytes()
    {
        using var bmp = new SKBitmap(120, 80);
        using (var c = new SKCanvas(bmp)) c.Clear(SKColors.Red);
        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private StoryRenderer Renderer(ILogoSource logos) =>
        new(new AssetStore(_dir, new Dictionary<string, string>(), new Random(1)), logos, "Bad Laer");

    private static Game Herren(bool home = true, string opponent = "SC Melle II", Score? result = null, string? place = null)
    {
        var us = new Team(1, "SV Bad Laer", "Bad Laer", 621);
        var them = new Team(2, opponent, "SCM", 999);
        return new Game(4711, "Senioren", "Bezirksliga", new DateTime(2026, 11, 28, 19, 30, 0),
            home ? us : them, home ? them : us, place, result, 621);
    }

    private static void AssertStory(RenderedStory story)
    {
        using var bmp = SKBitmap.Decode(story.Jpeg);
        Assert.NotNull(bmp);
        Assert.Equal(1080, bmp.Width);
        Assert.Equal(1920, bmp.Height);
    }

    [Fact]
    public async Task Preview_ProducesJpeg1080x1920()
    {
        var story = await Renderer(new FakeLogos(LogoBytes())).RenderAsync(new HerrenPreview(Herren()));

        Assert.Equal("herren-preview-4711.jpg", story.FileName);
        AssertStory(story);
    }

    [Fact]
    public async Task Preview_OpponentWithoutLogo_DoesNotThrow()
    {
        var logos = new FakeLogos(null);
        var story = await Renderer(logos).RenderAsync(new HerrenPreview(Herren()));

        Assert.Equal(1, logos.Calls);
        AssertStory(story);
        // Gegner (Gast) steht rechts im Fuß: Badge ist ein weißer Kreis.
        using var bmp = SKBitmap.Decode(story.Jpeg);
        var px = bmp.GetPixel(1080 - 72 - 79, 1768 - 60);
        Assert.True(px.Red > 230 && px.Green > 230 && px.Blue > 230, $"Badge erwartet, war {px}");
    }

    [Fact]
    public async Task Result_LongOpponentName_DoesNotThrow()
    {
        var game = Herren(opponent: "Turn- und Sportverein Bad Rothenfelde von 1898 e.V. II", result: new Score(64, 78));
        var story = await Renderer(new FakeLogos(null)).RenderAsync(new HerrenResult(game));

        Assert.Equal("herren-result-4711.jpg", story.FileName);
        AssertStory(story);
    }

    [Fact]
    public async Task Preview_Away_FileNameAndNoException()
    {
        var game = Herren(home: false, opponent: "Artland Dragons III", place: "Quakenbrück");
        var story = await Renderer(new FakeLogos(LogoBytes())).RenderAsync(new HerrenPreview(game));

        Assert.Equal("herren-preview-4711.jpg", story.FileName);
        AssertStory(story);
    }

    [Fact]
    public async Task JugendRequest_NotSupportedYet()
    {
        var request = new JugendResult(new DateOnly(2026, 11, 28), [], 1, 1);
        await Assert.ThrowsAsync<NotSupportedException>(() => Renderer(new FakeLogos(null)).RenderAsync(request));
    }
}
