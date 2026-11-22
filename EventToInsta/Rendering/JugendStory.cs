using EventToInsta.Domain;
using EventToInsta.Planning;
using SkiaSharp;
using Topten.RichTextKit;
using static EventToInsta.Rendering.StoryText;

namespace EventToInsta.Rendering;

/// <summary>
/// Jugend-Stories im Stil „Infokarte“: abgedunkeltes Foto, zentrierter Kopf, Kartenliste, Slogan.
/// Geometrie aus den Entwürfen (300 px breit) mal 3,6; Kartenhöhe so gewählt, dass <see cref="MaxCards"/>
/// Karten auch mit Sponsor und langen Namen zwischen Kopf und Fuß passen.
/// </summary>
public static class JugendStory
{
    public const int MaxCards = 6;

    private const float S = 3.6f;

    // Kopf
    private const float KickerCapTop = 100;
    private const float KickerToTitle = 54; // Platz für Umlautpunkte über den Versalien (Ä)
    private const float TitleToDate = 32;
    private const float HeaderToList = 44;

    // Liste
    private const float ListMargin = 16 * S;
    private const float CardHeight = 204;
    private const float CardGap = 24;
    private const float CardRadius = 12 * S;
    private const float PadX = 12 * S;
    private const float ColumnGap = 10 * S;
    private const float AgeSize = 144;
    private const float AgeRadius = 8 * S;
    private const float Border = 6 * S;
    private const float LineGap = 18;

    // Sponsor
    private const float SponsorLogoWidth = 46 * S;
    private const float SponsorLogoHeight = 22 * S;
    private const float SponsorLabelGap = 12;

    // Ergebniskarte mit Sponsor (Entwurf A in sponsor-results.html): kleinerer Sponsorblock, engere Abstände,
    // Gegnername darf stärker schrumpfen, damit übliche Vereinsnamen ungekürzt bleiben.
    private const float ResultSponsorScale = 0.75f;
    private const float ResultSponsorGap = 5 * S;
    internal const float ResultNameMinScale = 0.7f;

    // Fuß
    private const float FooterBaseline = Theme.Height - 78;

    internal sealed record ListLayout(float HeaderBottom, float FooterTop, IReadOnlyList<SKRect> Cards);

    public static void DrawPreview(SKCanvas c, JugendPreview r, SKBitmap photo, Func<string, SKBitmap?> sponsorFor, string homePlace)
    {
        EnsureFits(r.Games.Count);
        DrawBackground(c, photo);
        DrawHeader(c, r.Home ? "HEIMSPIELE" : "AUSWÄRTSSPIELE", PreviewDateLine(r));

        var layout = Layout(r.Games.Count);
        for (var i = 0; i < r.Games.Count; i++)
        {
            var g = r.Games[i];
            using var sponsor = sponsorFor(g.AgeClass);
            DrawPreviewCard(c, layout.Cards[i], g, PreviewInfoLine(g, homePlace), sponsor);
        }

        DrawFooter(c, "KOMMT VORBEI & FEUERT UNS AN!");
    }

    public static void DrawResult(SKCanvas c, JugendResult r, SKBitmap photo, Func<string, SKBitmap?> sponsorFor)
    {
        EnsureFits(r.Games.Count);
        if (r.Games.Any(g => !g.HasResult))
            throw new ArgumentException("Jugend-Ergebnis ohne Spielstand", nameof(r));

        DrawBackground(c, photo);
        DrawHeader(c, "ERGEBNISSE", ResultDateLine(r));

        var layout = Layout(r.Games.Count);
        var scoreColumnWidth = ScoreColumnWidth(r.Games);
        for (var i = 0; i < r.Games.Count; i++)
        {
            var g = r.Games[i];
            using var sponsor = sponsorFor(g.AgeClass);
            DrawResultCard(c, layout.Cards[i], g, sponsor, scoreColumnWidth);
        }

        DrawFooter(c, "STARK GESPIELT, TEAMS!");
    }

    internal static string PreviewInfoLine(Game g, string homePlace)
    {
        var line = $"{g.Kickoff.ToString("ddd", Theme.German)} {g.Kickoff.ToString("dd.MM.", Theme.German)} · {g.Kickoff:HH:mm} Uhr";
        if (!string.IsNullOrWhiteSpace(g.Place)
            && !string.Equals(g.Place.Trim(), homePlace.Trim(), StringComparison.OrdinalIgnoreCase))
            line += $" · in {g.Place.Trim()}";
        return line;
    }

