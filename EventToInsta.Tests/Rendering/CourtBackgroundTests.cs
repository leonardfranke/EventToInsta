using EventToInsta.Rendering;
using SkiaSharp;

namespace EventToInsta.Tests.Rendering;

[Collection("Fonts")]
public class CourtBackgroundTests
{
    private static SKBitmap Render()
    {
        var bmp = new SKBitmap(Theme.Width, Theme.Height);
        using var c = new SKCanvas(bmp);
        CourtBackground.Draw(c, SKRect.Create(0, 0, Theme.Width, Theme.Height));
        return bmp;
    }

    [Fact]
    public void Draw_IsDeterministic()
    {
        using var a = Render();
        using var b = Render();

        Assert.Equal(a.Bytes, b.Bytes);
    }

    [Fact]
    public void Draw_CentreIsBrighterAndBluerThanCorner()
    {
        using var bmp = Render();
        // Mitte des Lichtkegels (50 % / 38 %) liegt neben dem Mittelkreis, nicht auf einer Linie.
        var centre = bmp.GetPixel(Theme.Width / 2 + 250, (int)(Theme.Height * 0.38f));
        var corner = bmp.GetPixel(10, Theme.Height - 10);

        Assert.True(centre.Blue > corner.Blue + 60, $"Mitte {centre}, Ecke {corner}");
        Assert.True(centre.Red + centre.Green + centre.Blue > corner.Red + corner.Green + corner.Blue + 80,
            $"Mitte {centre}, Ecke {corner}");
    }

    [Fact]
    public void Draw_HalfCourtLineIsLighterThanNeighbour()
    {
        using var bmp = Render();
        // Mittellinie bei y = 266 × 3,6 ≈ 958, links vom Mittelkreis.
        var line = bmp.GetPixel(300, 958);
        var beside = bmp.GetPixel(300, 980);

        Assert.True(line.Red > beside.Red + 15 && line.Green > beside.Green + 15,
            $"Linie {line}, daneben {beside}");
    }
}
