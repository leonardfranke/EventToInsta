namespace EventToInsta;

internal static class Program
{
    /// <returns>0 bei Erfolg, 1 bei Fehler (Meldung auf stderr).</returns>
    public static async Task<int> Main(string[] args)
    {
        RunOptions options;
        try
        {
            options = RunOptions.Parse(args);
        }
        catch (ArgumentException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }

        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += onCancel;
        try
        {
            var config = AppConfig.FromEnvironment(Environment.GetEnvironmentVariable);
            var run = CompositionRoot.Build(config, options, Console.WriteLine);
            await run(cts.Token);
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Fehler: {Describe(e)}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    /// <summary>Meldung samt Ursachen; AggregateException.Message enthält die inneren Meldungen bereits.</summary>
    private static string Describe(Exception e) =>
        e is AggregateException || e.InnerException is null ? e.Message : $"{e.Message} ({Describe(e.InnerException)})";
}