    internal static string PreviewDateLine(JugendPreview r)
    {
        var start = r.WeekStart;
        var end = r.WeekEnd;
        var range = start == end ? $"{end:dd.MM.}"
            : start.Month == end.Month && start.Year == end.Year ? $"{start:dd.}–{end:dd.MM.}"
            : $"{start:dd.MM.}–{end:dd.MM.}";
        return $"Diese Woche · {range}" + PartSuffix(r.Part, r.PartCount);
    }

    internal static string ResultDateLine(JugendResult r) =>
        $"{r.Day.ToString("dddd", Theme.German)}, {r.Day.ToString("dd.MM.", Theme.German)}" + PartSuffix(r.Part, r.PartCount);

    private static string PartSuffix(int part, int count) => count > 1 ? $" ({part}/{count})" : "";

    /// <summary>Lage von Kopf, Fuß und Karten; die Karten stehen ab Kopfende untereinander.</summary>
    internal static ListLayout Layout(int count)
    {
        var header = HeaderMetrics();
        var footerTop = FooterBaseline - CapHeight(FooterStyle());
        var top = header.Bottom + HeaderToList;
        var cards = Enumerable.Range(0, count)
            .Select(i => SKRect.Create(ListMargin, top + i * (CardHeight + CardGap), Theme.Width - 2 * ListMargin, CardHeight))
            .ToList();
        return new ListLayout(header.Bottom, footerTop, cards);
    }

    private static void EnsureFits(int count)
    {
        if (count > MaxCards)
            throw new ArgumentOutOfRangeException(nameof(count), count, $"Höchstens {MaxCards} Karten pro Story");
    }

    // ---------- Styles ----------

    private static Style KickerStyle() => new()
    {
        FontFamily = Theme.Body, FontWeight = 700, FontSize = 10 * S, LetterSpacing = 4 * S,
        TextColor = Theme.White.WithAlpha(204),
    };

    private static Style TitleStyle() => new()
    {
        FontFamily = Theme.Display, FontSize = 36 * S, LetterSpacing = 3 * S, TextColor = Theme.White,
    };

    private static Style DateStyle() => new()
    {
        FontFamily = Theme.Body, FontWeight = 700, FontSize = 12 * S, TextColor = Theme.White.WithAlpha(230),
    };

    private static Style FooterStyle() => new()
    {
        FontFamily = Theme.Body, FontWeight = 700, FontSize = 10 * S, LetterSpacing = 2 * S,
        TextColor = Theme.White.WithAlpha(217),
    };

    private static Style Display(float size, SKColor color) =>
        new() { FontFamily = Theme.Display, FontSize = size, TextColor = color };

    private static Style Body(int weight, float size, SKColor color, float letterSpacing = 0) =>
        new() { FontFamily = Theme.Body, FontWeight = weight, FontSize = size, TextColor = color, LetterSpacing = letterSpacing };

    // ---------- Hintergrund, Kopf, Fuß ----------

    private static void DrawBackground(SKCanvas c, SKBitmap photo)
    {
        c.Clear(Theme.Blue);
        var full = SKRect.Create(0, 0, Theme.Width, Theme.Height);
        Primitives.DrawCover(c, photo, full);
        using var shade = new SKPaint { Color = Theme.Shade };
        c.DrawRect(full, shade);
    }

    private readonly record struct Header(float KickerBaseline, float TitleBaseline, float DateBaseline, float Bottom);

    private static Header HeaderMetrics()
    {
        var kicker = KickerCapTop + CapHeight(KickerStyle());
        var title = kicker + KickerToTitle + CapHeight(TitleStyle());
        var date = title + TitleToDate + CapHeight(DateStyle());
        return new Header(kicker, title, date, date + Descent(DateStyle()));
    }

    private static void DrawHeader(SKCanvas c, string title, string date)
    {
        var h = HeaderMetrics();
        const float width = Theme.Width - 2 * ListMargin;
        DrawCentered(c, "SV BAD LAER · JUGEND", KickerStyle(), ListMargin, width, h.KickerBaseline, letterSpacingCorrection: true);
        DrawCentered(c, title, TitleStyle(), ListMargin, width, h.TitleBaseline, minScale: 0.7f, letterSpacingCorrection: true);
        DrawCentered(c, date, DateStyle(), ListMargin, width, h.DateBaseline);
    }

