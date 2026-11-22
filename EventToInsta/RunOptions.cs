using System.Globalization;

namespace EventToInsta;

/// <param name="Now">Feste Berliner Ortszeit für Tests und Nachholläufe, sonst null.</param>
public record RunOptions(string Job, bool DryRun, bool Sample, DateTime? Now)
{
    public const string PreviewJob = "preview";
    public const string ResultsJob = "results";
    public static readonly IReadOnlyList<string> Jobs = [PreviewJob, ResultsJob];

    public const string Usage =
        """
        Aufruf: EventToInsta <preview|results> [--dry-run] [--sample] [--now yyyy-MM-ddTHH:mm]
          preview    Wochenvorschau (Herren und Jugend) posten
          results    Ergebnisse des Tages posten
          --dry-run  nichts hochladen, Bilder nach Output/ schreiben, keinen Zustand speichern
          --sample   Beispieldaten statt basketball-bund.net verwenden
          --now      feste Uhrzeit (Berliner Ortszeit), z. B. 2026-11-23T08:30
        """;

    public static RunOptions Parse(string[] args)
    {
        if (args.Length == 0)
            throw Error("Kein Job angegeben.");
        var job = args[0];
        if (!Jobs.Contains(job))
            throw Error($"Unbekannter Job: \"{job}\".");

        bool dryRun = false, sample = false;
        DateTime? now = null;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--sample":
                    sample = true;
                    break;
                case "--now":
                    if (i + 1 >= args.Length)
                        throw Error("--now braucht einen Wert.");
                    var value = args[++i];
                    if (!DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var parsed))
                        throw Error($"--now erwartet yyyy-MM-ddTHH:mm, nicht \"{value}\".");
                    now = parsed;
                    break;
                default:
                    throw Error($"Unbekanntes Argument: \"{args[i]}\".");
            }
        }
        return new RunOptions(job, dryRun, sample, now);
    }

    private static ArgumentException Error(string message) => new($"{message}{Environment.NewLine}{Usage}");
}
