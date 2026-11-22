using System.Globalization;
using System.Text.RegularExpressions;
using EventToInsta.Data;

namespace EventToInsta.Domain
{
    public static class GameMapper
    {
        private static readonly Regex AkSuffix = new(@"\(ak\)", RegexOptions.IgnoreCase);

        public static IReadOnlyList<Game> Map(IEnumerable<MatchDto> matches, int clubId, Action<string>? warn = null)
        {
            var games = new List<Game>();
            foreach (var m in matches)
            {
                if (m.Abgesagt || m.Verzicht || m.HomeTeam == null || m.GuestTeam == null
                    || m.HomeTeam.Verzicht || m.GuestTeam.Verzicht)
                    continue;

                if (!DateTime.TryParseExact($"{m.KickoffDate} {m.KickoffTime}", "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var kickoff))
                {
                    warn?.Invoke($"Spiel {m.MatchId}: Anpfiff nicht lesbar ({m.KickoffDate} {m.KickoffTime})");
                    continue;
                }
                if (kickoff.Hour < 7)
                    continue;

                if (m.HomeTeam.ClubId == clubId && m.GuestTeam.ClubId == clubId)
                {
                    warn?.Invoke($"Spiel {m.MatchId}: vereinsinternes Spiel wird übersprungen");
                    continue;
                }
                if (m.HomeTeam.ClubId != clubId && m.GuestTeam.ClubId != clubId)
                    continue;

                games.Add(new Game(
                    m.MatchId,
                    m.LigaData?.AkName ?? "",
                    m.LigaData?.SkName ?? "",
                    kickoff,
                    MapTeam(m.HomeTeam),
                    MapTeam(m.GuestTeam),
                    null,
                    Score.Parse(m.Result),
                    clubId));
            }
            return games;
        }

        private static Team MapTeam(TeamDto t) =>
            new(t.TeamPermanentId, Clean(t.TeamName), Clean(t.TeamNameSmall), t.ClubId);

        private static string Clean(string? name) => AkSuffix.Replace(name ?? "", "").Trim();
    }
}
