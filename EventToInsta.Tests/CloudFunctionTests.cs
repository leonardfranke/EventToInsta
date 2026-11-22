using Microsoft.AspNetCore.Http;

namespace EventToInsta.Tests;

[Collection(ConsoleCollection.Name)]
public class CloudFunctionTests
{
    private static async Task<(int Status, string Body)> InvokeAsync(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        using var body = new MemoryStream();
        context.Response.Body = body;

        await new CloudFunction().HandleAsync(context);

        return (context.Response.StatusCode, System.Text.Encoding.UTF8.GetString(body.ToArray()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?job=posten")]
    public async Task MissingOrUnknownJob_Returns400(string query)
    {
        var (status, body) = await InvokeAsync(query);

        Assert.Equal(400, status);
        Assert.Contains("preview", body);
    }

    [Fact]
    public async Task WithoutStateBucket_Returns500BeforeLoadingAnything()
    {
        // Die Prüfung liest die echte Umgebung; ist STATE_BUCKET auf der Maschine gesetzt, ist der Fall nicht prüfbar.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STATE_BUCKET")))
            return;

        var (status, body) = await InvokeAsync("?job=results");

        Assert.Equal(500, status);
        Assert.Contains("STATE_BUCKET ist nicht gesetzt", body);
    }
}
