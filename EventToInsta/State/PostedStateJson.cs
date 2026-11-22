using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventToInsta.State;

/// <summary>JSON-Format von posted.json, gemeinsam fÃ¼r Datei- und GCS-Store.</summary>
internal static class PostedStateJson
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private sealed record Dto(
        [property: JsonPropertyName("previewWeek")] string? PreviewWeek,
        [property: JsonPropertyName("date")] string? Date,
        [property: JsonPropertyName("herrenMatchIds")] int[]? HerrenMatchIds,
        [property: JsonPropertyName("jugendPosted")] bool JugendPosted);

    public static string Serialize(PostedState s) =>
        JsonSerializer.Serialize(
            new Dto(s.PreviewWeek, s.Date?.ToString("yyyy-MM-dd"), s.HerrenMatchIds.ToArray(), s.JugendPosted),
            Options);

    /// <summary>Wirft InvalidDataException (mit <paramref name="source"/> in der Meldung) bei ungültigem Inhalt, nie stilles Empty.</summary>
    public static PostedState Deserialize(string json, string source)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<Dto>(json, Options)
                      ?? throw new JsonException("Inhalt ist null.");
            return new PostedState(
                dto.PreviewWeek,
                dto.Date is null ? null : DateOnly.ParseExact(dto.Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                dto.HerrenMatchIds ?? Array.Empty<int>(),
                dto.JugendPosted);
        }
        catch (Exception e) when (e is JsonException or FormatException)
        {
            throw new InvalidDataException($"Zustandsdatei ungültig: {source}", e);
        }
    }
}
