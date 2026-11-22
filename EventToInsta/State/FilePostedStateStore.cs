namespace EventToInsta.State;

public sealed class FilePostedStateStore(string path) : IPostedStateStore
{
    public async Task<PostedState> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(path)) return PostedState.Empty;
        return PostedStateJson.Deserialize(await File.ReadAllTextAsync(path, ct));
    }

    public Task SaveAsync(PostedState state, CancellationToken ct = default) =>
        File.WriteAllTextAsync(path, PostedStateJson.Serialize(state), ct);
}
