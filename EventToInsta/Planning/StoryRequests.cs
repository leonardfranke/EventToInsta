using System.Security.Cryptography;
using System.Text;
using EventToInsta.Domain;

namespace EventToInsta.Planning;

/// <summary>
/// Der Dateiname ist zugleich Teil des Idempotency-Keys beim Upload. Er hängt deshalb an der Identität
/// des Bildes (Spiel bzw. Spiele des Teils), nie nur an seiner Position in der Liste.
/// </summary>
public abstract record StoryRequest(string FileName)
{
    /// <summary>Kurzer, stabiler Hash (8 Hex-Zeichen) über Aufteilung und Spiele eines Jugend-Teils.</summary>
    protected static string PartHash(IReadOnlyList<Game> games, int partCount)
    {
        var identity = $"{partCount}:{string.Join(",", games.Select(g => g.MatchId))}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)), 0, 4).ToLowerInvariant();
    }
}

public record HerrenPreview(Game Game)
    : StoryRequest($"herren-preview-{Game.MatchId}.jpg");

public record JugendPreview(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    IReadOnlyList<Game> Games,
    int Part,
    int PartCount)
    : StoryRequest($"jugend-preview-{Part}-{PartHash(Games, PartCount)}.jpg");

public record HerrenResult(Game Game)
    : StoryRequest($"herren-result-{Game.MatchId}.jpg");

public record JugendResult(
    DateOnly Day,
    IReadOnlyList<Game> Games,
    int Part,
    int PartCount)
    : StoryRequest($"jugend-result-{Day:yyyy-MM-dd}-{Part}-{PartHash(Games, PartCount)}.jpg");
