using EventToInsta.Planning;

namespace EventToInsta.Rendering;

public interface IStoryRenderer
{
    Task<RenderedStory> RenderAsync(StoryRequest r, CancellationToken ct = default);
}
