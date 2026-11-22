using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventToInsta.State;

/// <summary>JSON-Format von posted.json, gemeinsam für Datei- und GCS-Store.</summary>
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

    public static PostedState Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<Dto>(json, Options);
        if (dto is null) return PostedState.Empty;
        return new PostedState(
            dto.PreviewWeek,
            dto.Date is null ? null : DateOnly.ParseExact(dto.Date, "yyyy-MM-dd"),
            dto.HerrenMatchIds ?? Array.Empty<int>(),
            dto.JugendPosted);
    }
}
