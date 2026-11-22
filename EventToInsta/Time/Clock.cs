namespace EventToInsta.Time;

public interface IClock
{
    /// <summary>Berliner Ortszeit, Kind Unspecified.</summary>
    DateTime Now { get; }
}

public sealed class SystemClock(Func<DateTimeOffset>? utcNow = null) : IClock
{
    private readonly TimeZoneInfo _berlin = LoadBerlin();
    private readonly Func<DateTimeOffset> _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);

    /// <summary>Lädt Europe/Berlin; fehlen die Zeitzonendaten (z. B. tzdata/ICU unter Linux), mit klarer Meldung.</summary>
    internal static TimeZoneInfo LoadBerlin(Func<string, TimeZoneInfo>? find = null)
    {
        const string id = "Europe/Berlin";
        try
        {
            return (find ?? TimeZoneInfo.FindSystemTimeZoneById)(id);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"Zeitzone {id} nicht verfügbar. Auf dem System fehlen Zeitzonendaten (tzdata/ICU): {e.Message}", e);
        }
    }

    public DateTime Now
    {
        get
        {
            var local = TimeZoneInfo.ConvertTime(_utcNow(), _berlin).DateTime;
            return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        }
    }
}

public sealed class FixedClock(DateTime now) : IClock
{
    public DateTime Now { get; } = now;
}
