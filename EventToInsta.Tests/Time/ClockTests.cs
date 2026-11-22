using EventToInsta.Time;

namespace EventToInsta.Tests.Time;

public class ClockTests
{
    [Fact]
    public void SystemClock_UtcLateSaturday_IsSundayInBerlin()
    {
        var clock = new SystemClock(() => DateTimeOffset.Parse("2026-11-28T23:30:00Z"));
        Assert.Equal(new DateTime(2026, 11, 29, 0, 30, 0), clock.Now);
        Assert.Equal(DateTimeKind.Unspecified, clock.Now.Kind);
    }

    [Fact]
    public void SystemClock_Summer_UsesCest()
    {
        var clock = new SystemClock(() => DateTimeOffset.Parse("2026-07-04T10:00:00Z"));
        Assert.Equal(12, clock.Now.Hour);
    }

    [Fact]
    public void LoadBerlin_MissingTimeZoneData_ThrowsClearMessage()
    {
        var e = Assert.Throws<InvalidOperationException>(() =>
            SystemClock.LoadBerlin(_ => throw new TimeZoneNotFoundException("not found")));

        Assert.Contains("Europe/Berlin", e.Message);
        Assert.Contains("Zeitzone", e.Message);
        Assert.IsType<TimeZoneNotFoundException>(e.InnerException);
    }
}
