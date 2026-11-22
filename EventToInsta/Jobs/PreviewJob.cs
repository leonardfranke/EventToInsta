using EventToInsta.Data;
using EventToInsta.Planning;
using EventToInsta.Publishing;
using EventToInsta.Rendering;
using EventToInsta.State;
using EventToInsta.Time;

namespace EventToInsta.Jobs;

public sealed class PreviewJob(
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
        var today = DateOnly.FromDateTime(clock.Now);
        var week = PostedState.IsoWeek(today);
        var current = await state.LoadAsync(ct);
        if (current.PreviewWeek == week)
        {
            log($"Vorschau {week} bereits gepostet, übersprungen.");
            return;
        }

        var requests = PreviewPlanner.Plan(await games.LoadGamesAsync(ct), today, maxCards);
        if (requests.Count == 0)
        {
            log("Keine Spiele für die Vorschau, nichts zu posten.");
            return;
        }

        // Erst alles rendern: ein Renderfehler darf nie zu einem halben Beitrag führen.
        var images = new List<RenderedStory>();
        foreach (var request in requests)
            images.Add(await renderer.RenderAsync(request, ct));

        var key = $"preview-{week}";
        await publisher.PublishAsync(new StoryPost(key, images), ct);
        log($"Veröffentlicht: {key}");
        // Zustand erst nach erfolgreichem Publish speichern, und zwar unabhängig vom Abbruch:
        // Der Beitrag ist öffentlich, die Buchführung darf nicht ausfallen (sonst Duplikat).
        await state.SaveAsync(current.WithPreviewWeek(week), CancellationToken.None);
    }
}
