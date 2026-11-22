using System.Globalization;
using EventToInsta.State;

namespace EventToInsta.Tests.State;

public class PostedStateTests
{
    [Fact]
    public void ForDay_OtherDate_ResetsDailyButKeepsPreviewWeek()
    {
        var state = new PostedState("2026-W47", new DateOnly(2026, 11, 21), new[] { 1, 2 }, true);
        var result = state.ForDay(new DateOnly(2026, 11, 22));
        Assert.Equal("2026-W47", result.PreviewWeek);
        Assert.Equal(new DateOnly(2026, 11, 22), result.Date);
        Assert.Empty(result.HerrenMatchIds);
        Assert.False(result.JugendPosted);
    }

    [Fact]
    public void ForDay_SameDate_KeepsEverything()
    {
        var state = new PostedState("2026-W47", new DateOnly(2026, 11, 22), new[] { 1, 2 }, true);
        Assert.Equal(state, state.ForDay(new DateOnly(2026, 11, 22)));
    }

    [Fact]
    public void IsoWeek_2026_11_23_IsW48() =>
        Assert.Equal("2026-W48", PostedState.IsoWeek(new DateOnly(2026, 11, 23)));

    [Fact]
    public void IsoWeek_2027_01_01_IsW53OfPrevYear()
    {
        var day = new DateOnly(2027, 1, 1);
        var dt = day.ToDateTime(TimeOnly.MinValue);
        Assert.Equal(2026, ISOWeek.GetYear(dt));
        Assert.Equal(53, ISOWeek.GetWeekOfYear(dt));
        Assert.Equal("2026-W53", PostedState.IsoWeek(day));
    }
}
