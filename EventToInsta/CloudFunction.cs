using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;

namespace EventToInsta;

/// <summary>HTTP-Einstieg für Cloud Scheduler: <c>?job=preview|results</c>. Läuft nie als Testlauf.</summary>
public sealed class CloudFunction : IHttpFunction
{
    private readonly Func<string, string?> _getEnv;

    /// <summary>Vom Functions-Framework genutzt: liest die echte Umgebung.</summary>
    public CloudFunction() : this(Environment.GetEnvironmentVariable) { }

    /// <summary>Für Tests: Umgebung austauschbar. Internal, damit das Framework nur den parameterlosen Konstruktor sieht.</summary>
    internal CloudFunction(Func<string, string?> getEnv) => _getEnv = getEnv;

    public async Task HandleAsync(HttpContext context)
    {
        var job = context.Request.Query["job"].ToString();
        if (!RunOptions.Jobs.Contains(job))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(
                $"Unbekannter oder fehlender Job \"{job}\". Erlaubt: ?job={string.Join("|", RunOptions.Jobs)}");
            return;
        }

        try
        {
            var config = AppConfig.FromEnvironment(_getEnv);
            var run = CompositionRoot.Build(config, new RunOptions(job, DryRun: false, Sample: false, Now: null), Console.WriteLine,
                requireStateBucket: true);
            await run(context.RequestAborted);
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("ok");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Job {job} fehlgeschlagen: {e}");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync($"Job {job} fehlgeschlagen: {e.Message}");
        }
    }
}
