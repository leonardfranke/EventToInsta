using EventToInsta.Rendering;
using SkiaSharp;

namespace EventToInsta.Tests.Rendering;

public class PrimitivesTests
{
    [Fact]
    public void DrawCover_LandscapePhoto_FillsTargetWithoutDistortion()
    {
        using var photo = new SKBitmap(2000, 1000);
        using (var pc = new SKCanvas(photo))
        {
            pc.Clear(SKColors.Red);
            using var green = new SKPaint { Color = SKColors.Green };
            pc.DrawRect(1000, 0, 1000, 1000, green);
        }

        using var target = new SKBitmap(1080, 1920);
        using (var c = new SKCanvas(target))
            Primitives.DrawCover(c, photo, new SKRect(0, 0, 1080, 1920));

        Assert.NotEqual(0, target.GetPixel(0, 960).Alpha);
        Assert.NotEqual(0, target.GetPixel(1079, 960).Alpha);
        Assert.NotEqual(target.GetPixel(538, 960), target.GetPixel(542, 960));
        Assert.True(target.GetPixel(100, 960).Red > 200);
        Assert.True(target.GetPixel(1000, 960).Green > 100);
    }

    [Fact]
    public void DrawContained_KeepsAspectRatioInsideBox()
    {
        using var logo = new SKBitmap(200, 100);
        using (var lc = new SKCanvas(logo)) lc.Clear(SKColors.Red);

        using var target = new SKBitmap(400, 400);
        using (var c = new SKCanvas(target))
            Primitives.DrawContained(c, logo, new SKRect(0, 0, 400, 400));

        Assert.NotEqual(0, target.GetPixel(200, 200).Alpha);
        Assert.Equal(0, target.GetPixel(200, 10).Alpha);
        Assert.Equal(0, target.GetPixel(200, 390).Alpha);
    }
}
