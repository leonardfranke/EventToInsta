using EventToInsta.Domain;
using EventToInsta.Planning;
using SkiaSharp;
using Topten.RichTextKit;

namespace EventToInsta.Rendering;

using Team = EventToInsta.Domain.Team;

/// <summary>
/// Herren-Stories im Stil „Matchday“. Geometrie aus den Entwürfen (300 px breit) mal 3,6.
/// Alle Textzeilen werden über ihre Grundlinie gesetzt, damit die Abstände unabhängig von den
/// Zeilenmetriken der Schrift fest bleiben und sich nichts überlappt.
/// </summary>
public static class HerrenStory
{
    private const float S = 3.6f;
    private const float Margin = 20 * S;

    private const float PhotoBottomLeft = 293 * S;
    private const float PhotoBottomRight = 220 * S;
    private const float StripeHeight = 11 * S;
    private const float LineTopLeft = 309 * S;
    private const float LineTopRight = 236 * S;
    private const float LineHeight = 4 * S;

    // Fußzeile: Trennlinie, darunter Logos (44 px) mit 12 px Abstand, 20 px Rand unten.
    private const float LogoSize = 44 * S;
    private const float FooterBottom = Theme.Height - Margin;
    private const float FooterLogoTop = FooterBottom - LogoSize;
    private const float FooterRule = FooterLogoTop - 12 * S;
    private const float FooterGap = 10 * S;

    // Unterer Inhaltsblock: Infozeile endet deutlich über der Trennlinie.
    private const float InfoGapAboveRule = 52;
    private const float InfoBaseline = FooterRule - InfoGapAboveRule;

    // Oberkanten der Großbuchstaben im Inhaltsblock unter der Schräge.
    private const float GameDayCapTop = 1170;
    private const float ScoreCapTop = 1170;
    private const float VerdictCapTop = 1440;

    // Logos sitzen auf weißem Kreis; der Einzug hält auch rechteckige Logos innerhalb des Kreises.
    private const float LogoInset = 0.15f;

    private const float Unlimited = 100_000f;

    private static readonly SKColor Dimmed = Theme.White.WithAlpha(140);

    public static void DrawPreview(SKCanvas c, HerrenPreview r, SKBitmap photo, SKBitmap clubLogo, SKBitmap? opponentLogo) =>
        DrawPreview(c, r, photo, clubLogo, opponentLogo, homePlace: null);

    internal static void DrawPreview(
        SKCanvas c, HerrenPreview r, SKBitmap photo, SKBitmap clubLogo, SKBitmap? opponentLogo, string? homePlace)
    {
        var g = r.Game;
        DrawBackground(c, photo);
        DrawTag(c, g.IsHome ? "HEIMSPIEL" : "AUSWÄRTSSPIEL", win: false);

        // „GAME“ / „DAY“: 54 px, Zeilenhöhe 0,92.
        var big = new Style { FontFamily = Theme.Display, FontSize = 54 * S, TextColor = Theme.White, LetterSpacing = 1 * S };
        var bigCap = CapHeight(big);
        var gameBaseline = GameDayCapTop + bigCap;
        var dayBaseline = gameBaseline + 54 * S * 0.92f;
        DrawLeft(c, "GAME", big, Margin, gameBaseline, Theme.Width - 2 * Margin);
        DrawLeft(c, "DAY", big, Margin, dayBaseline, Theme.Width - 2 * Margin);

        var info = $"{g.Kickoff.ToString("dddd", Theme.German)}, {g.Kickoff.ToString("dd.MM.", Theme.German)} · {g.Kickoff:HH:mm} UHR";
        if (!g.IsHome && !string.IsNullOrWhiteSpace(g.Place)
            && !string.Equals(g.Place.Trim(), homePlace?.Trim(), StringComparison.OrdinalIgnoreCase))
            info += $" · {g.Place.Trim()}";
        DrawLeft(c, info.ToUpper(Theme.German), InfoStyle(), Margin, InfoBaseline, Theme.Width - 2 * Margin, minScale: 0.7f);

        DrawFooter(c, g, clubLogo, opponentLogo);
    }

    public static void DrawResult(SKCanvas c, HerrenResult r, SKBitmap photo, SKBitmap clubLogo, SKBitmap? opponentLogo)
    {
        var g = r.Game;
        var score = g.Result ?? throw new ArgumentException("Herren-Ergebnis ohne Spielstand", nameof(r));
        var win = g.IsWin == true;

        DrawBackground(c, photo);
        DrawTag(c, "ENDSTAND", win);
        DrawScore(c, score);

        if (win)
        {
            var verdict = new Style { FontFamily = Theme.Display, FontSize = 22 * S, TextColor = Theme.White, LetterSpacing = 4 * S };
            DrawCentered(c, "SIEG!", verdict, 0, Theme.Width, VerdictCapTop + CapHeight(verdict), letterSpacingCorrection: true);
        }

        var info = $"{g.Kickoff.ToString("dddd", Theme.German)}, {g.Kickoff.ToString("dd.MM.", Theme.German)} · {g.League}";
        DrawCentered(c, info.ToUpper(Theme.German), InfoStyle(), Margin, Theme.Width - 2 * Margin, InfoBaseline, minScale: 0.7f);

        DrawFooter(c, g, clubLogo, opponentLogo);
    }

