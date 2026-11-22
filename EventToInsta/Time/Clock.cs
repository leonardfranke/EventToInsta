namespace EventToInsta.Time;

public interface IClock
{
    /// <summary>Berliner Ortszeit, Kind Unspecified.</summary>
    DateTime Now { get; }
}

public sealed class SystemClock(Func<DateTimeOffset>? utcNow = null) : IClock
{
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
    private readonly Func<DateTimeOffset> _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);

    public DateTime Now
    {
        get
        {
            var local = TimeZoneInfo.ConvertTime(_utcNow(), Berlin).DateTime;
            return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        }
    }
}

public sealed class FixedClock(DateTime now) : IClock
{
    public DateTime Now { get; } = now;
}
