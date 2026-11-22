using EventToInsta.Domain;
using EventToInsta.Planning;
using EventToInsta.State;

namespace EventToInsta.Tests.Planning;

public class ResultsPlannerTests
{
    private static readonly DateOnly Day = new(2026, 11, 22);
    private static readonly Score S = new(50, 40);
    private static DateTime At(int hour, int day = 22) => new(2026, 11, day, hour, 0, 0);
    private static PostedState Fresh => PostedState.Empty.ForDay(Day);

    [Fact]
    public void Results_HerrenWithResult_NotYetPosted_IsPlanned()
    {
        var plan = ResultsPlanner.Plan(new[] { GameFactory.Make(1, At(14), herren: true, result: S) }, At(17), Fresh, 6);
        var r = Assert.Single(plan.Herren);
        Assert.Equal("herren-result-1.jpg", r.FileName);
        Assert.Empty(plan.Jugend);
    }

    [Fact]
    public void Results_HerrenAlreadyInState_Skipped()
    {
        var games = new[]
        {
            GameFactory.Make(1, At(14), herren: true, result: S),
            GameFactory.Make(2, At(16), herren: true, result: S),
        };
        var plan = ResultsPlanner.Plan(games, At(18), Fresh.WithHerren(1), 6);
        Assert.Equal(2, Assert.Single(plan.Herren).Game.MatchId);
    }

    [Fact]
    public void Results_JugendNotAllDone_Before23_NothingPlanned()
    {
        var games = new[] { GameFactory.Make(1, At(10), result: S), GameFactory.Make(2, At(16)) };
        Assert.Empty(ResultsPlanner.Plan(games, At(18), Fresh, 6).Jugend);
    }

    [Fact]
    public void Results_JugendAllDone_Planned()
    {
        var games = new[] { GameFactory.Make(2, At(11), result: S), GameFactory.Make(1, At(10), result: S) };
        var j = Assert.Single(ResultsPlanner.Plan(games, At(15), Fresh, 6).Jugend);
        Assert.Matches(@"^jugend-result-2026-11-22-1-[0-9a-f]{8}\.jpg$", j.FileName);
        Assert.Equal(Day, j.Day);
        Assert.Equal(new[] { 1, 2 }, j.Games.Select(g => g.MatchId).ToArray());
        Assert.Equal(1, j.PartCount);
    }

    [Fact]
    public void Results_JugendAt23_PostsOnlyThoseWithResult()
    {
        var games = new[] { GameFactory.Make(1, At(10), result: S), GameFactory.Make(2, At(16)) };
        var j = Assert.Single(ResultsPlanner.Plan(games, At(23), Fresh, 6).Jugend);
        Assert.Equal(new[] { 1 }, j.Games.Select(g => g.MatchId).ToArray());
    }

    [Fact]
    public void Results_JugendAt23_NoneWithResult_NothingPlanned()
    {
        var games = new[] { GameFactory.Make(1, At(10)), GameFactory.Make(2, At(16)) };
        Assert.Empty(ResultsPlanner.Plan(games, At(23), Fresh, 6).Jugend);
    }

    [Fact]
    public void Results_JugendAlreadyPosted_NothingPlanned()
    {
        var games = new[] { GameFactory.Make(1, At(10), result: S) };
        Assert.Empty(ResultsPlanner.Plan(games, At(15), Fresh.WithJugendPosted(), 6).Jugend);
    }

    [Fact]
    public void Results_IgnoresYesterdaysGames()
    {
        var games = new[]
        {
            GameFactory.Make(1, At(10, 21), result: S),
            GameFactory.Make(2, At(10, 21), herren: true, result: S),
        };
        var plan = ResultsPlanner.Plan(games, At(15), Fresh, 6);
        Assert.Empty(plan.Jugend);
        Assert.Empty(plan.Herren);
    }

    [Fact]
    public void Results_HerrenPendingDoesNotBlockJugend()
    {
        var games = new[] { GameFactory.Make(1, At(14), herren: true), GameFactory.Make(2, At(10), result: S) };
        var plan = ResultsPlanner.Plan(games, At(15), Fresh, 6);
        Assert.Empty(plan.Herren);
        Assert.Single(plan.Jugend);
    }

    [Fact]
    public void Results_ManyJugend_SplitIntoParts()
    {
        var games = Enumerable.Range(1, 7).Select(i => GameFactory.Make(i, At(8 + i), result: S)).ToArray();
        var plan = ResultsPlanner.Plan(games, At(20), Fresh, 6);
        Assert.Equal(new[] { 4, 3 }, plan.Jugend.Select(j => j.Games.Count).ToArray());
        Assert.Matches(@"^jugend-result-2026-11-22-2-[0-9a-f]{8}\.jpg$", plan.Jugend[1].FileName);
    }
}
