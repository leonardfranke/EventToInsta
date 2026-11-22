using EventToInsta.Publishing;
using EventToInsta.Rendering;

namespace EventToInsta.Tests.Publishing;

public class LocalFolderPublisherTests
{
    [Fact]
    public async Task LocalFolder_WritesAllFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "eti-" + Guid.NewGuid());
        try
        {
            var publisher = new LocalFolderPublisher(dir);
            var post = new StoryPost("k", [new RenderedStory("a.jpg", [1, 2]), new RenderedStory("b.jpg", [3])]);

            await publisher.PublishAsync(post);
            // Vorhandene Dateien werden überschrieben
            await publisher.PublishAsync(new StoryPost("k", [new RenderedStory("a.jpg", [9])]));

            Assert.Equal(new byte[] { 9 }, await File.ReadAllBytesAsync(Path.Combine(dir, "a.jpg")));
            Assert.Equal(new byte[] { 3 }, await File.ReadAllBytesAsync(Path.Combine(dir, "b.jpg")));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
