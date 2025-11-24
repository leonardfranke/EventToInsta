using Flurl.Http;

namespace EventToInsta
{
    public static class MatchesLoader
    {
        private static FlurlClient _client;

        static MatchesLoader()
        {
            _client = new FlurlClient("https://www.basketball-bund.net/rest/club/id/621/actualmatches?justHome=true&rangeDays=8");
        }

        public static async Task<IEnumerable<MatchInfo>> LoadMatches()
        {
            var dbbRes = await _client.Request().GetJsonAsync<MatchesResponse>();
            var matchInfo = dbbRes.Data.Matches
                .Where(match => match.Verzicht == false)
                .Select(match =>
                    new MatchInfo
                    {
                        Age = match.LigaData.AkName,
                        OpponentName = match.GuestTeam.TeamName,
                        OpponentNameSmall = match.GuestTeam.TeamNameSmall,
                        TipoffTime = DateTime.Parse($"{match.KickoffDate} {match.KickoffTime}")
                    })
                .Where(match => match.TipoffTime.Hour >= 7)
                .Where(match => DateTime.UtcNow <= match.TipoffTime);
            return matchInfo;
        }
    }

    class MatchesResponse
    {
        public Data Data { get; set; }
    }

    class Data
    {
        public List<Match> Matches { get; set; }
    }

    class Match
    {
        public LigaData LigaData { get; set; }
        public Team GuestTeam { get; set; }
        public string KickoffDate { get; set; }
        public string KickoffTime { get; set; }
        public bool Verzicht { get; set; }
    }

    class Team
    {
        public string TeamName { get; set; }
        public string TeamNameSmall { get; set; }
    }

    class LigaData
    {
        public string AkName { get; set; }
    }

    public class MatchInfo
    {
        public string Age { get; set; }
        public string OpponentName { get; set; }
        public string OpponentNameSmall { get; set; }
        public DateTime TipoffTime { get; set; }
    }
}
