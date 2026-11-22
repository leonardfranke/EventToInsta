namespace EventToInsta.Domain
{
    public record Team(int PermanentId, string Name, string ShortName, int ClubId);

    public record Score(int Home, int Guest)
    {
        public static Score? Parse(string? result)
        {
            if (string.IsNullOrWhiteSpace(result))
                return null;
            var parts = result.Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0].Trim(), out var home)
                || !int.TryParse(parts[1].Trim(), out var guest))
                return null;
            return new Score(home, guest);
        }
    }

    /// <param name="Kickoff">Berliner Ortszeit, DateTimeKind.Unspecified.</param>
    public record Game(
        int MatchId,
        string AgeClass,
        string League,
        DateTime Kickoff,
        Team HomeTeam,
        Team GuestTeam,
        string? Place,
        Score? Result,
        int ClubId)
    {
        public bool IsHome => HomeTeam.ClubId == ClubId;
        public bool IsHerren => AgeClass == "Senioren";
        public Team Us => IsHome ? HomeTeam : GuestTeam;
        public Team Opponent => IsHome ? GuestTeam : HomeTeam;
        public bool HasResult => Result != null;
        public int? OwnPoints => Result == null ? null : IsHome ? Result.Home : Result.Guest;
        public int? OpponentPoints => Result == null ? null : IsHome ? Result.Guest : Result.Home;
        public bool? IsWin => HasResult ? OwnPoints > OpponentPoints : null;

        public Game WithPlace(string? place) => this with { Place = place };
    }
}
