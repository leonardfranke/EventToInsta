using EventToInsta.Data;
using EventToInsta.Jobs;
using EventToInsta.Publishing;
using EventToInsta.Rendering;
using EventToInsta.State;
using EventToInsta.Time;
using Google.Cloud.Storage.V1;

namespace EventToInsta;

/// <summary>Verdrahtet die Bausteine je nach Konfiguration und Aufrufoptionen (ohne DI-Framework).</summary>
public static class CompositionRoot
{
    public const string OutputDirectory = "Output";
    private const string StateFile = "posted.json";
    private static readonly Uri BasketballBund = new("https://www.basketball-bund.net/");
    private static readonly TimeSpan UploadTimeout = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Prüft die Konfiguration sofort (z. B. fehlender API-Key) und liefert den Lauf. HTTP-Clients leben nur
    /// für die Dauer eines Laufs, damit wiederholte Aufrufe der Cloud Function keine Verbindungen anhäufen.
    /// </summary>
    public static Func<CancellationToken, Task> Build(AppConfig cfg, RunOptions opt, Action<string> log)
    {
        if (!RunOptions.Jobs.Contains(opt.Job))
            throw new ArgumentException($"Unbekannter Job: \"{opt.Job}\"");
        if (!opt.DryRun && cfg.UploadPostApiKey is null)
            throw new InvalidOperationException(
                "UPLOAD_POST_API_KEY ist nicht gesetzt. Ohne --dry-run wird der API-Key von upload-post.com benötigt.");

        Fonts.Register(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts"));
        var imagesDirectory = Path.Combine(AppContext.BaseDirectory, "Images");
        IClock clock = opt.Now is { } now ? new FixedClock(now) : new SystemClock();

        return async ct =>
        {
            using var bundHttp = new HttpClient { BaseAddress = BasketballBund };
            using var uploadHttp = new HttpClient { Timeout = UploadTimeout };

            IGameSource games;
            ILogoSource logos;
            if (opt.Sample)
            {
                games = new SampleGameSource(clock, opt.Job == RunOptions.PreviewJob ? SampleSet.Week : SampleSet.Today);
                logos = new SampleLogoSource(await File.ReadAllBytesAsync(Path.Combine(imagesDirectory, "Logos", "Dachse.png"), ct));
            }
            else
            {
                var client = new BasketballBundClient(bundHttp, cfg.ClubId, log);
                games = client;
                logos = client;
            }

            IPostedStateStore state = opt.DryRun ? new NullPostedStateStore()
                : cfg.StateBucket is { } bucket ? new GcsPostedStateStore(await StorageClient.CreateAsync(), bucket)
                : new FilePostedStateStore(StateFile);

            IStoryPublisher publisher;
            if (opt.DryRun)
            {
                ClearOutputDirectory();
                publisher = new LocalFolderPublisher(OutputDirectory);
                log($"Testlauf: nichts wird hochgeladen, Bilder landen in {Path.GetFullPath(OutputDirectory)}");
            }
            else
            {
                publisher = new UploadPostPublisher(uploadHttp, cfg.UploadPostApiKey!, cfg.UploadPostUser);
            }

            var renderer = new StoryRenderer(new AssetStore(imagesDirectory, cfg.Sponsors, new Random()), logos, cfg.HomePlace);

            if (opt.Job == RunOptions.PreviewJob)
                await new PreviewJob(games, renderer, publisher, state, clock, JugendStory.MaxCards, log).RunAsync(ct);
            else
                await new ResultsJob(games, renderer, publisher, state, clock, JugendStory.MaxCards, log).RunAsync(ct);
        };
    }

    /// <summary>Nur Dateien, keine Unterordner: ein Testlauf zeigt genau die Bilder dieses Laufs.</summary>
    private static void ClearOutputDirectory()
    {
        if (!Directory.Exists(OutputDirectory)) return;
        foreach (var file in Directory.EnumerateFiles(OutputDirectory))
            File.Delete(file);
    }
}
