using EventToInsta.Domain;
using EventToInsta.Planning;

namespace EventToInsta.Tests.Planning;

/// <summary>
/// Die Dateinamen sind Teil des Idempotency-Keys. Sie müssen deshalb von den Spielen des Teils abhängen,
/// nicht nur von der Position: Ändert sich die Liste zwischen Fehlschlag und Wiederholung, darf ein
/// anderes Bild nie denselben Namen tragen.
/// </summary>
public class StoryRequestsTests
{
    private static readonly DateOnly Day = new(2026, 11, 22);
    private static readonly DateOnly WeekEnd = new(2026, 11, 29);
    private static DateTime At(int hour) => new(2026, 11, 22, hour, 0, 0);

    private static Game[] Games(params int[] ids) => ids.Select(i => GameFactory.Make(i, At(10))).ToArray();

    [Fact]
    public void JugendResult_FileName_HasPartAndShortAsciiHash()
    {
        var name = new JugendResult(Day, Games(1, 2), 1, 2).FileName;
        Assert.Matches(@"^jugend-result-2026-11-22-1-[0-9a-f]{8}\.jpg$", name);
    }

    [Fact]
    public void JugendPreview_FileName_HasPartAndShortAsciiHash()
    {
        Assert.Matches(@"^jugend-preview-2-[0-9a-f]{8}\.jpg$",
            new JugendPreview(Day, WeekEnd, Games(1, 2), 2, 2).FileName);
        Assert.Matches(@"^jugend-preview-1-[0-9a-f]{8}\.jpg$",
            new JugendPreview(Day, WeekEnd, Games(3), 1, 1).FileName);
    }

    [Fact]
    public void JugendResult_SameGames_SameFileName()
    {
        Assert.Equal(new JugendResult(Day, Games(1, 2), 1, 2).FileName, new JugendResult(Day, Games(1, 2), 1, 2).FileName);
    }

    [Fact]
    public void JugendResult_DifferentGamesSamePosition_DifferentFileName()
    {
        var before = new JugendResult(Day, Games(1, 2), 1, 2).FileName;
        Assert.NotEqual(before, new JugendResult(Day, Games(1, 3), 1, 2).FileName);
        Assert.NotEqual(before, new JugendResult(Day, Games(1, 2, 3), 1, 2).FileName);
        // gleiche Spiele, aber andere Aufteilung („1/2“ statt „1/3“ auf dem Bild)
        Assert.NotEqual(before, new JugendResult(Day, Games(1, 2), 1, 3).FileName);
    }

    [Fact]
    public void JugendPreview_DifferentGamesSamePosition_DifferentFileName()
    {
        Assert.NotEqual(
            new JugendPreview(Day, WeekEnd, Games(1, 2), 1, 1).FileName,
            new JugendPreview(Day, WeekEnd, Games(1, 4), 1, 1).FileName);
    }
}
