using System.Net;
using EventToInsta.Data;

namespace EventToInsta.Tests.Data
{
    public class BasketballBundClientTests
    {
        private sealed class FakeHandler(Func<string, HttpResponseMessage> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
                Task.FromResult(respond(request.RequestUri!.PathAndQuery));
        }

        private static BasketballBundClient CreateClient(Func<string, HttpResponseMessage> respond, Action<string>? log = null) =>
            new(new HttpClient(new FakeHandler(respond)) { BaseAddress = new Uri("https://example.test/") }, 621, log);

        private const string MatchesPath = "/rest/club/id/621/actualmatches?justHome=false&rangeDays=7";

        /// <summary>Fixture mit 1001 und 1003; matchInfo von 1001 schlägt mit <paramref name="failure"/> fehl.</summary>
        private static async Task<(IReadOnlyList<EventToInsta.Domain.Game> Games, List<string> Log)> LoadWithFailingMatchInfo(
            Func<HttpResponseMessage> failure, CancellationToken ct = default)
        {
            var fixture = await File.ReadAllTextAsync(Path.Combine("Data", "actualmatches.json"));
            var log = new List<string>();
            var client = CreateClient(path =>
            {
                if (path == MatchesPath) return Json(fixture);
                if (path == "/rest/match/id/1001/matchInfo") return failure();
                if (path == "/rest/match/id/1003/matchInfo")
                    return Json("""{"data":{"matchInfo":{"spielfeld":{"ort":"Glandorf"}}}}""");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }, log.Add);
            return (await client.LoadGamesAsync(ct), log);
        }

        [Fact]
        public async Task LoadGames_MatchInfoInvalidJson_PlaceNullWarningLogged_OthersLoaded()
        {
            var (games, log) = await LoadWithFailingMatchInfo(() => Json("<html>wartung</html>"));

            Assert.Null(games.Single(g => g.MatchId == 1001).Place);
            Assert.Equal("Glandorf", games.Single(g => g.MatchId == 1003).Place);
            Assert.Contains(log, l => l.Contains("1001"));
        }

        [Fact]
        public async Task LoadGames_MatchInfoTimeout_PlaceNullWarningLogged_OthersLoaded()
        {
            // So meldet HttpClient einen Timeout: TaskCanceledException, ohne dass der Aufrufer abgebrochen hat.
            var (games, log) = await LoadWithFailingMatchInfo(() => throw new TaskCanceledException("timeout"));

            Assert.Null(games.Single(g => g.MatchId == 1001).Place);
            Assert.Equal("Glandorf", games.Single(g => g.MatchId == 1003).Place);
            Assert.Contains(log, l => l.Contains("1001"));
        }

        [Fact]
        public async Task LoadGames_CallerCancels_Propagates()
        {
            using var cts = new CancellationTokenSource();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => LoadWithFailingMatchInfo(() =>
            {
                cts.Cancel();
                throw new TaskCanceledException("abgebrochen", null, cts.Token);
            }, cts.Token));
        }

        [Fact]
        public async Task LoadTeamLogo_Timeout_ReturnsNull()
        {
            var log = new List<string>();
            var client = CreateClient(_ => throw new TaskCanceledException("timeout"), log.Add);

            Assert.Null(await client.LoadTeamLogoAsync(5001));
            Assert.Contains(log, l => l.Contains("5001"));
        }

        private static HttpResponseMessage Json(string json) =>
            new(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

        [Fact]
        public async Task LoadGames_MapsFixtureAndLoadsPlaces()
        {
            var fixture = await File.ReadAllTextAsync(Path.Combine("Data", "actualmatches.json"));
            var client = CreateClient(path =>
            {
                if (path == "/rest/club/id/621/actualmatches?justHome=false&rangeDays=7")
                    return Json(fixture);
                if (path == "/rest/match/id/1001/matchInfo")
                    return Json("""{"data":{"matchInfo":{"spielfeld":{"ort":"Dissen"}}}}""");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var games = await client.LoadGamesAsync();

            Assert.Equal(2, games.Count);
            Assert.Equal("Dissen", games.Single(g => g.MatchId == 1001).Place);
            Assert.Null(games.Single(g => g.MatchId == 1003).Place);
            Assert.DoesNotContain(games, g => g.MatchId == 1002);
        }

        [Fact]
        public async Task LoadTeamLogo_SmallResponse_ReturnsNull()
        {
            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[500])
            });

            Assert.Null(await client.LoadTeamLogoAsync(5001));
        }

        [Fact]
        public async Task LoadTeamLogo_NotFound_ReturnsNull()
        {
            var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            Assert.Null(await client.LoadTeamLogoAsync(5001));
        }

        [Fact]
        public async Task LoadTeamLogo_LargeResponse_ReturnsBytes()
        {
            string? requested = null;
            var client = CreateClient(path =>
            {
                requested = path;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3000]) };
            });

            var bytes = await client.LoadTeamLogoAsync(5001);

            Assert.Equal(3000, bytes!.Length);
            Assert.Equal("/media/team/5001/logo", requested);
        }
    }
}
