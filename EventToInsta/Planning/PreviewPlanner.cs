using EventToInsta.Domain;

namespace EventToInsta.Planning;

public static class PreviewPlanner
{
    public static IReadOnlyList<StoryRequest> Plan(IReadOnlyList<Game> games, DateOnly today, int maxCardsPerImage)
    {
        var daysToSunday = ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;
        var weekEnd = today.AddDays(daysToSunday);

        var inWindow = games
            .Where(g =>
            {
                var d = DateOnly.FromDateTime(g.Kickoff);
                return d >= today && d <= weekEnd;
            })
            .OrderBy(g => g.Kickoff).ThenBy(g => g.MatchId)
            .ToList();

        var plan = new List<StoryRequest>();
        var herren = inWindow.Where(g => g.IsHerren).ToList();
        plan.AddRange(herren.Where(g => g.IsHome).Select(g => new HerrenPreview(g)));
        plan.AddRange(herren.Where(g => !g.IsHome).Select(g => new HerrenPreview(g)));

        var jugend = inWindow.Where(g => !g.IsHerren).ToList();
        foreach (var home in new[] { true, false })
        {
            var group = jugend.Where(g => g.IsHome == home).ToList();
            var parts = Chunker.SplitEvenly(group, maxCardsPerImage);
            for (var i = 0; i < parts.Count; i++)
                plan.Add(new JugendPreview(home, today, weekEnd, parts[i], i + 1, parts.Count));
        }
        return plan;
    }
}
