using System.Globalization;
using SkiaSharp;

namespace EventToInsta.Rendering;

public static class Theme
{
    public static readonly SKColor Blue = SKColor.Parse("#0B4EA2");
    public static readonly SKColor White = SKColors.White;
    public static readonly SKColor LightBlue = SKColor.Parse("#6FA8F0");
    public static readonly SKColor PaleBlue = SKColor.Parse("#A9CBF7");
    public static readonly SKColor Grey = SKColor.Parse("#B8C4D6");

    // Jugend-Infokarte: neutrale Grautöne auf den weißen Karten.
    public static readonly SKColor MutedText = SKColor.Parse("#555555");
    public static readonly SKColor FaintText = SKColor.Parse("#888888");
    public static readonly SKColor LossText = SKColor.Parse("#8090A8");

    public const int Width = 1080;
    public const int Height = 1920;
    public const int JpegQuality = 95;

    public const string Display = "Anton";
    public const string Body = "Inter";

    public static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
}
