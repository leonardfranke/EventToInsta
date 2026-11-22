using EventToInsta.Domain;
using EventToInsta.Time;

namespace EventToInsta.Data;

/// <summary>Welche Beispielspiele geliefert werden: die Woche für die Vorschau oder der heutige Spieltag mit Ergebnissen.</summary>
public enum SampleSet
{
    Week,
    Today,
}

/// <summary>
/// Beispieldaten für <c>--sample</c>, relativ zu <see cref="IClock.Now"/>. Beide Sätze liegen zwangsläufig
/// im Vorschaufenster (heute bis Sonntag), deshalb liefert die Quelle je Job nur ihren Satz.
/// </summary>
public sealed class SampleGameSource(IClock clock, SampleSet set) : IGameSource
{
    private const int ClubId = 621;
    public const string LongOpponentName = "Spielgemeinschaft Osnabrücker Land Nordwest Basketball e. V.";

    private static readonly Team Us = new(1000, "SV Bad Laer", "Bad Laer", ClubId);

    public Task<IReadOnlyList<Game>> LoadGamesAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.Now);
        return Task.FromResult(set == SampleSet.Week ? Week(today) : Today(today));
    }

    private static IReadOnlyList<Game> Week(DateOnly today)
    {
        var sunday = today.AddDays(((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7);
        // Tage vor Sonntag, nie vor heute (am Sonntag liegt alles auf heute).
        DateTime At(int daysBeforeSunday, int hour, int minute = 0)
        {
            var day = sunday.AddDays(-daysBeforeSunday);
            if (day < today) day = today;
            return day.ToDateTime(new TimeOnly(hour, minute));
        }

        return
        [
            Home(9001, "Senioren", "Oberliga Herren", At(1, 19, 30), Opp(5001, "TV Georgsmarienhütte", "GMHütte"), "Bad Laer"),
            Away(9002, "Senioren", "Oberliga Herren", At(0, 17), Opp(0, "BG Osnabrück", "Osnabrück"), "Osnabrück"),

            Home(9011, "U12", "Bezirksliga U12", At(1, 10), Opp(5011, "TSG Dissen", "Dissen"), "Bad Laer"),
            Home(9012, "U14", "Bezirksoberliga U14", At(1, 12), Opp(5012, LongOpponentName, "SG OS-Land"), "Bad Laer"),
            Home(9013, "U16", "Bezirksliga U16", At(1, 14), Opp(5013, "SC Melle", "Melle"), "Bad Laer"),
            Home(9014, "U18", "Oberliga U18", At(0, 11), Opp(5014, "Osnabrücker SC", "OSC"), "Glandorf"),
            Home(9015, "U10", "Kreisliga U10", At(0, 13), Opp(5015, "TuS Bersenbrück", "Bersenbrück"), "Bad Laer"),
            Home(9016, "U14", "Bezirksliga U14", At(0, 15), Opp(5016, "BBC Bramsche", "Bramsche"), null),
            Home(9017, "U16", "Bezirksoberliga U16", At(4, 18), Opp(5017, "TV Bad Rothenfelde", "Rothenfelde"), "Bad Laer"),

            Away(9021, "U12", "Bezirksliga U12", At(1, 15), Opp(5021, "Sparta Werlte", "Werlte"), "Werlte"),
            Away(9022, "U18", "Oberliga U18", At(0, 12), Opp(5022, "Artland Dragons", "Dragons"), "Quakenbrück"),
        ];
    }

    private static IReadOnlyList<Game> Today(DateOnly today)
    {
        DateTime At(int hour) => today.ToDateTime(new TimeOnly(hour, 0));

        return
        [
            Home(9101, "Senioren", "Oberliga Herren", At(17), Opp(5001, "TV Georgsmarienhütte", "GMHütte"), "Bad Laer", new Score(78, 64)),

            Home(9111, "U12", "Bezirksliga U12", At(10), Opp(5011, "TSG Dissen", "Dissen"), "Bad Laer", new Score(54, 40)),
            Away(9112, "U14", "Bezirksoberliga U14", At(11), Opp(5013, "SC Melle", "Melle"), "Melle", new Score(61, 38)),
            Home(9113, "U16", "Bezirksliga U16", At(13), Opp(5014, "Osnabrücker SC", "OSC"), "Bad Laer", new Score(70, 72)),
            Away(9114, "U18", "Oberliga U18", At(14), Opp(5012, LongOpponentName, "SG OS-Land"), "Hasbergen", new Score(59, 66)),
            Home(9115, "U10", "Kreisliga U10", At(15), Opp(5015, "TuS Bersenbrück", "Bersenbrück"), "Bad Laer"),
        ];
    }

    private static Team Opp(int permanentId, string name, string shortName) => new(permanentId, name, shortName, 9999);

    private static Game Home(int id, string age, string league, DateTime kickoff, Team opponent, string? place, Score? result = null) =>
        new(id, age, league, kickoff, Us, opponent, place, result, ClubId);

    private static Game Away(int id, string age, string league, DateTime kickoff, Team opponent, string? place, Score? result = null) =>
        new(id, age, league, kickoff, opponent, Us, place, result, ClubId);
}

/// <summary>Logo-Fake für <c>--sample</c>: Vereinslogo für jede Team-ID außer 0 (0 = Gegner ohne Logo, zeigt das Badge).</summary>
public sealed class SampleLogoSource(byte[] logo) : ILogoSource
{
    public Task<byte[]?> LoadTeamLogoAsync(int teamPermanentId, CancellationToken ct = default) =>
        Task.FromResult(teamPermanentId == 0 ? null : logo);
}
