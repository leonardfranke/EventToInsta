using EventToInsta.Domain;
using EventToInsta.State;

namespace EventToInsta.Planning;

public record ResultsPlan(IReadOnlyList<HerrenResult> Herren, IReadOnlyList<JugendResult> Jugend);

public static class ResultsPlanner
{
    public const int FinalHour = 23;

    /// <param name="state">Bereits per ForDay auf heute gesetzt.</param>
    public static ResultsPlan Plan(IReadOnlyList<Game> games, DateTime now, PostedState state, int maxCardsPerImage)
    {
        var day = DateOnly.FromDateTime(now);
        var today = games
            .Where(g => DateOnly.FromDateTime(g.Kickoff) == day)
            .OrderBy(g => g.Kickoff).ThenBy(g => g.MatchId)
            .ToList();

        var herren = today
            .Where(g => g.IsHerren && g.HasResult && !state.HerrenMatchIds.Contains(g.MatchId))
            .Select(g => new HerrenResult(g))
            .ToList();

        var jugend = new List<JugendResult>();
        var jugendToday = today.Where(g => !g.IsHerren).ToList();
        if (!state.JugendPosted
            && jugendToday.Count > 0
            && (jugendToday.All(g => g.HasResult) || now.Hour >= FinalHour))
        {
            var withResult = jugendToday.Where(g => g.HasResult).ToList();
            var parts = Chunker.SplitEvenly(withResult, maxCardsPerImage);
            for (var i = 0; i < parts.Count; i++)
                jugend.Add(new JugendResult(day, parts[i], i + 1, parts.Count));
        }
        return new ResultsPlan(herren, jugend);
    }
}
