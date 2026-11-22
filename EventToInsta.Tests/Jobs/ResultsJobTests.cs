using EventToInsta.Domain;
using EventToInsta.Jobs;
using EventToInsta.State;
using EventToInsta.Tests.Planning;
using EventToInsta.Time;

namespace EventToInsta.Tests.Jobs;

public class ResultsJobTests
{
    private static readonly Score S = new(50, 40);
    private static DateTime At(int hour, int day = 22) => new(2026, 11, day, hour, 0, 0);

    private static ResultsJob Job(FakeGameSource g, RecordingPublisher p, InMemoryStateStore s, DateTime now,
        List<string>? log = null) =>
        new(g, new FakeRenderer(), p, s, new FixedClock(now), 6, m => log?.Add(m), saveRetryDelay: TimeSpan.Zero);

    private static FakeGameSource Games() => new(
        GameFactory.Make(1, At(14), herren: true, result: S),
        GameFactory.Make(2, At(10), result: S));

    [Fact]
    public async Task Results_HerrenPublished_StateSavedBeforeJugend()
    {
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher(k => k.StartsWith("jugend"));
        var ex = await Assert.ThrowsAsync<AggregateException>(() => Job(Games(), pub, store, At(18)).RunAsync());
        Assert.Single(ex.InnerExceptions);
        Assert.Contains(1, store.Current.HerrenMatchIds);
        Assert.False(store.Current.JugendPosted);
        Assert.Equal(new[] { "herren-result-1" }, pub.Posts.Select(p => p.Key));
    }

    [Fact]
    public async Task Results_StateSavedAfterEachPublish_NotOnlyAtEnd()
    {
        var store = new InMemoryStateStore();
        var seen = new List<(string Key, bool HerrenSaved, int Saves)>();
        var pub = new RecordingPublisher(onPublishStart: k =>
            seen.Add((k, store.Current.HerrenMatchIds.Contains(1), store.Saves)));
        await Job(Games(), pub, store, At(18)).RunAsync();
        Assert.Equal(("herren-result-1", false, 0), seen[0]);
        Assert.Equal(("jugend-result-2026-11-22", true, 1), seen[1]);
        Assert.Equal(2, store.Saves);
    }

    [Fact]
    public async Task Results_CancelledAfterPublish_StateStillSaved()
    {
        using var cts = new CancellationTokenSource();
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher(afterRecorded: _ => cts.Cancel());
        var g = new FakeGameSource(GameFactory.Make(1, At(14), herren: true, result: S));
        await Job(g, pub, store, At(18)).RunAsync(cts.Token);
        Assert.Contains(1, store.Current.HerrenMatchIds);
    }

    [Fact]
    public async Task Results_HerrenFails_JugendStillPosted()
    {
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher(k => k.StartsWith("herren"));
        await Assert.ThrowsAsync<AggregateException>(() => Job(Games(), pub, store, At(18)).RunAsync());
        Assert.Empty(store.Current.HerrenMatchIds);
        Assert.True(store.Current.JugendPosted);
        Assert.Equal("jugend-result-2026-11-22", Assert.Single(pub.Posts).Key);
    }

    [Fact]
    public async Task Results_SecondRunSameDay_DoesNotRepostHerren()
    {
        var store = new InMemoryStateStore();
        var pub = new RecordingPublisher();
        await Job(Games(), pub, store, At(18)).RunAsync();
        Assert.Equal(2, pub.Posts.Count);
        await Job(Games(), pub, store, At(19)).RunAsync();
        Assert.Equal(2, pub.Posts.Count);
    }

    [Fact]
    public async Task Results_NewDay_ResetsDailyState()
    {
        var yesterday = new DateOnly(2026, 11, 21);
        var store = new InMemoryStateStore(PostedState.Empty.ForDay(yesterday).WithHerren(1).WithJugendPosted());
        var pub = new RecordingPublisher();
        await Job(Games(), pub, store, At(18)).RunAsync();
        Assert.Equal(2, pub.Posts.Count);
        Assert.Equal(new DateOnly(2026, 11, 22), store.Current.Date);
    }

    [Fact]
    public async Task Results_SaveFailsTwiceThenSucceeds_SavedWithoutError()
    {
        var store = new InMemoryStateStore(failSaves: 2);
        var log = new List<string>();
        var pub = new RecordingPublisher();
        await Job(Games(), pub, store, At(18), log).RunAsync();
        Assert.Equal(4, store.SaveAttempts); // Herren: 2 Fehlschläge + 1, Jugend: 1
        Assert.Contains(1, store.Current.HerrenMatchIds);
        Assert.True(store.Current.JugendPosted);
        Assert.DoesNotContain(log, l => l.Contains("NICHT gespeichert"));
    }

    [Fact]
    public async Task Results_SaveAlwaysFails_DistinctMessagePerPost_InAggregate()
    {
        var store = new InMemoryStateStore(failSaves: int.MaxValue);
        var log = new List<string>();
        var pub = new RecordingPublisher();
        var ex = await Assert.ThrowsAsync<AggregateException>(() => Job(Games(), pub, store, At(18), log).RunAsync());
        Assert.Equal(2, pub.Posts.Count); // Speicherfehler bei Herren hält Jugend nicht auf
        var keys = new[] { "herren-result-1", "jugend-result-2026-11-22" };
        Assert.Equal(keys.Length, ex.InnerExceptions.Count);
        foreach (var (inner, key) in ex.InnerExceptions.Zip(keys))
        {
            var expected = $"VERÖFFENTLICHT, Zustand NICHT gespeichert – {key} – nicht erneut auslösen";
            Assert.StartsWith(expected, Assert.IsType<StateNotSavedException>(inner).Message);
            Assert.Contains(log, l => l.StartsWith(expected));
        }
        Assert.DoesNotContain(log, l => l.StartsWith("Fehler bei"));
        Assert.Equal(6, store.SaveAttempts);
    }

    [Fact]
    public async Task Results_Nothing_NoSave()
    {
        var store = new InMemoryStateStore();
        await Job(new FakeGameSource(), new RecordingPublisher(), store, At(18)).RunAsync();
        Assert.Equal(0, store.Saves);
    }
}
