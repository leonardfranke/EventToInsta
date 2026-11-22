using EventToInsta.Data;
using EventToInsta.Domain;
using EventToInsta.Planning;
using EventToInsta.Rendering;
using SkiaSharp;

namespace EventToInsta.Tests.Rendering;

[Collection("Fonts")]
public sealed class JugendStoryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "jugend-" + Guid.NewGuid());

    public JugendStoryTests()
    {
        Fonts.Register(TextFitTests.FontsDirectory);
        WritePng("Jugend/photo.png", 1200, 1600, SKColors.DarkSlateGray);
        WritePng("Logos/Dachse.png", 200, 200, SKColors.Orange);
        WritePng("Sponsoren/sponsor.png", 300, 120, SKColors.Red);
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

    private sealed class NoLogos : ILogoSource
    {
        public Task<byte[]?> LoadTeamLogoAsync(int teamPermanentId, CancellationToken ct = default) =>
            Task.FromResult<byte[]?>(null);
    }

    private StoryRenderer Renderer() =>
        new(new AssetStore(_dir, new Dictionary<string, string> { ["U12"] = "sponsor.png", ["U14"] = "sponsor.png" },
            new Random(1)), new NoLogos(), "Bad Laer");

    private static Game Jugend(
        int id, string age, bool home = true, string opponent = "SC Melle II", Score? result = null,
        string? place = null, DateTime? kickoff = null)
    {
        var us = new Team(1, "SV Bad Laer", "Bad Laer", 621);
        var them = new Team(2 + id, opponent, "SCM", 999);
        return new Game(id, age, "Kreisliga", kickoff ?? new DateTime(2026, 11, 28, 10, 0, 0),
            home ? us : them, home ? them : us, place, result, 621);
    }

    private static SKBitmap Decode(RenderedStory story)
    {
        var bmp = SKBitmap.Decode(story.Jpeg);
        Assert.NotNull(bmp);
        Assert.Equal(1080, bmp.Width);
        Assert.Equal(1920, bmp.Height);
        return bmp;
    }

    private static readonly DateOnly WeekStart = new(2026, 11, 23);
    private static readonly DateOnly WeekEnd = new(2026, 11, 29);

    [Fact]
    public async Task Preview_SixCards_ProducesJpeg1080x1920()
    {
        string[] ages = ["U10", "U12", "U14", "U16", "U18", "U12"];
        var games = ages.Select((a, i) => Jugend(i + 1, a, kickoff: new DateTime(2026, 11, 28, 10 + i, 0, 0))).ToList();

        var story = await Renderer().RenderAsync(new JugendPreview(true, WeekStart, WeekEnd, games, 1, 2));

        Assert.Matches(@"^jugend-preview-heim-1-[0-9a-f]{8}\.jpg$", story.FileName);
        using var _ = Decode(story);
    }

    [Fact]
    public async Task Preview_LongNameWithSponsor_DoesNotThrow()
    {
        var name = new string('W', 30) + " " + new string('m', 29);
        Assert.Equal(60, name.Length);
        var games = new[] { Jugend(1, "U12", home: false, opponent: name, place: "Quakenbrück") };

        var story = await Renderer().RenderAsync(new JugendPreview(false, WeekStart, WeekEnd, games, 1, 1));

        Assert.Matches(@"^jugend-preview-auswaerts-1-[0-9a-f]{8}\.jpg$", story.FileName);
        using var _ = Decode(story);
    }

    [Fact]
    public async Task Result_MixedHomeAway_DoesNotThrow()
    {
        var games = new[]
        {
            Jugend(1, "U12", home: true, result: new Score(54, 38)),
            Jugend(2, "U14", home: false, result: new Score(47, 41)),
            Jugend(3, "U16", home: false, result: new Score(59, 66)),
            Jugend(4, "U18", home: true, result: new Score(70, 72)),
        };

        var story = await Renderer().RenderAsync(new JugendResult(new DateOnly(2026, 11, 28), games, 1, 1));

        Assert.Matches(@"^jugend-result-2026-11-28-1-[0-9a-f]{8}\.jpg$", story.FileName);
        using var _ = Decode(story);
    }

    [Fact]
    public async Task Result_WinHasBlueBorder_LossHasGreyBorder()
    {
        var games = new[]
        {
            Jugend(1, "U12", home: true, result: new Score(54, 38)),   // Sieg
            Jugend(2, "U14", home: false, result: new Score(47, 41)),  // Niederlage (Bad Laer Gast mit 41)
        };

        var story = await Renderer().RenderAsync(new JugendResult(new DateOnly(2026, 11, 28), games, 1, 1));

        using var bmp = Decode(story);
        var cards = JugendStory.Layout(2).Cards;
        var win = bmp.GetPixel((int)cards[0].Left + 8, (int)cards[0].MidY);
        var loss = bmp.GetPixel((int)cards[1].Left + 8, (int)cards[1].MidY);
        Assert.True(win.Blue > 130 && win.Red < 50, $"blauer Rand erwartet, war {win}");
        Assert.True(loss.Red > 150 && loss.Green > 160 && loss.Blue > 180, $"grauer Rand erwartet, war {loss}");
    }

    private static int CountRed(SKBitmap bmp, SKRect card)
    {
        var count = 0;
        for (var y = (int)card.Top; y < (int)card.Bottom; y += 2)
        for (var x = (int)card.Left; x < (int)card.Right; x += 2)
        {
            var p = bmp.GetPixel(x, y);
            if (p.Red > 200 && p.Green < 80 && p.Blue < 80) count++;
        }
        return count;
    }

    [Fact]
    public async Task Result_WithSponsor_DrawsSponsorLogoOnlyForSponsoredAgeClass()
    {
        var games = new[]
        {
            Jugend(1, "U12", home: true, result: new Score(54, 38)),   // Sponsor (rotes Testlogo)
            Jugend(2, "U16", home: false, result: new Score(47, 41)),  // kein Sponsor
        };

        var story = await Renderer().RenderAsync(new JugendResult(new DateOnly(2026, 11, 28), games, 1, 1));

        using var bmp = Decode(story);
        var cards = JugendStory.Layout(2).Cards;
        Assert.True(CountRed(bmp, cards[0]) > 500, "Sponsorlogo in der U12-Karte erwartet");
        Assert.Equal(0, CountRed(bmp, cards[1]));
    }

    [Fact]
    public async Task Result_SixSponsoredCardsLongNamesThreeDigitScores_DoesNotThrow()
    {
        var name = "Turn- und Sportverein Bad Rothenfelde von 1898 e.V. Basketball";
        var games = Enumerable.Range(1, JugendStory.MaxCards)
            .Select(i => Jugend(i, i % 2 == 0 ? "U12" : "U14", home: i % 2 == 0, opponent: name,
                result: i % 3 == 0 ? new Score(100, 101) : new Score(101, 100)))
            .ToList();

        var story = await Renderer().RenderAsync(new JugendResult(new DateOnly(2026, 11, 28), games, 1, 1));

        using var bmp = Decode(story);
        var layout = JugendStory.Layout(games.Count);
        foreach (var card in layout.Cards)
            Assert.True(CountRed(bmp, card) > 200, "Sponsorlogo in jeder Karte erwartet");
        var middle = JugendStory.ResultMiddleBlock(layout.Cards[^1], sponsored: true);
        Assert.True(middle.Top >= layout.Cards[^1].Top && middle.Bottom <= layout.Cards[^1].Bottom,
            "Mittelblock mit Sponsorzeile ragt aus der Karte");
        Assert.True(layout.Cards[^1].Bottom < layout.FooterTop, "letzte Karte überlappt den Fuß");
    }

    [Fact]
    public async Task Result_WithSponsor_DrawsLogoInMiddleBlockBelowInfoLine()
    {
        var games = new[] { Jugend(1, "U12", home: true, result: new Score(54, 38)) };

        var story = await Renderer().RenderAsync(new JugendResult(new DateOnly(2026, 11, 28), games, 1, 1));

        using var bmp = Decode(story);
        var card = JugendStory.Layout(1).Cards[0];
        var middle = JugendStory.ResultMiddleBlock(card, sponsored: true);
        // Unterer Teil des Mittelblocks (Sponsorzeile) enthält das rote Testlogo, rechts davon (Spielstand) keines.
        var sponsorLine = new SKRect(middle.Left, middle.Bottom - 40, middle.Right, middle.Bottom);
        Assert.True(CountRed(bmp, sponsorLine) > 50, "Sponsorlogo unter der Infozeile erwartet");
        Assert.Equal(0, CountRed(bmp, new SKRect(middle.Right, card.Top, card.Right, card.Bottom)));
    }

    [Theory]
    [InlineData("@ TV Bad Rothenfelde")]
    [InlineData("@ Artland Dragons III")]
    public void ResultCardWithSponsor_CommonNamesFitWithoutEllipsis(string line)
    {
        // Ungünstigste Spielstand-Spalte („100:100“ / „NIEDERLAGE“); die Sponsorzeile kostet keine Breite.
        var card = JugendStory.Layout(1).Cards[0];
        var width = JugendStory.ResultMiddleBlock(card, sponsored: true).Width;
        var natural = StoryText.NaturalWidth(line, JugendStory.ResultNameStyle());

        Assert.True(natural <= width, $"„{line}“ braucht {natural:F0} px, verfügbar {width:F0} px");
    }

    [Fact]
    public void ScoreColumnWidth_IsSameForWinAndLossAndCoversWorstCase()
    {
        var win = JugendStory.ScoreColumnWidth([Jugend(1, "U12", result: new Score(54, 38))]);
        var loss = JugendStory.ScoreColumnWidth([Jugend(1, "U12", result: new Score(38, 54))]);

        Assert.Equal(win, loss);
        Assert.True(JugendStory.ScoreColumnWidth([Jugend(1, "U12", result: new Score(108, 100))]) >= win);
    }

    [Fact]
    public void Layout_MaxCards_FitsBetweenHeaderAndFooterWithoutOverlap()
    {
        var layout = JugendStory.Layout(JugendStory.MaxCards);

        Assert.Equal(6, JugendStory.MaxCards);
        Assert.Equal(JugendStory.MaxCards, layout.Cards.Count);
        Assert.True(layout.Cards[0].Top > layout.HeaderBottom, "erste Karte überlappt den Kopf");
        Assert.True(layout.Cards[^1].Bottom < layout.FooterTop, "letzte Karte überlappt den Fuß");
        for (var i = 1; i < layout.Cards.Count; i++)
            Assert.True(layout.Cards[i].Top > layout.Cards[i - 1].Bottom, $"Karte {i + 1} überlappt Karte {i}");
    }

    [Fact]
    public void Preview_AwayPlaceEqualsHomePlace_NoPlaceSuffix()
    {
        var kickoff = new DateTime(2026, 11, 28, 10, 0, 0);

        Assert.Equal("Sa 28.11. · 10:00 Uhr",
            JugendStory.PreviewInfoLine(Jugend(1, "U12", home: false, place: "bad laer", kickoff: kickoff), "Bad Laer"));
        Assert.Equal("Sa 28.11. · 10:00 Uhr",
            JugendStory.PreviewInfoLine(Jugend(1, "U12", home: true, place: null, kickoff: kickoff), "Bad Laer"));
        Assert.Equal("Sa 28.11. · 10:00 Uhr · in Dissen",
            JugendStory.PreviewInfoLine(Jugend(1, "U12", home: false, place: "Dissen", kickoff: kickoff), "Bad Laer"));
    }

    [Theory]
    [InlineData(2026, 11, 23, 2026, 11, 29, 1, 1, "Diese Woche · 23.–29.11.")]
    [InlineData(2026, 11, 30, 2026, 12, 6, 1, 1, "Diese Woche · 30.11.–06.12.")]
    [InlineData(2026, 11, 23, 2026, 11, 29, 2, 3, "Diese Woche · 23.–29.11. (2/3)")]
    public void PreviewDateLine_FormatsWeekRange(int y1, int m1, int d1, int y2, int m2, int d2, int part, int count, string expected)
    {
        var r = new JugendPreview(true, new DateOnly(y1, m1, d1), new DateOnly(y2, m2, d2), [], part, count);

        Assert.Equal(expected, JugendStory.PreviewDateLine(r));
    }

    [Theory]
    [InlineData(1, 1, "Samstag, 28.11.")]
    [InlineData(1, 2, "Samstag, 28.11. (1/2)")]
    public void ResultDateLine_FormatsDay(int part, int count, string expected)
    {
        var r = new JugendResult(new DateOnly(2026, 11, 28), [], part, count);

        Assert.Equal(expected, JugendStory.ResultDateLine(r));
    }
}
