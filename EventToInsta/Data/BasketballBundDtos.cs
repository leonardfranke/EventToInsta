using System.Text.Json.Serialization;

namespace EventToInsta.Data
{
    public class MatchesResponse
    {
        [JsonPropertyName("data")]
        public MatchesData? Data { get; set; }
    }

    public class MatchesData
    {
        [JsonPropertyName("matches")]
        public List<MatchDto> Matches { get; set; } = new();
    }

    public class MatchDto
    {
        [JsonPropertyName("matchId")]
        public int MatchId { get; set; }

        [JsonPropertyName("ligaData")]
        public LigaDataDto? LigaData { get; set; }

        [JsonPropertyName("homeTeam")]
        public TeamDto? HomeTeam { get; set; }

        [JsonPropertyName("guestTeam")]
        public TeamDto? GuestTeam { get; set; }

        [JsonPropertyName("kickoffDate")]
        public string? KickoffDate { get; set; }

        [JsonPropertyName("kickoffTime")]
        public string? KickoffTime { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("abgesagt")]
        public bool Abgesagt { get; set; }

        [JsonPropertyName("verzicht")]
        public bool Verzicht { get; set; }
    }

    public class TeamDto
    {
        [JsonPropertyName("teamPermanentId")]
        public int TeamPermanentId { get; set; }

        [JsonPropertyName("teamname")]
        public string? TeamName { get; set; }

        [JsonPropertyName("teamnameSmall")]
        public string? TeamNameSmall { get; set; }

        [JsonPropertyName("clubId")]
        public int ClubId { get; set; }

        [JsonPropertyName("verzicht")]
        public bool Verzicht { get; set; }
    }

    public class LigaDataDto
    {
        [JsonPropertyName("akName")]
        public string? AkName { get; set; }

        [JsonPropertyName("skName")]
        public string? SkName { get; set; }
    }

    public class MatchInfoResponse
    {
        [JsonPropertyName("data")]
        public MatchInfoData? Data { get; set; }
    }

    public class MatchInfoData
    {
        [JsonPropertyName("matchInfo")]
        public MatchInfoDto? MatchInfo { get; set; }
    }

    public class MatchInfoDto
    {
        [JsonPropertyName("spielfeld")]
        public SpielfeldDto? Spielfeld { get; set; }
    }

    public class SpielfeldDto
    {
        [JsonPropertyName("ort")]
        public string? Ort { get; set; }
    }
}
