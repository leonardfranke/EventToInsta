using EventToInsta.Jobs;
using EventToInsta.State;
using EventToInsta.Tests.Planning;
using EventToInsta.Time;

namespace EventToInsta.Tests.Jobs;

public class PreviewJobTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 9, 0, 0); // Sonntag
    private static readonly string Week = PostedState.IsoWeek(new DateOnly(2026, 11, 22));

    private static PreviewJob Job(FakeGameSource g, FakeRenderer r, RecordingPublisher p, InMemoryStateStore s,
        List<string>? log = null) =>
        new(g, r, p, s, new FixedClock(Now), 6, m => log?.Add(m), saveRetryDelay: TimeSpan.Zero);

    private static FakeGameSource TwoGames() => new(
        GameFactory.Make(1, Now.AddHours(3), herren: true),
        GameFactory.Make(2, Now.AddHours(4), herren: false));

    [Fact]
    public async Task Preview_AlreadyPostedThisWeek_PublishesNothing()
    {
        var store = new InMemoryStateStore(PostedState.Empty.WithPreviewWeek(Week));
        var pub = new RecordingPublisher();
        await Job(TwoGames(), new FakeRenderer(), pub, store).RunAsync();
        Assert.Equal(0, pub.Calls);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task Preview_RendersAllBeforePublishing()
    {
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Job(TwoGames(), new FakeRenderer(n => n == 2), pub, store).RunAsync());
        Assert.Equal(0, pub.Calls);
        Assert.Equal(0, store.Saves);
        Assert.Equal(PostedState.Empty, store.Current);
    }

    [Fact]
    public async Task Preview_PublishFails_StateNotSaved_Throws()
    {
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher(_ => true);
        await Assert.ThrowsAsync<EventToInsta.Publishing.UploadFailedException>(() =>
            Job(TwoGames(), new FakeRenderer(), pub, store).RunAsync());
        Assert.Equal(0, store.Saves);
        Assert.Null(store.Current.PreviewWeek);
    }

    [Fact]
    public async Task Preview_SavesOnlyAfterPublish()
    {
        var store = new InMemoryStateStore();
        var savesAtPublish = -1;
        var pub = new RecordingPublisher(onPublishStart: _ => savesAtPublish = store.Saves);
        await Job(TwoGames(), new FakeRenderer(), pub, store).RunAsync();
        Assert.Equal(0, savesAtPublish);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task Preview_CancelledAfterPublish_StateStillSaved()
    {
        using var cts = new CancellationTokenSource();
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher(afterRecorded: _ => cts.Cancel());
        await Job(TwoGames(), new FakeRenderer(), pub, store).RunAsync(cts.Token);
        Assert.Equal(Week, store.Current.PreviewWeek);
    }

    [Fact]
    public async Task Preview_SaveFailsTwiceThenSucceeds_SavedWithoutError()
    {
        var store = new InMemoryStateStore(failSaves: 2);
        var log = new List<string>();
        var pub = new RecordingPublisher();
        await Job(TwoGames(), new FakeRenderer(), pub, store, log).RunAsync();
        Assert.Equal(3, store.SaveAttempts);
        Assert.Equal(Week, store.Current.PreviewWeek);
        Assert.Single(pub.Posts);
        Assert.DoesNotContain(log, l => l.Contains("NICHT gespeichert"));
    }

    [Fact]
    public async Task Preview_SaveAlwaysFails_DistinctMessageLoggedAndThrown()
    {
        var store = new InMemoryStateStore(failSaves: int.MaxValue);
        var log = new List<string>();
        var pub = new RecordingPublisher();
        var ex = await Assert.ThrowsAsync<StateNotSavedException>(() =>
            Job(TwoGames(), new FakeRenderer(), pub, store, log).RunAsync());
        var expected = $"VERÖFFENTLICHT, Zustand NICHT gespeichert – preview-{Week} – nicht erneut auslösen";
        Assert.StartsWith(expected, ex.Message);
        Assert.IsType<IOException>(ex.InnerException);
        Assert.Contains(log, l => l.StartsWith(expected));
        Assert.Equal(3, store.SaveAttempts);
        Assert.Equal(1, pub.Calls);
    }

    [Fact]
    public async Task Preview_NoGames_NoPublish_NoStateSaved()
    {
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher();
        await Job(new FakeGameSource(), new FakeRenderer(), pub, store).RunAsync();
        Assert.Equal(0, pub.Calls);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task Preview_Success_SavesWeek()
    {
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher();
        await Job(TwoGames(), new FakeRenderer(), pub, store).RunAsync();
        var post = Assert.Single(pub.Posts);
        Assert.Equal($"preview-{Week}", post.Key);
        Assert.Equal(2, post.Images.Count);
        Assert.Equal(Week, store.Current.PreviewWeek);
    }
}
