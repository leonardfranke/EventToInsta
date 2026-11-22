using EventToInsta.Rendering;

namespace EventToInsta.Publishing;

public record StoryPost(string Key, IReadOnlyList<RenderedStory> Images);