    private static Style InfoStyle() => new()
    {
        FontFamily = Theme.Body,
        FontWeight = 800,
        FontSize = 13 * S,
        TextColor = Theme.PaleBlue,
        LetterSpacing = 1 * S,
    };

    private static void DrawBackground(SKCanvas c, SKBitmap photo)
    {
        c.Clear(Theme.Blue);

        using (var clip = Polygon((0, 0), (Theme.Width, 0), (Theme.Width, PhotoBottomRight), (0, PhotoBottomLeft)))
        {
            c.Save();
            c.ClipPath(clip, antialias: true);
            Primitives.DrawCover(c, photo, new SKRect(0, 0, Theme.Width, PhotoBottomLeft));
            c.Restore();
        }

        using var white = new SKPaint { Color = Theme.White, IsAntialias = true };
        using (var stripe = Polygon(
                   (0, PhotoBottomLeft), (Theme.Width, PhotoBottomRight),
                   (Theme.Width, PhotoBottomRight + StripeHeight), (0, PhotoBottomLeft + StripeHeight)))
            c.DrawPath(stripe, white);

        using var light = new SKPaint { Color = Theme.LightBlue, IsAntialias = true };
        using var line = Polygon(
            (0, LineTopLeft), (Theme.Width, LineTopRight),
            (Theme.Width, LineTopRight + LineHeight), (0, LineTopLeft + LineHeight));
        c.DrawPath(line, light);
    }

    private static void DrawTag(SKCanvas c, string text, bool win)
    {
        var style = new Style
        {
            FontFamily = Theme.Display,
            FontSize = 14 * S,
            TextColor = win ? Theme.Blue : Theme.White,
            LetterSpacing = 2 * S,
        };
        var block = TextFit.Line(text, style, Theme.Width - 2 * Margin);
        var cap = CapHeight(style);
        const float padX = 10 * S;
        const float padY = 8 * S;
        var left = 18 * S;
        var top = 18 * S;
        var width = block.MeasuredWidth - style.LetterSpacing + 2 * padX;
        using var fill = new SKPaint { Color = win ? Theme.White : Theme.Blue, IsAntialias = true };
        c.DrawRect(SKRect.Create(left, top, width, cap + 2 * padY), fill);
        PaintAtBaseline(c, block, left + padX, top + padY + cap);
    }

    private static void DrawScore(SKCanvas c, Score score)
    {
        var homeLoses = score.Home < score.Guest;
        var guestLoses = score.Guest < score.Home;
        var size = 76 * S;
        var maxWidth = Theme.Width - 2 * Margin;

        while (true)
        {
            var num = new Style { FontFamily = Theme.Display, FontSize = size };
            var colon = new Style { FontFamily = Theme.Display, FontSize = size * 50 / 76, TextColor = Theme.White.WithAlpha(179) };
            var home = TextFit.Line(score.Home.ToString(), With(num, homeLoses ? Dimmed : Theme.White), Theme.Width);
            var guest = TextFit.Line(score.Guest.ToString(), With(num, guestLoses ? Dimmed : Theme.White), Theme.Width);
            var sep = TextFit.Line(":", colon, Theme.Width);
            var gap = size * 14 / 76;
            var total = home.MeasuredWidth + gap + sep.MeasuredWidth + gap + guest.MeasuredWidth;
            if (total > maxWidth && size > 40 * S)
            {
                size *= maxWidth / total;
                continue;
            }

            var cap = CapHeight(num);
            var baseline = ScoreCapTop + cap;
            var x = (Theme.Width - total) / 2;
            PaintAtBaseline(c, home, x, baseline);
            x += home.MeasuredWidth + gap;
            // Doppelpunkt optisch auf die Ziffernmitte setzen.
            var colonFace = FontMapper.Default.TypefaceFromStyle(colon, false);
            using (var font = new SKFont(colonFace, colon.FontSize))
            {
                font.MeasureText(":", out var bounds);
                var colonBaseline = baseline - cap / 2 - (bounds.Top + bounds.Bottom) / 2;
                PaintAtBaseline(c, sep, x, colonBaseline);
            }
            x += sep.MeasuredWidth + gap;
            PaintAtBaseline(c, guest, x, baseline);
            return;
        }
    }

