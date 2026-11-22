using System.Globalization;

namespace EventToInsta.State;

public record PostedState(
    string? PreviewWeek,
    DateOnly? Date,
    IReadOnlyList<int> HerrenMatchIds,
    bool JugendPosted)
{
    public static PostedState Empty { get; } = new(null, null, Array.Empty<int>(), false);

    public PostedState ForDay(DateOnly today) =>
        Date == today ? this : this with { Date = today, HerrenMatchIds = Array.Empty<int>(), JugendPosted = false };

    public PostedState WithHerren(int matchId) =>
        HerrenMatchIds.Contains(matchId) ? this : this with { HerrenMatchIds = [.. HerrenMatchIds, matchId] };

    public PostedState WithJugendPosted() => this with { JugendPosted = true };

    public PostedState WithPreviewWeek(string week) => this with { PreviewWeek = week };

    public static string IsoWeek(DateOnly day)
    {
        var dt = day.ToDateTime(TimeOnly.MinValue);
        return $"{ISOWeek.GetYear(dt)}-W{ISOWeek.GetWeekOfYear(dt):00}";
    }
}
