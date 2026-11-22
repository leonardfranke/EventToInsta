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

    private sealed class ThrowingLogos : ILogoSource
    {
        public Task<byte[]?> LoadTeamLogoAsync(int teamPermanentId, CancellationToken ct = default) =>
            throw new HttpRequestException("Logo-Server nicht erreichbar");
    }

    // Mitte der Logo-Felder im Fuß (Rand 72, Logo 158,4 px, unten 72 px Abstand).
    private const int LeftLogoX = 151;
    private const int RightLogoX = 929;
    private const int LogoY = 1769;

    private static bool IsWhite(SKColor p) => p.Red > 230 && p.Green > 230 && p.Blue > 230;
    private static bool IsRed(SKColor p) => p.Red > 200 && p.Green < 80 && p.Blue < 80;
    private static bool IsOrange(SKColor p) => p.Red > 200 && p.Green is > 110 and < 220 && p.Blue < 80;

    /// <summary>Badge: weißer Kreis, in der Mitte weder Gegner-Testlogo (rot) noch Vereinslogo (orange).</summary>
    private static void AssertBadge(SKBitmap bmp, int x)
    {
        var top = bmp.GetPixel(x, LogoY - 60);
        var mid = bmp.GetPixel(x, LogoY);
        Assert.True(IsWhite(top), $"weißer Kreis erwartet, war {top}");
        Assert.False(IsRed(mid) || IsOrange(mid), $"Badge erwartet, Mitte war {mid}");
    }

    private static void AssertClubLogo(SKBitmap bmp, int x)
    {
        var mid = bmp.GetPixel(x, LogoY);
        Assert.True(IsOrange(mid), $"Vereinslogo erwartet, war {mid}");
        Assert.True(IsWhite(bmp.GetPixel(x, LogoY - 60)), "weißer Kreis hinter dem Vereinslogo erwartet");
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
        // Heimspiel: Bad Laer links, Gegner (Gast) rechts als Badge.
        using var bmp = SKBitmap.Decode(story.Jpeg);
        AssertClubLogo(bmp, LeftLogoX);
        AssertBadge(bmp, RightLogoX);
    }

    [Fact]
    public async Task Preview_OpponentLogo_DrawnOnWhiteDisc()
    {
        var story = await Renderer(new FakeLogos(LogoBytes())).RenderAsync(new HerrenPreview(Herren()));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        Assert.True(IsRed(bmp.GetPixel(RightLogoX, LogoY)), "Gegnerlogo erwartet");
        Assert.True(IsWhite(bmp.GetPixel(RightLogoX, LogoY - 60)), "weißer Kreis hinter dem Gegnerlogo erwartet");
    }

    [Fact]
    public async Task Preview_Away_HomeOpponentLeft_ClubRight()
    {
        var game = Herren(home: false, place: "Melle");
        var story = await Renderer(new FakeLogos(null)).RenderAsync(new HerrenPreview(game));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        AssertBadge(bmp, LeftLogoX);
        AssertClubLogo(bmp, RightLogoX);
    }

    [Fact]
    public async Task Preview_UndecodableLogoBytes_FallsBackToBadge()
    {
        var story = await Renderer(new FakeLogos([1, 2, 3])).RenderAsync(new HerrenPreview(Herren()));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        AssertBadge(bmp, RightLogoX);
    }

    [Fact]
    public async Task Preview_LogoSourceThrows_FallsBackToBadge()
    {
        var story = await Renderer(new ThrowingLogos()).RenderAsync(new HerrenPreview(Herren()));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        AssertBadge(bmp, RightLogoX);
    }

    [Fact]
    public async Task Preview_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var logos = new CancellingLogos();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Renderer(logos).RenderAsync(new HerrenPreview(Herren()), cts.Token));
    }

    private sealed class CancellingLogos : ILogoSource
    {
        public Task<byte[]?> LoadTeamLogoAsync(int teamPermanentId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<byte[]?>(null);
        }
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
}
