namespace EventToInsta.State;

public sealed class NullPostedStateStore : IPostedStateStore
{
    public Task<PostedState> LoadAsync(CancellationToken ct = default) => Task.FromResult(PostedState.Empty);
    public Task SaveAsync(PostedState state, CancellationToken ct = default) => Task.CompletedTask;
}