    private static void DrawFooter(SKCanvas c, string slogan) =>
        DrawCentered(c, slogan, FooterStyle(), ListMargin, Theme.Width - 2 * ListMargin, FooterBaseline,
            letterSpacingCorrection: true);

    // ---------- Karten ----------

    private static void DrawPreviewCard(SKCanvas c, SKRect card, Game g, string info, SKBitmap? sponsor)
    {
        DrawCardBody(c, card, border: null);
        var age = SKRect.Create(card.Left + PadX, card.MidY - AgeSize / 2, AgeSize, AgeSize);
        DrawAge(c, age, g.AgeClass, 15 * S);

        var right = card.Right - PadX;
        if (sponsor != null)
            right = DrawSponsor(c, sponsor, right, card.MidY) - ColumnGap;

        DrawTwoLines(c, age.Right + ColumnGap, right, card.MidY,
            OpponentLine(g), Display(14 * S, Theme.Blue),
            info, Body(700, 9.5f * S, Theme.MutedText));
    }

    private static void DrawResultCard(SKCanvas c, SKRect card, Game g, SKBitmap? sponsor, float scoreColumnWidth)
    {
        var win = g.IsWin == true;
        DrawCardBody(c, card, border: win ? Theme.Blue : Theme.Grey);
        var age = ResultAgeBox(card);
        DrawAge(c, age, g.AgeClass, 14 * S);

        var color = win ? Theme.Blue : Theme.LossText;
        var scoreRight = card.Right - PadX;
        var right = DrawResultColumn(c, $"{g.OwnPoints}:{g.OpponentPoints}", win ? "SIEG" : "NIEDERLAGE",
            color, scoreRight, card.MidY) - ColumnGap;
        var minScale = 0.8f;
        if (sponsor != null)
        {
            // Feste Spaltenbreite für den Spielstand, damit die Sponsorspalte auf allen Karten an derselben Stelle steht.
            var sponsorRight = scoreRight - scoreColumnWidth - ResultSponsorGap;
            right = DrawSponsor(c, sponsor, sponsorRight, card.MidY, ResultSponsorScale) - ResultSponsorGap;
            minScale = ResultNameMinScale;
        }

        DrawTwoLines(c, age.Right + ColumnGap, right, card.MidY,
            OpponentLine(g), ResultNameStyle(),
            g.IsHome ? "Heim" : "Auswärts", Body(700, 9 * S, Theme.MutedText), minScale);
    }

    private static SKRect ResultAgeBox(SKRect card) =>
        SKRect.Create(card.Left + Border + PadX, card.MidY - AgeSize / 2, AgeSize, AgeSize);

    internal static Style ResultNameStyle() => Display(13 * S, Theme.Blue);

    /// <summary>Breite für Gegner und Infozeile in einer Ergebniskarte mit Sponsor (ohne Zeichnen, für Tests).</summary>
    internal static float ResultNameWidthWithSponsor(float scoreColumnWidth)
    {
        var card = Layout(1).Cards[0];
        var sponsorRight = card.Right - PadX - scoreColumnWidth - ResultSponsorGap;
        var sponsorLeft = sponsorRight - SponsorWidth(ResultSponsorScale);
        return sponsorLeft - ResultSponsorGap - (ResultAgeBox(card).Right + ColumnGap);
    }

    /// <summary>Breite der Spielstand-Spalte für den ungünstigsten Fall („100:100“, „NIEDERLAGE“) oder breiter, falls nötig.</summary>
    internal static float ScoreColumnWidth(IEnumerable<Game> games)
    {
        var scoreWidth = games.Select(g => $"{g.OwnPoints}:{g.OpponentPoints}").Append("100:100")
            .Max(score => NaturalWidth(score, ScoreStyle(Theme.Blue)));
        return Math.Max(scoreWidth, VerdictWidth("NIEDERLAGE"));
    }

    private static string OpponentLine(Game g) => (g.IsHome ? "vs " : "@ ") + g.Opponent.Name;

    private static void DrawCardBody(SKCanvas c, SKRect card, SKColor? border)
    {
        using var rounded = new SKRoundRect(card, CardRadius);
        using var white = new SKPaint { Color = Theme.White, IsAntialias = true };
        c.Save();
        c.ClipRoundRect(rounded, antialias: true);
        c.DrawRect(card, white);
        if (border is { } b)
        {
            using var paint = new SKPaint { Color = b, IsAntialias = true };
            c.DrawRect(SKRect.Create(card.Left, card.Top, Border, card.Height), paint);
        }
        c.Restore();
    }

