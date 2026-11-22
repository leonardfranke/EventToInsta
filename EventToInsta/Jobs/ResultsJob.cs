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
    Action<string> log,
    TimeSpan? saveRetryDelay = null)
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
            var matchId = result.Game.MatchId;
            await PostAsync($"herren-result-{matchId}", [result], s => s.WithHerren(matchId));
        }

        if (plan.Jugend.Count > 0)
            await PostAsync($"jugend-result-{today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
                plan.Jugend, s => s.WithJugendPosted());

        if (errors.Count > 0)
            throw new AggregateException("Mindestens ein Beitrag ist fehlgeschlagen.", errors);

        // Ein Beitrag: alle Bilder rendern, veröffentlichen, dann (getrennt davon) den Zustand speichern.
        async Task PostAsync(string key, IEnumerable<StoryRequest> requests, Func<PostedState, PostedState> markPosted)
        {
            try
            {
                var images = new List<RenderedStory>();
                foreach (var request in requests)
                    images.Add(await renderer.RenderAsync(request, ct));
                await publisher.PublishAsync(new StoryPost(key, images), ct);
                log($"Veröffentlicht: {key}");
            }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                log($"Fehler bei {key}: {e.Message}");
                errors.Add(e);
                return;
            }

            current = markPosted(current);
            try
            {
                // nach Publish nie abbrechen; StateSaver loggt die eigene Meldung selbst
                await StateSaver.SaveAfterPublishAsync(state, current, key, saveRetryDelay ?? StateSaver.DefaultDelay, log);
            }
            catch (StateNotSavedException e)
            {
                errors.Add(e);
            }
        }
    }
}
