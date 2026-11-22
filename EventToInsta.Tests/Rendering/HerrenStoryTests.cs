using EventToInsta.Data;
using EventToInsta.Domain;
using EventToInsta.Planning;
using EventToInsta.Rendering;
using SkiaSharp;

namespace EventToInsta.Tests.Rendering;

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

    // Mitte der Logo-Kreise im oberen Bereich (Entwurf x=78/222, y=150, Radius 47,5; mal 3,6).
    private const int LeftLogoX = 281;
    private const int RightLogoX = 799;
    private const int LogoY = 540;
    // Innerhalb des weißen Kreises (Radius 171), aber über dem eingezogenen Logo-Feld (halbe Kante 120).
    private const int DiscRimY = LogoY - 150;

    // Früherer Platz der Fuß-Logos (Rand 72, Logo 158,4 px): dort ist jetzt nur noch Blau.
    private const int OldFooterLogoY = 1769;

    private static bool IsWhite(SKColor p) => p.Red > 230 && p.Green > 230 && p.Blue > 230;
    private static bool IsRed(SKColor p) => p.Red > 200 && p.Green < 80 && p.Blue < 80;
    private static bool IsOrange(SKColor p) => p.Red > 200 && p.Green is > 110 and < 220 && p.Blue < 80;
    private static bool IsBlue(SKColor p) => p.Blue > 120 && p.Blue > p.Red + 60;

    /// <summary>Badge: weißer Kreis, in der Mitte weder Gegner-Testlogo (rot) noch Vereinslogo (orange).</summary>
    private static void AssertBadge(SKBitmap bmp, int x)
    {
        var top = bmp.GetPixel(x, DiscRimY);
        var mid = bmp.GetPixel(x, LogoY);
        Assert.True(IsWhite(top), $"weißer Kreis erwartet, war {top}");
        Assert.False(IsRed(mid) || IsOrange(mid), $"Badge erwartet, Mitte war {mid}");
    }

    private static void AssertClubLogo(SKBitmap bmp, int x)
    {
        var mid = bmp.GetPixel(x, LogoY);
        Assert.True(IsOrange(mid), $"Vereinslogo erwartet, war {mid}");
        Assert.True(IsWhite(bmp.GetPixel(x, DiscRimY)), "weißer Kreis hinter dem Vereinslogo erwartet");
    }

    /// <summary>Etikett oben links: immer weiß hinterlegt (Innenabstand links oben).</summary>
    private static void AssertWhiteTag(SKBitmap bmp)
    {
        var p = bmp.GetPixel(72, 72);
        Assert.True(IsWhite(p), $"weißes Etikett erwartet, war {p}");
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
        new(new AssetStore(_dir, new Dictionary<string, string>()), logos, "Bad Laer");

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
        Assert.True(IsWhite(bmp.GetPixel(RightLogoX, DiscRimY)), "weißer Kreis hinter dem Gegnerlogo erwartet");
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
    public async Task Result_LongOpponentName_FooterTextStaysInsideMargins()
    {
        var game = Herren(opponent: "Turn- und Sportverein Bad Rothenfelde von 1898 e.V. II", result: new Score(64, 78));
        var story = await Renderer(new FakeLogos(null)).RenderAsync(new HerrenResult(game));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        var textPixels = 0;
        for (var y = 1650; y < 1920; y += 2)
        for (var x = 0; x < 1080; x += 2)
        {
            if (!IsWhite(bmp.GetPixel(x, y))) continue;
            Assert.InRange(x, 72, 1008);
            Assert.InRange(y, 1650, 1848);
            textPixels++;
        }
        Assert.True(textPixels > 100, "Teamzeile im Fuß erwartet");
    }

    [Theory]
    [InlineData(true, "HERREN VS ", "")]
    [InlineData(false, "", " VS HERREN")]
    public void TeamsLine_LongOpponent_ShortensOnlyOpponentName(bool home, string prefix, string suffix)
    {
        var game = Herren(home, opponent: "Turn- und Sportverein Grün-Weiß Osnabrück-Haste von 1898 e.V. II");
        var style = new Topten.RichTextKit.Style { FontFamily = "Anton", FontSize = 54 };

        var line = HerrenStory.TeamsLine(game, style, 936, 0.75f);

        Assert.StartsWith(prefix, line);
        Assert.EndsWith(suffix, line);
        Assert.Contains("…", line);
        Assert.Contains("TURN- UND", line);
    }

    [Fact]
    public void TeamsLine_ShortOpponent_Unchanged()
    {
        var style = new Topten.RichTextKit.Style { FontFamily = "Anton", FontSize = 54 };

        Assert.Equal("SC MELLE II VS HERREN", HerrenStory.TeamsLine(Herren(home: false), style, 936, 0.75f));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Preview_FooterHasNoLogosAnyMore(bool home)
    {
        var story = await Renderer(new FakeLogos(LogoBytes())).RenderAsync(new HerrenPreview(Herren(home, place: "Melle")));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        foreach (var x in new[] { 80, 1000 })
        {
            var p = bmp.GetPixel(x, OldFooterLogoY);
            Assert.True(IsBlue(p), $"kein Logo im Fuß erwartet bei x={x}, war {p}");
        }
    }

    [Fact]
    public async Task Preview_TagIsWhite()
    {
        foreach (var home in new[] { true, false })
        {
            var story = await Renderer(new FakeLogos(null)).RenderAsync(new HerrenPreview(Herren(home)));
            using var bmp = SKBitmap.Decode(story.Jpeg);
            AssertWhiteTag(bmp);
        }
    }

    [Theory]
    [InlineData(78, 64)]
    [InlineData(64, 78)]
    public async Task Result_TagIsWhite_WinAndLoss(int home, int guest)
    {
        var story = await Renderer(new FakeLogos(null)).RenderAsync(new HerrenResult(Herren(result: new Score(home, guest))));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        AssertWhiteTag(bmp);
    }

    [Fact]
    public async Task Result_LogosInTopArea_HomeLeftGuestRight()
    {
        var game = Herren(home: false, result: new Score(70, 60));
        var story = await Renderer(new FakeLogos(LogoBytes())).RenderAsync(new HerrenResult(game));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        Assert.True(IsRed(bmp.GetPixel(LeftLogoX, LogoY)), "Gegnerlogo (Heim) links erwartet");
        AssertClubLogo(bmp, RightLogoX);
    }

    [Fact]
    public async Task Preview_TopAreaShowsCourtBackground_NotPhoto()
    {
        // Im Testordner liegt ein dunkelgraues Foto unter Images/Herren: es darf nicht mehr erscheinen.
        var story = await Renderer(new FakeLogos(null)).RenderAsync(new HerrenPreview(Herren()));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        var p = bmp.GetPixel(540, 250);
        Assert.True(IsBlue(p), $"Spielfeld-Hintergrund erwartet, war {p}");
    }

    [Fact]
    public async Task Preview_WithoutPhotoFolder_RendersSamePixelsEveryTime()
    {
        Directory.Delete(Path.Combine(_dir, "Herren"), true);
        var request = new HerrenPreview(Herren());

        var first = await Renderer(new FakeLogos(LogoBytes())).RenderAsync(request);
        var second = await Renderer(new FakeLogos(LogoBytes())).RenderAsync(request);

        Assert.Equal(first.Jpeg, second.Jpeg);
    }

    [Fact]
    public async Task Preview_VsBetweenLogos()
    {
        var story = await Renderer(new FakeLogos(null)).RenderAsync(new HerrenPreview(Herren()));

        using var bmp = SKBitmap.Decode(story.Jpeg);
        var white = 0;
        for (var y = LogoY - 40; y < LogoY + 40; y++)
        for (var x = 500; x < 580; x++)
            if (IsWhite(bmp.GetPixel(x, y))) white++;
        Assert.True(white > 200, "weißes „VS“ zwischen den Logos erwartet");
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
