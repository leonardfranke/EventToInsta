namespace EventToInsta.Publishing;

public interface IStoryPublisher
{
    Task PublishAsync(StoryPost post, CancellationToken ct = default);
}
