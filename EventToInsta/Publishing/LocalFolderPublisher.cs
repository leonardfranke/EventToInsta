namespace EventToInsta.Publishing;

public sealed class LocalFolderPublisher(string directory) : IStoryPublisher
{
    public async Task PublishAsync(StoryPost post, CancellationToken ct = default)
    {
        Directory.CreateDirectory(directory);
        foreach (var image in post.Images)
            await File.WriteAllBytesAsync(Path.Combine(directory, image.FileName), image.Jpeg, ct);
    }
}
