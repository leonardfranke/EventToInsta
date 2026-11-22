namespace EventToInsta.State;

public interface IPostedStateStore
{
    Task<PostedState> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(PostedState state, CancellationToken ct = default);
}
