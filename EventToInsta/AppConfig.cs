using System.Globalization;

namespace EventToInsta;

public record AppConfig(
    int ClubId,
    string HomePlace,
    string? UploadPostApiKey,
    string UploadPostUser,
    string? StateBucket,
    IReadOnlyDictionary<string, string> Sponsors)
{
    public const int DefaultClubId = 621;
    public const string DefaultHomePlace = "Bad Laer";
    public const string DefaultUploadPostUser = "Dachse";
    public const string DefaultSponsors = "U12=Böhnke.png;U14=Mann.png;U16=Stockmeyer.webp;U18=Mittelstedt.png";

    /// <summary>Liest die Umgebungsvariablen; leere Werte gelten als nicht gesetzt.</summary>
    public static AppConfig FromEnvironment(Func<string, string?> getEnv)
    {
        string? Get(string key) => getEnv(key) is { } v && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

        var clubIdText = Get("CLUB_ID");
        var clubId = DefaultClubId;
        if (clubIdText is not null && !int.TryParse(clubIdText, NumberStyles.None, CultureInfo.InvariantCulture, out clubId))
            throw new ArgumentException($"CLUB_ID ist keine gültige Zahl: \"{clubIdText}\"");

        return new AppConfig(
            clubId,
            Get("HOME_PLACE") ?? DefaultHomePlace,
            Get("UPLOAD_POST_API_KEY"),
            Get("UPLOAD_POST_USER") ?? DefaultUploadPostUser,
            Get("STATE_BUCKET"),
            ParseSponsors(Get("SPONSORS") ?? DefaultSponsors));
    }

    /// <summary>Format <c>AK=Datei;AK=Datei</c>.</summary>
    private static IReadOnlyDictionary<string, string> ParseSponsors(string value)
    {
        var result = new Dictionary<string, string>();
        foreach (var entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split('=', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                throw new ArgumentException($"SPONSORS-Eintrag ungültig (erwartet AK=Datei): \"{entry}\"");
            result[parts[0]] = parts[1];
        }
        return result;
    }
}
