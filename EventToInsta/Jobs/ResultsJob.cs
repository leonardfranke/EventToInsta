using System.Globalization;
using EventToInsta.Data;
using EventToInsta.Planning;
using EventToInsta.Publishing;
using EventToInsta.Rendering;
using EventToInsta.State;
using EventToInsta.Time;

namespace EventToInsta.Jobs;

public sealed class ResultsJob(
    IGameSource games,
    IStoryRenderer renderer,
    IStoryPublisher publisher,
    IPostedStateStore state,
    IClock clock,
    int maxCards,
    Action<string> log)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = clock.Now;
        var today = DateOnly.FromDateTime(now);
        var current = (await state.LoadAsync(ct)).ForDay(today);

        var plan = ResultsPlanner.Plan(await games.LoadGamesAsync(ct), now, current, maxCards);
        if (plan.Herren.Count == 0 && plan.Jugend.Count == 0)
        {
            log("Keine neuen Ergebnisse, nichts zu posten.");
            return;
        }

        var errors = new List<Exception>();

        foreach (var result in plan.Herren)
        {
            var key = $"herren-result-{result.Game.MatchId}";
            try
            {
                var image = await renderer.RenderAsync(result, ct);
                await publisher.PublishAsync(new StoryPost(key, [image]), ct);
                log($"Veröffentlicht: {key}");
                current = current.WithHerren(result.Game.MatchId);
                await state.SaveAsync(current, ct);
            }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                log($"Fehler bei {key}: {e.Message}");
                errors.Add(e);
            }
        }

        if (plan.Jugend.Count > 0)
        {
            var key = $"jugend-result-{today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
            try
            {
                var images = new List<RenderedStory>();
                foreach (var part in plan.Jugend)
                    images.Add(await renderer.RenderAsync(part, ct));
                await publisher.PublishAsync(new StoryPost(key, images), ct);
                log($"Veröffentlicht: {key}");
                current = current.WithJugendPosted();
                await state.SaveAsync(current, ct);
            }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                log($"Fehler bei {key}: {e.Message}");
                errors.Add(e);
            }
        }

        if (errors.Count > 0)
            throw new AggregateException("Mindestens ein Beitrag ist fehlgeschlagen.", errors);
    }
}
