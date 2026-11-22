using EventToInsta.Domain;
using EventToInsta.Planning;

namespace EventToInsta.Tests.Planning;

public class PreviewPlannerTests
{
    // Mittwoch 18.11.2026, Wochenende ist Sonntag 22.11.2026.
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static DateTime At(int day, int hour = 12) => new(2026, 11, day, hour, 0, 0);

    [Fact]
    public void Preview_SplitsHerrenHomeAwayAndJugendGroups()
    {
        var games = new[]
        {
            GameFactory.Make(5, At(21), home: false),
            GameFactory.Make(4, At(20), home: true),
            GameFactory.Make(3, At(19), herren: true, home: false),
            GameFactory.Make(2, At(21, 10), herren: true, home: true),
            GameFactory.Make(1, At(20, 10), home: true),
        };
        var plan = PreviewPlanner.Plan(games, Today, 6);
        Assert.Equal(4, plan.Count);
        Assert.Equal("herren-preview-2.jpg", Assert.IsType<HerrenPreview>(plan[0]).FileName);
        Assert.Equal("herren-preview-3.jpg", Assert.IsType<HerrenPreview>(plan[1]).FileName);
        var h = Assert.IsType<JugendPreview>(plan[2]);
        Assert.True(h.Home);
        Assert.Matches(@"^jugend-preview-heim-1-[0-9a-f]{8}\.jpg$", h.FileName);
        Assert.Equal(new[] { 1, 4 }, h.Games.Select(g => g.MatchId).ToArray());
        var a = Assert.IsType<JugendPreview>(plan[3]);
        Assert.False(a.Home);
        Assert.Matches(@"^jugend-preview-auswaerts-1-[0-9a-f]{8}\.jpg$", a.FileName);
        Assert.Equal(Today, a.WeekStart);
        Assert.Equal(new DateOnly(2026, 11, 22), a.WeekEnd);
    }

    [Fact]
    public void Preview_IgnoresGamesOutsideWeek()
    {
        var games = new[]
        {
            GameFactory.Make(1, At(23), herren: true),
            GameFactory.Make(2, At(18, 8), herren: true),
            GameFactory.Make(3, At(17), herren: true),
            GameFactory.Make(4, At(22, 23), herren: true),
        };
        var plan = PreviewPlanner.Plan(games, Today, 6);
        Assert.Equal(new[] { "herren-preview-2.jpg", "herren-preview-4.jpg" }, plan.Select(p => p.FileName).ToArray());
    }

    [Fact]
    public void Preview_SundayWindowIsJustToday()
    {
        var games = new[] { GameFactory.Make(1, At(22), herren: true), GameFactory.Make(2, At(23), herren: true) };
        var plan = PreviewPlanner.Plan(games, new DateOnly(2026, 11, 22), 6);
        Assert.Single(plan);
    }

    [Fact]
    public void Preview_EmptyGroupsProduceNoRequest()
    {
        var plan = PreviewPlanner.Plan(new[] { GameFactory.Make(1, At(20)) }, Today, 6);
        var p = Assert.IsType<JugendPreview>(Assert.Single(plan));
        Assert.True(p.Home);
    }

    [Fact]
    public void Preview_SevenJugendHome_TwoPartsFourAndThree()
    {
        var games = Enumerable.Range(1, 7).Select(i => GameFactory.Make(i, At(20, 8 + i))).ToArray();
        var plan = PreviewPlanner.Plan(games, Today, 6).Cast<JugendPreview>().ToArray();
        Assert.Equal(2, plan.Length);
        Assert.Equal(new[] { 4, 3 }, plan.Select(p => p.Games.Count).ToArray());
        Assert.Equal(new[] { 1, 2 }, plan.Select(p => p.Part).ToArray());
        Assert.All(plan, p => Assert.Equal(2, p.PartCount));
        Assert.Matches(@"^jugend-preview-heim-2-[0-9a-f]{8}\.jpg$", plan[1].FileName);
    }
}
