using EventToInsta.Data;

namespace EventToInsta.Tests.Domain
{
    using EventToInsta.Domain;

    public class GameMapperTests
    {
        private const int Us = 621;
        private const int Other = 823;

        private static TeamDto T(int clubId, string name = "Team", bool verzicht = false) =>
            new() { TeamPermanentId = clubId * 10, TeamName = name, TeamNameSmall = name, ClubId = clubId, Verzicht = verzicht };

        private static MatchDto M(TeamDto home, TeamDto guest, string? result = null, string time = "19:30",
            string date = "2026-11-28", string ak = "U16", bool abgesagt = false, bool verzicht = false) =>
            new()
            {
                MatchId = 1,
                HomeTeam = home,
                GuestTeam = guest,
                Result = result,
                KickoffDate = date,
                KickoffTime = time,
                Abgesagt = abgesagt,
                Verzicht = verzicht,
                LigaData = new LigaDataDto { AkName = ak, SkName = "Liga" }
            };

        private static Game One(MatchDto m) => Assert.Single(GameMapper.Map(new[] { m }, Us));

        [Fact]
        public void Map_AwayGame_ComputesPointsFromBadLaerPerspective()
        {
            var g = One(M(T(Other), T(Us), "113:16"));
            Assert.False(g.IsHome);
            Assert.Equal(16, g.OwnPoints);
            Assert.Equal(113, g.OpponentPoints);
            Assert.False(g.IsWin);
        }

        [Fact]
        public void Map_HomeWin_IsWinTrue()
        {
            var g = One(M(T(Us), T(Other), "78:64"));
            Assert.True(g.IsWin);
            Assert.Equal(Other, g.Opponent.ClubId);
        }

        [Fact]
        public void Map_NoResult_HasResultFalse()
        {
            var g = One(M(T(Us), T(Other), null));
            Assert.False(g.HasResult);
            Assert.Null(g.IsWin);
        }

        [Fact]
        public void Map_FiltersCancelledVerzichtEarlyAndDerby()
        {
            var warnings = new List<string>();
            var games = GameMapper.Map(new[]
            {
                M(T(Us), T(Other), abgesagt: true),
                M(T(Us), T(Other), verzicht: true),
                M(T(Us), T(Other), time: "06:59"),
                M(T(Us), T(Us)),
            }, Us, warnings.Add);
            Assert.Empty(games);
            Assert.Single(warnings);
        }

        [Fact]
        public void Map_FiltersTeamVerzicht()
        {
            Assert.Empty(GameMapper.Map(new[] { M(T(Us), T(Other, verzicht: true)) }, Us));
            Assert.Empty(GameMapper.Map(new[] { M(T(Us, verzicht: true), T(Other)) }, Us));
        }

        [Fact]
        public void Map_CleansAkSuffix()
        {
            var g = One(M(T(Us), T(Other, "SC Melle I (aK)")));
            Assert.Equal("SC Melle I", g.Opponent.Name);
        }

        [Fact]
        public void Map_Senioren_IsHerren()
        {
            Assert.True(One(M(T(Us), T(Other), ak: "Senioren")).IsHerren);
            Assert.False(One(M(T(Us), T(Other), ak: "U16")).IsHerren);
        }

        [Fact]
        public void Map_ParsesKickoffAsLocal()
        {
            var g = One(M(T(Us), T(Other), date: "2026-11-28", time: "19:30"));
            Assert.Equal(new DateTime(2026, 11, 28, 19, 30, 0), g.Kickoff);
            Assert.Equal(DateTimeKind.Unspecified, g.Kickoff.Kind);
        }

        [Fact]
        public void ScoreParse_Valid()
        {
            Assert.Equal(new Score(57, 64), Score.Parse("57:64"));
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData("12:")]
        [InlineData(null)]
        public void ScoreParse_Invalid_ReturnsNull(string? input)
        {
            Assert.Null(Score.Parse(input));
        }
    }
}