    private static void DrawFooter(SKCanvas c, Game g, SKBitmap clubLogo, SKBitmap? opponentLogo)
    {
        using var rule = new SKPaint { Color = Theme.White.WithAlpha(64), IsAntialias = true };
        c.DrawRect(SKRect.Create(Margin, FooterRule, Theme.Width - 2 * Margin, 1 * S), rule);

        var leftBox = SKRect.Create(Margin, FooterLogoTop, LogoSize, LogoSize);
        var rightBox = SKRect.Create(Theme.Width - Margin - LogoSize, FooterLogoTop, LogoSize, LogoSize);
        DrawTeamLogo(c, g, g.HomeTeam, clubLogo, opponentLogo, leftBox);
        DrawTeamLogo(c, g, g.GuestTeam, clubLogo, opponentLogo, rightBox);

        var style = new Style { FontFamily = Theme.Display, FontSize = 15 * S, TextColor = Theme.White };
        var textLeft = leftBox.Right + FooterGap;
        var textWidth = rightBox.Left - FooterGap - textLeft;
        var cap = CapHeight(style);
        var pitch = 15 * S * 1.2f;
        var firstBaseline = leftBox.MidY - (pitch + cap) / 2 + cap;
        DrawCentered(c, Label(g, g.HomeTeam), style, textLeft, textWidth, firstBaseline);
        DrawCentered(c, "VS " + Label(g, g.GuestTeam), style, textLeft, textWidth, firstBaseline + pitch);
    }

    private static void DrawTeamLogo(SKCanvas c, Game g, Team team, SKBitmap clubLogo, SKBitmap? opponentLogo, SKRect box)
    {
        var logo = team == g.Us ? clubLogo : opponentLogo;
        if (logo == null)
        {
            Primitives.DrawLogoOrBadge(c, null, team.ShortName, box, Theme.White, Theme.Blue);
            return;
        }

        // Weißer Kreis wie beim Badge, damit auch dunkle Logos auf Blau lesbar bleiben.
        using var disc = new SKPaint { Color = Theme.White, IsAntialias = true };
        c.DrawCircle(box.MidX, box.MidY, Math.Min(box.Width, box.Height) / 2, disc);
        var inset = Math.Min(box.Width, box.Height) * LogoInset;
        Primitives.DrawContained(c, logo, SKRect.Inflate(box, -inset, -inset));
    }

    private static string Label(Game g, Team team) =>
        team == g.Us ? "HERREN" : team.Name.ToUpper(Theme.German);

    private static void DrawLeft(SKCanvas c, string text, Style style, float x, float baseline, float maxWidth, float minScale = 1f)
    {
        var block = Fit(text, style, maxWidth, minScale, centered: false);
        PaintAtBaseline(c, block, x, baseline);
    }

    private static void DrawCentered(
        SKCanvas c, string text, Style style, float left, float width, float baseline,
        float minScale = 0.75f, bool letterSpacingCorrection = false)
    {
        var block = Fit(text, style, width, minScale, centered: true);
        // Laufweite steht auch hinter dem letzten Zeichen; für echte Zentrierung halb zurücknehmen.
        var shift = letterSpacingCorrection ? style.LetterSpacing / 2 : 0;
        PaintAtBaseline(c, block, left + shift, baseline);
    }

    /// <summary>Verkleinert die Schrift bis <paramref name="minScale"/>, erst danach kürzt TextFit mit „…“.</summary>
    private static TextBlock Fit(string text, Style style, float maxWidth, float minScale, bool centered)
    {
        // Breite wächst linear mit Schriftgröße und Laufweite, also reicht eine Messung ohne Begrenzung.
        var natural = TextFit.Line(text, style, Unlimited).MeasuredWidth;
        var scale = natural > maxWidth ? Math.Max(minScale, maxWidth / natural * 0.99f) : 1f;
        var s = With(style, style.TextColor);
        s.FontSize = style.FontSize * scale;
        s.LetterSpacing = style.LetterSpacing * scale;
        return centered ? TextFit.Centered(text, s, maxWidth) : TextFit.Line(text, s, maxWidth);
    }

    private static Style With(Style s, SKColor color) => new()
    {
        FontFamily = s.FontFamily,
        FontWeight = s.FontWeight,
        FontSize = s.FontSize,
        LetterSpacing = s.LetterSpacing,
        TextColor = color,
    };

    private static void PaintAtBaseline(SKCanvas c, TextBlock block, float x, float baseline) =>
        block.Paint(c, new SKPoint(x, baseline - block.Lines[0].BaseLine));

    private static float CapHeight(Style style)
    {
        var face = FontMapper.Default.TypefaceFromStyle(style, false);
        using var font = new SKFont(face, style.FontSize);
        return font.Metrics.CapHeight;
    }

    private static SKPath Polygon(params (float X, float Y)[] points)
    {
        var path = new SKPath();
        path.MoveTo(points[0].X, points[0].Y);
        foreach (var (x, y) in points.Skip(1)) path.LineTo(x, y);
        path.Close();
        return path;
    }
}
