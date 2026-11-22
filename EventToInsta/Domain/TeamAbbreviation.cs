using System.Text;
using System.Text.RegularExpressions;

namespace EventToInsta.Domain;

/// <summary>
/// Kürzel für den Logo-Ersatz: Das API-Kürzel (teamnameSmall) bis 6 Zeichen, sonst die Anfangsbuchstaben des Namens.
/// </summary>
public static partial class TeamAbbreviation
{
    private const int MaxShortName = 6;
    private const int MaxInitials = 5;
    private const int MaxTotal = 6;

    public static string For(Team team)
    {
        var shortName = team.ShortName?.Trim() ?? "";
        if (shortName.Length > 0 && shortName.Length <= MaxShortName)
            return shortName.ToUpperInvariant();
        return Initials(team.Name ?? "");
    }

    private static string Initials(string name)
    {
        var words = name.Split([' ', '-', '/', '.', '\t'], StringSplitOptions.RemoveEmptyEntries);

        // Römische Zahl oder Ziffer am Ende (z. B. „II“) bleibt unverändert erhalten.
        string? numeral = null;
        var count = words.Length;
        if (count > 1 && Numeral().IsMatch(words[^1]))
        {
            numeral = words[^1];
            count--;
        }

        var initials = new StringBuilder();
        for (var i = 0; i < count && initials.Length < MaxInitials; i++)
        {
            var first = words[i][0];
            // Füllwörter wie „und“, „e.“, „v.“ (klein geschrieben) und Wörter ohne Buchstabenanfang entfallen.
            var registeredAssociation = words[i] == "V" && i > 0 && words[i - 1] == "e";
            if (char.IsLetter(first) && !char.IsLower(first) && !registeredAssociation)
                initials.Append(char.ToUpperInvariant(first));
        }

        if (numeral != null && initials.Length + numeral.Length <= MaxTotal)
            initials.Append(numeral);
        return initials.ToString();
    }

    [GeneratedRegex(@"^(\d{1,2}|I{1,3}|IV|VI{1,3}|IX|X)$")]
    private static partial Regex Numeral();
}
