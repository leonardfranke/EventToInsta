namespace EventToInsta.State;

public sealed class FilePostedStateStore(string path) : IPostedStateStore
{
    public async Task<PostedState> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(path)) return PostedState.Empty;
        return PostedStateJson.Deserialize(await File.ReadAllTextAsync(path, ct), path);
    }

    public async Task SaveAsync(PostedState state, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, PostedStateJson.Serialize(state), ct);
        File.Move(tmp, path, overwrite: true);
    }
}
