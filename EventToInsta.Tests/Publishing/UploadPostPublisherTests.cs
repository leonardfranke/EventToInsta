using System.Net;
using System.Text;
using EventToInsta.Publishing;
using EventToInsta.Rendering;

namespace EventToInsta.Tests.Publishing;

public class UploadPostPublisherTests
{
    private sealed record Captured(HttpRequestMessage Request, byte[] Body);

    private sealed class FakeHandler(Func<int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Captured> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = await request.Content!.ReadAsByteArrayAsync(ct);
            Calls.Add(new Captured(request, body));
            return respond(Calls.Count);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string json) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static readonly byte[] JpegA = [0xFF, 0xD8, 0x01, 0x02, 0x03, 0xFF, 0xD9];
    private static readonly byte[] JpegB = [0xFF, 0xD8, 0x09, 0xFF, 0xD9];

    private static StoryPost Post(string key = "jugend-result-2026-11-21") =>
        new(key, [new RenderedStory("a.jpg", JpegA), new RenderedStory("b.jpg", JpegB)]);

    private static (UploadPostPublisher, FakeHandler) Create(Func<int, HttpResponseMessage> respond)
    {
        var handler = new FakeHandler(respond);
        return (new UploadPostPublisher(new HttpClient(handler), "test-key", "Dachse"), handler);
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle)) return i;
        return -1;
    }

    [Fact]
    public async Task Publish_TwoImages_TwoRequestsWithIdempotencyKeys()
    {
        var (publisher, handler) = Create(_ => Json(HttpStatusCode.OK, """{"success": true}"""));

        await publisher.PublishAsync(Post());

        Assert.Equal(2, handler.Calls.Count);
        Assert.Equal("jugend-result-2026-11-21-1", handler.Calls[0].Request.Headers.GetValues("Idempotency-Key").Single());
        Assert.Equal("jugend-result-2026-11-21-2", handler.Calls[1].Request.Headers.GetValues("Idempotency-Key").Single());
        Assert.All(handler.Calls, c =>
        {
            Assert.Equal(HttpMethod.Post, c.Request.Method);
            Assert.Equal("https://api.upload-post.com/api/upload_photos", c.Request.RequestUri!.ToString());
        });
    }

    [Fact]
    public async Task Publish_SendsRequiredFormFields()
    {
        var (publisher, handler) = Create(_ => Json(HttpStatusCode.OK,
            """{"success": true, "results": {"instagram": {"success": true}}}"""));

        await publisher.PublishAsync(Post());

        var call = handler.Calls[0];
        Assert.Equal("Apikey", call.Request.Headers.Authorization!.Scheme);
        Assert.Equal("test-key", call.Request.Headers.Authorization!.Parameter);

        // Rohe Multipart-Bytes, so wie der Server sie sieht (Namen in Anführungszeichen, ohne Prozent-Kodierung)
        var text = Encoding.Latin1.GetString(call.Body);
        Assert.Contains("name=user\r\n\r\nDachse\r\n", text);
        Assert.Contains("name=\"platform[]\"\r\n\r\ninstagram\r\n", text);
        Assert.Contains("name=media_type\r\n\r\nSTORIES\r\n", text);
        Assert.Contains("name=\"photos[]\"; filename=a.jpg", text);
        Assert.Contains("Content-Type: image/jpeg", text);
        Assert.True(IndexOf(call.Body, JpegA) > 0);
        Assert.Equal(-1, IndexOf(call.Body, JpegB));
        Assert.DoesNotContain("test-key", text);
    }

    [Fact]
    public async Task Publish_200WithSuccessFalse_Throws()
    {
        var (publisher, _) = Create(_ => Json(HttpStatusCode.OK, """{"success": false, "message": "nope"}"""));

        var ex = await Assert.ThrowsAsync<UploadFailedException>(() => publisher.PublishAsync(Post()));

        Assert.Contains("200", ex.Message);
        Assert.Contains("nope", ex.Message);
        Assert.DoesNotContain("test-key", ex.Message);
    }

    [Fact]
    public async Task Publish_InstagramResultFailed_Throws()
    {
        var (publisher, _) = Create(_ => Json(HttpStatusCode.OK,
            """{"success": true, "results": {"instagram": {"success": false, "error": "bad"}}}"""));

        await Assert.ThrowsAsync<UploadFailedException>(() => publisher.PublishAsync(Post()));
    }

    [Fact]
    public async Task Publish_AsyncRequestId_IsAccepted()
    {
        var (publisher, handler) = Create(_ => Json(HttpStatusCode.OK, """{"success": true, "request_id": "abc"}"""));

        await publisher.PublishAsync(Post());

        Assert.Equal(2, handler.Calls.Count);
    }

    [Fact]
    public async Task Publish_NonJsonBody_Throws()
    {
        var (publisher, _) = Create(_ => Json(HttpStatusCode.OK, "<html>oops</html>"));

        await Assert.ThrowsAsync<UploadFailedException>(() => publisher.PublishAsync(Post()));
    }

    [Fact]
    public async Task Publish_500_Throws()
    {
        var (publisher, handler) = Create(_ => Json(HttpStatusCode.InternalServerError, new string('x', 2000)));

        var ex = await Assert.ThrowsAsync<UploadFailedException>(() => publisher.PublishAsync(Post()));

        Assert.Contains("500", ex.Message);
        Assert.Contains("xxx", ex.Message);
        Assert.True(ex.Message.Length < 800);
        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task Publish_SecondImageFails_ThrowsAfterFirstSent()
    {
        var (publisher, handler) = Create(n => n == 1
            ? Json(HttpStatusCode.OK, """{"success": true}""")
            : Json(HttpStatusCode.BadGateway, "bad gateway"));

        var ex = await Assert.ThrowsAsync<UploadFailedException>(() => publisher.PublishAsync(Post()));

        Assert.Equal(2, handler.Calls.Count);
        Assert.Contains("502", ex.Message);
    }
}
