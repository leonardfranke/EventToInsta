using EventToInsta.Domain;
using EventToInsta.Planning;
using SkiaSharp;
using Topten.RichTextKit;
using static EventToInsta.Rendering.StoryText;

namespace EventToInsta.Rendering;

/// <summary>
/// Jugend-Stories im Stil „Infokarte“: Spielfeld-Hintergrund (<see cref="CourtBackground"/>), zentrierter Kopf, Kartenliste, Slogan.
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
    private const float CardShadowOffset = 2 * S;
    private const float CardShadowSigma = 4 * S; // CSS-Blur 8 px entspricht Sigma 4
    private const float PadX = 12 * S;
    private const float ColumnGap = 10 * S;
    private const float AgeSize = 144;
    private const float AgeRadius = 8 * S;
    private const float Border = 6 * S;
    private const float LineGap = 18;

    // Sponsor
    private const float SponsorLogoWidth = 58 * S;
    private const float SponsorLogoHeight = 28 * S;
    private const float SponsorLabelGap = 12;

    // Ergebniskarte mit Sponsor (Entwurf B in sponsor-results.html): kleine Zeile „unterstützt von [Logo]“
    // unter „Heim“/„Auswärts“; Gegnername und Spielstand behalten ihre volle Breite.
    private const float SponsorLineTextSize = 7.5f * S;
    private const float SponsorLineLogoHeight = 12 * S;
    private const float SponsorLineLogoGap = 4 * S;
    private const float SponsorLineGap = 3 * S + 8; // Abstand Grundlinie „Heim“ bis Oberkante Sponsorzeile

    // Fuß
    private const float FooterBaseline = Theme.Height - 78;

    internal sealed record ListLayout(float HeaderBottom, float FooterTop, IReadOnlyList<SKRect> Cards);

    public static void DrawPreview(SKCanvas c, JugendPreview r, Func<string, SKBitmap?> sponsorFor)
    {
        EnsureFits(r.Games.Count);
        CourtBackground.Draw(c, SKRect.Create(0, 0, Theme.Width, Theme.Height));
        DrawHeader(c, "SPIELE", PreviewDateLine(r));

        var layout = Layout(r.Games.Count);
        for (var i = 0; i < r.Games.Count; i++)
        {
            var g = r.Games[i];
            using var sponsor = sponsorFor(g.AgeClass);
            DrawPreviewCard(c, layout.Cards[i], g, PreviewInfoLine(g), sponsor);
        }

        DrawFooter(c, "KOMMT VORBEI & FEUERT UNS AN!");
    }

    public static void DrawResult(SKCanvas c, JugendResult r, Func<string, SKBitmap?> sponsorFor)
    {
        EnsureFits(r.Games.Count);
        if (r.Games.Any(g => !g.HasResult))
            throw new ArgumentException("Jugend-Ergebnis ohne Spielstand", nameof(r));

        CourtBackground.Draw(c, SKRect.Create(0, 0, Theme.Width, Theme.Height));
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

    /// <summary>Nur Tag und Uhrzeit; ob auswärts, zeigt schon „@“ vor dem Gegner.</summary>
    internal static string PreviewInfoLine(Game g) =>
        $"{g.Kickoff.ToString("ddd", Theme.German)} {g.Kickoff.ToString("dd.MM.", Theme.German)} · {g.Kickoff:HH:mm} Uhr";

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

    // ---------- Kopf, Fuß ----------

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
        DrawResultColumn(c, $"{g.OwnPoints}:{g.OpponentPoints}", win ? "SIEG" : "NIEDERLAGE",
            color, card.Right - PadX, card.MidY);

        // Feste Spaltenbreite für den Spielstand, damit der Mittelblock auf allen Karten gleich breit ist.
        var middle = ResultMiddleBlock(card, sponsor != null, scoreColumnWidth);
        var nameStyle = ResultNameStyle();
        var infoStyle = ResultInfoStyle();
        var nameBaseline = middle.Top + CapHeight(nameStyle);
        var infoBaseline = nameBaseline + LineGap + CapHeight(infoStyle);
        DrawLeft(c, OpponentLine(g), nameStyle, middle.Left, nameBaseline, middle.Width, minScale: 0.8f);
        DrawLeft(c, g.IsHome ? "Heim" : "Auswärts", infoStyle, middle.Left, infoBaseline, middle.Width, minScale: 0.8f);
        if (sponsor != null)
            DrawSponsorLine(c, sponsor, middle.Left, infoBaseline + SponsorLineGap, middle.Width);
    }

    private static SKRect ResultAgeBox(SKRect card) =>
        SKRect.Create(card.Left + Border + PadX, card.MidY - AgeSize / 2, AgeSize, AgeSize);

    internal static Style ResultNameStyle() => Display(13 * S, Theme.Blue);

    private static Style ResultInfoStyle() => Body(700, 9 * S, Theme.MutedText);

    private static Style SponsorLineStyle() => Body(500, SponsorLineTextSize, Theme.FaintText);

    /// <summary>
    /// Bereich für Gegner, Infozeile und gegebenenfalls Sponsorzeile in einer Ergebniskarte: zwischen Altersklasse und
    /// Spielstand-Spalte (ohne Angabe: ungünstigster Fall), senkrecht zentriert; oben Versalhöhe des Gegners.
    /// </summary>
    internal static SKRect ResultMiddleBlock(SKRect card, bool sponsored, float? scoreColumnWidth = null)
    {
        var left = ResultAgeBox(card).Right + ColumnGap;
        var right = card.Right - PadX - (scoreColumnWidth ?? ScoreColumnWidth([])) - ColumnGap;
        var height = CapHeight(ResultNameStyle()) + LineGap + CapHeight(ResultInfoStyle());
        if (sponsored)
            height += SponsorLineGap + SponsorLineLogoHeight;
        var top = card.MidY - height / 2;
        return new SKRect(left, top, right, top + height);
    }

    /// <summary>„unterstützt von“ und rechts daneben das Logo, linksbündig ab <paramref name="left"/>, Oberkante <paramref name="top"/>.</summary>
    private static void DrawSponsorLine(SKCanvas c, SKBitmap logo, float left, float top, float maxWidth)
    {
        var label = SponsorLineStyle();
        const string text = "unterstützt von";
        var labelWidth = NaturalWidth(text, label);
        var midY = top + SponsorLineLogoHeight / 2;
        DrawLeft(c, text, label, left, midY + CapHeight(label) / 2, labelWidth + 1);
        var logoLeft = left + labelWidth + SponsorLineLogoGap;
        var logoWidth = Math.Min(logo.Width * SponsorLineLogoHeight / logo.Height, maxWidth - (logoLeft - left));
        if (logoWidth > 0)
            Primitives.DrawContained(c, logo, SKRect.Create(logoLeft, top, logoWidth, SponsorLineLogoHeight));
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
        // Leichter Schatten (Entwurf: 0 2px 8px rgba(0,0,0,.18)) hebt die Karte vom hellen Lichtkegel ab.
        using (var shadow = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(46), IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, CardShadowSigma),
        })
        {
            using var offset = new SKRoundRect(card, CardRadius);
            offset.Offset(0, CardShadowOffset);
            c.DrawRoundRect(offset, shadow);
        }
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
        string first, Style firstStyle, string second, Style secondStyle)
    {
        var width = right - left;
        var firstCap = CapHeight(firstStyle);
        var height = firstCap + LineGap + CapHeight(secondStyle);
        var firstBaseline = midY - height / 2 + firstCap;
        DrawLeft(c, first, firstStyle, left, firstBaseline, width, minScale: 0.8f);
        DrawLeft(c, second, secondStyle, left, midY + height / 2, width, minScale: 0.8f);
    }

    /// <summary>„unterstützt von“ über dem Logo, rechtsbündig; liefert die linke Kante der Spalte.</summary>
    private static float DrawSponsor(SKCanvas c, SKBitmap logo, float right, float midY)
    {
        var label = Body(500, 7 * S, Theme.FaintText);
        var width = Math.Max(NaturalWidth("unterstützt von", label), SponsorLogoWidth);
        var left = right - width;
        var cap = CapHeight(label);
        var top = midY - (cap + SponsorLabelGap + SponsorLogoHeight) / 2;
        DrawCentered(c, "unterstützt von", label, left, width, top + cap, minScale: 1f);
        var box = SKRect.Create(left + (width - SponsorLogoWidth) / 2, top + cap + SponsorLabelGap,
            SponsorLogoWidth, SponsorLogoHeight);
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
