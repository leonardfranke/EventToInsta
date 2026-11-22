using EventToInsta.Data;
using EventToInsta.Time;

namespace EventToInsta.Tests.Data;

public class SampleGameSourceTests
{
    [Theory]
    [InlineData(2026, 11, 23)] // Montag
    [InlineData(2026, 11, 26)] // Donnerstag
    [InlineData(2026, 11, 29)] // Sonntag
    public async Task Week_HasPreviewGamesWithinTodayToSunday(int y, int m, int d)
    {
        var now = new DateTime(y, m, d, 9, 0, 0);
        var games = await new SampleGameSource(new FixedClock(now), SampleSet.Week).LoadGamesAsync();

        var today = DateOnly.FromDateTime(now);
        var sunday = today.AddDays(((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7);
        Assert.All(games, g =>
        {
            var day = DateOnly.FromDateTime(g.Kickoff);
            Assert.InRange(day, today, sunday);
            Assert.True(g.Kickoff.Hour >= 7);
            Assert.False(g.HasResult);
        });

        var herren = games.Where(g => g.IsHerren).ToList();
        Assert.Equal(2, herren.Count);
        Assert.Single(herren, g => g.IsHome);
        Assert.Single(herren, g => g.Opponent.PermanentId == 0);

        var jugendHome = games.Where(g => !g.IsHerren && g.IsHome).ToList();
        Assert.Equal(7, jugendHome.Count);
        Assert.Single(jugendHome, g => g.Opponent.Name.Length == 60);
        Assert.Single(jugendHome, g => g.Place is not null && g.Place != "Bad Laer");
        Assert.Equal(2, games.Count(g => !g.IsHerren && !g.IsHome));
        Assert.Equal(games.Count, games.Select(g => g.MatchId).Distinct().Count());
    }

    [Fact]
    public async Task Today_HasResultsOnTheSameDay()
    {
        var now = new DateTime(2026, 11, 22, 23, 0, 0);
        var games = await new SampleGameSource(new FixedClock(now), SampleSet.Today).LoadGamesAsync();

        Assert.All(games, g => Assert.Equal(DateOnly.FromDateTime(now), DateOnly.FromDateTime(g.Kickoff)));
        var herren = Assert.Single(games, g => g.IsHerren);
        Assert.True(herren.IsWin);
        var jugend = games.Where(g => !g.IsHerren).ToList();
        Assert.Equal(4, jugend.Count(g => g.HasResult));
        Assert.Single(jugend, g => !g.HasResult);
        Assert.Contains(jugend, g => g.IsWin == true);
        Assert.Contains(jugend, g => g.IsWin == false);
    }

    [Fact]
    public async Task LogoSource_ReturnsClubLogoExceptForZero()
    {
        var logos = new SampleLogoSource([1, 2, 3]);

        Assert.Equal([1, 2, 3], await logos.LoadTeamLogoAsync(4711));
        Assert.Null(await logos.LoadTeamLogoAsync(0));
    }
}
