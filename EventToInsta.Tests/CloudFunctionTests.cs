using Microsoft.AspNetCore.Http;

namespace EventToInsta.Tests;

[Collection(ConsoleCollection.Name)]
public class CloudFunctionTests
{
    private static async Task<(int Status, string Body)> InvokeAsync(string query, Func<string, string?>? env = null)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        using var body = new MemoryStream();
        context.Response.Body = body;

        var function = env is null ? new CloudFunction() : new CloudFunction(env);
        await function.HandleAsync(context);

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
        // Leere Umgebung statt der echten: der Test gilt auch, wenn STATE_BUCKET auf der Maschine gesetzt ist.
        var (status, body) = await InvokeAsync("?job=results", _ => null);

        Assert.Equal(500, status);
        Assert.Contains("STATE_BUCKET ist nicht gesetzt", body);
    }
}
