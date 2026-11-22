using EventToInsta.Domain;

namespace EventToInsta.Planning;

public abstract record StoryRequest(string FileName);

public record HerrenPreview(Game Game)
    : StoryRequest($"herren-preview-{Game.MatchId}.jpg");

public record JugendPreview(
    bool Home,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    IReadOnlyList<Game> Games,
    int Part,
    int PartCount)
    : StoryRequest($"jugend-preview-{(Home ? "heim" : "auswaerts")}-{Part}.jpg");

public record HerrenResult(Game Game)
    : StoryRequest($"herren-result-{Game.MatchId}.jpg");

public record JugendResult(
    DateOnly Day,
    IReadOnlyList<Game> Games,
    int Part,
    int PartCount)
    : StoryRequest($"jugend-result-{Day:yyyy-MM-dd}-{Part}.jpg");
