using Flurl.Http;
using System.Text.RegularExpressions;

namespace EventToInsta
{
    public static class MatchesLoader
    {
        private static FlurlClient _client;

        static MatchesLoader()
        {
            _client = new FlurlClient("https://www.basketball-bund.net/");
        }

        public static async Task<IEnumerable<MatchData>> LoadMatches()
        {
            var matchesRes = await _client.Request("rest/club/id/621/actualmatches?justHome=true&rangeDays=8").GetJsonAsync<MatchesResponse>();
            var matches = new List<MatchData>();
            foreach(var matchData in matchesRes.Data.Matches.Where(match => match.Verzicht == false))
            {
                var match = new MatchData
                {
                    Age = matchData.LigaData.AkName,
                    League = matchData.LigaData.SkName,
                    OpponentName = matchData.GuestTeam.TeamName,
                    OpponentNameSmall = matchData.GuestTeam.TeamNameSmall,
                    OpponentTeamId = matchData.GuestTeam.TeamPermanentId,
                    TipoffTime = DateTime.Parse($"{matchData.KickoffDate} {matchData.KickoffTime}")
                };

                if (match.TipoffTime.Hour < 7 || DateTime.UtcNow > match.TipoffTime)
                    continue;

                var matchRes = await _client.Request($"rest/match/id/{matchData.MatchId}/matchInfo").GetJsonAsync<MatchResponse>();

                match.Place = matchRes.Data.MatchInfo.Spielfeld?.Ort;
                matches.Add(match);
            }
            return matches;
        }

        public static async Task<byte[]> LoadTeamImage(int teamPermanentId)
        {
            var imageRes = await _client.Request($"media/team/{teamPermanentId}/logo").GetBytesAsync();
            if (imageRes.Length < 2000)
                return null;
            return imageRes;
        }
    }

    public class MatchResponse
    {
        public Match Data { get; set; }
    }

    public class MatchesResponse
    {
        public MatchesData Data { get; set; }
    }

    public class MatchesData
    {
        public List<Match> Matches { get; set; }
    }

    public class Match
    {
        public int MatchId { get; set; }
        public LigaData LigaData { get; set; }
        public Team GuestTeam { get; set; }
        public string KickoffDate { get; set; }
        public string KickoffTime { get; set; }
        public MatchInfo MatchInfo { get; set; }
        public bool Verzicht { get; set; }
    }

    public class MatchInfo
    {
        public Spielfeld Spielfeld { get; set; }
    }

    public class Spielfeld
    {
        public string Ort { get; set; }
    }

    public class Team
    {
        public string TeamName { get; set; }
        public string TeamNameSmall { get; set; }
        public int TeamPermanentId { get; set; }
    }

    public class LigaData
    {
        public string AkName { get; set; }
        public string SkName { get; set; }
    }

    public class MatchData
    {
        public string Age { get; set; }
        public string League { get; set; }
        public string OpponentName { get; set; }
        public string OpponentNameSmall { get; set; }
        public DateTime TipoffTime { get; set; }
        public string Place { get; set; }
        public int OpponentTeamId { get; set; }
    }
}
