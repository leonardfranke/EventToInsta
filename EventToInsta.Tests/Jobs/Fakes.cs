using EventToInsta.Data;
using EventToInsta.Domain;
using EventToInsta.Planning;
using EventToInsta.Publishing;
using EventToInsta.Rendering;
using EventToInsta.State;

namespace EventToInsta.Tests.Jobs;

internal sealed class FakeGameSource(params Game[] games) : IGameSource
{
    public Task<IReadOnlyList<Game>> LoadGamesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Game>>(games);
}

internal sealed class InMemoryStateStore(PostedState? initial = null) : IPostedStateStore
{
    public PostedState Current { get; private set; } = initial ?? PostedState.Empty;
    public int Saves { get; private set; }
    public Task<PostedState> LoadAsync(CancellationToken ct = default) => Task.FromResult(Current);
    public Task SaveAsync(PostedState state, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); // wie die echten Stores
        Current = state;
        Saves++;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingPublisher(
    Func<string, bool>? failFor = null,
    Action<string>? onPublishStart = null,
    Action<string>? afterRecorded = null) : IStoryPublisher
{
    public List<StoryPost> Posts { get; } = new();
    public int Calls { get; private set; }
    public Task PublishAsync(StoryPost post, CancellationToken ct = default)
    {
        Calls++;
        onPublishStart?.Invoke(post.Key);
        if (failFor?.Invoke(post.Key) == true) throw new UploadFailedException("boom " + post.Key);
        Posts.Add(post);
        afterRecorded?.Invoke(post.Key);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRenderer(Func<int, bool>? failAtCall = null) : IStoryRenderer
{
    private int _calls;
    public Task<RenderedStory> RenderAsync(StoryRequest r, CancellationToken ct = default)
    {
        _calls++;
        if (failAtCall?.Invoke(_calls) == true) throw new InvalidOperationException("render boom");
        return Task.FromResult(new RenderedStory(r.FileName, new byte[] { 1 }));
    }
}
