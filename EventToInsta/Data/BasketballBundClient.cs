using System.Net.Http.Json;
using EventToInsta.Domain;

namespace EventToInsta.Data
{
    public interface IGameSource
    {
        Task<IReadOnlyList<Game>> LoadGamesAsync(CancellationToken ct = default);
    }

    public interface ILogoSource
    {
        Task<byte[]?> LoadTeamLogoAsync(int teamPermanentId, CancellationToken ct = default);
    }

    public sealed class BasketballBundClient(HttpClient http, int clubId) : IGameSource, ILogoSource
    {
        private const int MinLogoBytes = 2000;

        public async Task<IReadOnlyList<Game>> LoadGamesAsync(CancellationToken ct = default)
        {
            var response = await http.GetFromJsonAsync<MatchesResponse>(
                $"rest/club/id/{clubId}/actualmatches?justHome=false&rangeDays=7", ct);
            var games = GameMapper.Map(response?.Data?.Matches ?? new List<MatchDto>(), clubId,
                msg => Console.WriteLine(msg));

            var result = new List<Game>();
            foreach (var game in games)
                result.Add(game.WithPlace(await LoadPlaceAsync(game.MatchId, ct)));
            return result;
        }

        public async Task<byte[]?> LoadTeamLogoAsync(int teamPermanentId, CancellationToken ct = default)
        {
            try
            {
                using var response = await http.GetAsync($"media/team/{teamPermanentId}/logo", ct);
                if (!response.IsSuccessStatusCode)
                    return null;
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                return bytes.Length < MinLogoBytes ? null : bytes;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private async Task<string?> LoadPlaceAsync(int matchId, CancellationToken ct)
        {
            try
            {
                var info = await http.GetFromJsonAsync<MatchInfoResponse>($"rest/match/id/{matchId}/matchInfo", ct);
                return info?.Data?.MatchInfo?.Spielfeld?.Ort;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