    private static void DrawAge(SKCanvas c, SKRect box, string ageClass, float fontSize)
    {
        using var fill = new SKPaint { Color = Theme.Blue, IsAntialias = true };
        c.DrawRoundRect(box, AgeRadius, AgeRadius, fill);
        var style = Display(fontSize, Theme.White);
        DrawCentered(c, ageClass, style, box.Left + 12, box.Width - 24, box.MidY + CapHeight(style) / 2, minScale: 0.6f);
    }

    /// <summary>Gegner und Infozeile, gemeinsam senkrecht zentriert; beide verkleinern sich, dann „…“.</summary>
    private static void DrawTwoLines(SKCanvas c, float left, float right, float midY,
        string first, Style firstStyle, string second, Style secondStyle, float firstMinScale = 0.8f)
    {
        var width = right - left;
        var firstCap = CapHeight(firstStyle);
        var height = firstCap + LineGap + CapHeight(secondStyle);
        var firstBaseline = midY - height / 2 + firstCap;
        DrawLeft(c, first, firstStyle, left, firstBaseline, width, firstMinScale);
        DrawLeft(c, second, secondStyle, left, midY + height / 2, width, minScale: 0.8f);
    }

    private static Style SponsorLabelStyle(float scale) => Body(500, 7 * S * scale, Theme.FaintText);

    private static float SponsorWidth(float scale) =>
        Math.Max(NaturalWidth("unterstützt von", SponsorLabelStyle(scale)), SponsorLogoWidth * scale);

    /// <summary>„unterstützt von“ über dem Logo, rechtsbündig; liefert die linke Kante der Spalte.</summary>
    private static float DrawSponsor(SKCanvas c, SKBitmap logo, float right, float midY, float scale = 1f)
    {
        var label = SponsorLabelStyle(scale);
        var width = SponsorWidth(scale);
        var logoWidth = SponsorLogoWidth * scale;
        var logoHeight = SponsorLogoHeight * scale;
        var gap = SponsorLabelGap * scale;
        var left = right - width;
        var cap = CapHeight(label);
        var top = midY - (cap + gap + logoHeight) / 2;
        DrawCentered(c, "unterstützt von", label, left, width, top + cap, minScale: 1f);
        var box = SKRect.Create(left + (width - logoWidth) / 2, top + cap + gap, logoWidth, logoHeight);
        Primitives.DrawContained(c, logo, box);
        return left;
    }

    private static Style ScoreStyle(SKColor color) => Display(20 * S, color);

    private static Style VerdictStyle(SKColor color) => Body(800, 8 * S, color, letterSpacing: 1 * S);

    // Laufweite hinter dem letzten Zeichen nicht mitzählen, damit beide Zeilen bündig enden.
    private static float VerdictWidth(string verdict) =>
        NaturalWidth(verdict, VerdictStyle(Theme.Blue)) - VerdictStyle(Theme.Blue).LetterSpacing;

    /// <summary>Spielstand über „SIEG“/„NIEDERLAGE“, rechtsbündig; liefert die linke Kante der Spalte.</summary>
    private static float DrawResultColumn(SKCanvas c, string score, string verdict, SKColor color, float right, float midY)
    {
        var scoreStyle = ScoreStyle(color);
        var verdictStyle = VerdictStyle(color);
        var scoreWidth = NaturalWidth(score, scoreStyle);
        var verdictWidth = VerdictWidth(verdict);
        var scoreCap = CapHeight(scoreStyle);
        var height = scoreCap + LineGap + CapHeight(verdictStyle);
        var scoreBaseline = midY - height / 2 + scoreCap;
        PaintAtBaseline(c, TextFit.Line(score, scoreStyle, scoreWidth + 1), right - scoreWidth, scoreBaseline);
        PaintAtBaseline(c, TextFit.Line(verdict, verdictStyle, verdictWidth + verdictStyle.LetterSpacing + 1),
            right - verdictWidth, midY + height / 2);
        return right - Math.Max(scoreWidth, verdictWidth);
    }

    private static float Descent(Style style)
    {
        var face = FontMapper.Default.TypefaceFromStyle(style, false);
        using var font = new SKFont(face, style.FontSize);
        return font.Metrics.Descent;
    }
}
