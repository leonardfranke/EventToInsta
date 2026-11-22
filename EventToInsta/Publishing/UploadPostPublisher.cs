using System.Net.Http.Headers;
using System.Text.Json;
using EventToInsta.Rendering;

namespace EventToInsta.Publishing;

public sealed class UploadFailedException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class UploadPostPublisher(HttpClient http, string apiKey, string user) : IStoryPublisher
{
    private const string Endpoint = "https://api.upload-post.com/api/upload_photos";
    private const int MaxBodyChars = 500;

    public async Task PublishAsync(StoryPost post, CancellationToken ct = default)
    {
        var duplicate = post.Images.GroupBy(i => i.FileName).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Beitrag {post.Key}: Dateiname {duplicate.Key} kommt mehrfach vor.", nameof(post));

        foreach (var image in post.Images)
            await UploadAsync(image, IdempotencyKey(post.Key, image), ct);
    }

    /// <summary>
    /// Je Bild aus stabiler Identität (Dateiname, bei Jugend-Teilen mit Hash der Spiele), nie aus der
    /// Position: Ändert sich die Bilderliste zwischen Fehlschlag und Wiederholung, bekäme sonst ein neues
    /// Bild den Key eines alten, und upload-post.com würde es als bereits erledigt verwerfen.
    /// </summary>
    internal static string IdempotencyKey(string postKey, RenderedStory image) =>
        $"{postKey}-{Path.GetFileNameWithoutExtension(image.FileName)}";

    private async Task UploadAsync(RenderedStory image, string idempotencyKey, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(user), "user");
        form.Add(new StringContent("instagram"), "platform[]");
        form.Add(new StringContent("STORIES"), "media_type");
        var file = new ByteArrayContent(image.Jpeg);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "photos[]", image.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Apikey", apiKey);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new UploadFailedException($"Upload von {image.FileName} fehlgeschlagen: {ex.Message}", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
                throw Failure(image, $"HTTP {status}", body);
            if (!IsAccepted(body))
                throw Failure(image, $"HTTP {status} ohne success=true", body);
        }
    }

    private static bool IsAccepted(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("success", out var success)
                || success.ValueKind != JsonValueKind.True)
                return false;
            if (root.TryGetProperty("results", out var results)
                && results.ValueKind == JsonValueKind.Object
                && results.TryGetProperty("instagram", out var ig)
                && ig.ValueKind == JsonValueKind.Object
                && ig.TryGetProperty("success", out var igSuccess)
                && igSuccess.ValueKind == JsonValueKind.False)
                return false;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static UploadFailedException Failure(RenderedStory image, string status, string body)
    {
        if (body.Length > MaxBodyChars) body = body[..MaxBodyChars] + "...";
        return new UploadFailedException($"Upload von {image.FileName} fehlgeschlagen: {status}, Antwort: {body}");
    }
}
