using EventToInsta.Rendering;
using Topten.RichTextKit;

namespace EventToInsta.Tests.Rendering;

public class TextFitTests
{
    public static string FontsDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");

    public TextFitTests() => Fonts.Register(FontsDirectory);

    private static Style StyleOf(float size) =>
        new() { FontFamily = Theme.Display, FontSize = size };

    [Fact]
    public void Fonts_Register_AntonResolvesToBundledTypeface()
    {
        var face = FontMapper.Default.TypefaceFromStyle(StyleOf(40), false);
        Assert.Equal("Anton", face.FamilyName);
    }

    [Fact]
    public void Fonts_Register_InterWeightsMapToNearestCut()
    {
        int Weight(int w) => FontMapper.Default.TypefaceFromStyle(
            new Style { FontFamily = Theme.Body, FontWeight = w }, false).FontWeight;
        Assert.Equal(500, Weight(500));
        Assert.Equal(700, Weight(700));
        Assert.Equal(800, Weight(800));
        Assert.Equal(700, Weight(650));
    }

    [Fact]
    public void Fonts_UnknownFamily_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FontMapper.Default.TypefaceFromStyle(new Style { FontFamily = "Arial" }, false));
    }

    [Fact]
    public void TextFit_LongOpponent_FitsWidthAndEndsWithEllipsis()
    {
        var block = TextFit.Line("Turn- und Sportverein Grün-Weiß Osnabrück-Haste II", StyleOf(56), 600);

        Assert.True(block.MeasuredWidth <= 600, $"MeasuredWidth {block.MeasuredWidth}");
        Assert.Single(block.Lines);
        Assert.Equal(FontRunKind.Ellipsis, block.Lines[0].Runs.Last().RunKind);
    }

    [Fact]
    public void TextFit_ShortText_Unchanged()
    {
        var block = TextFit.Line("Dachse", StyleOf(56), 600);

        Assert.True(block.MeasuredWidth <= 600);
        Assert.DoesNotContain(block.Lines[0].Runs, r => r.RunKind == FontRunKind.Ellipsis);
        Assert.Single(block.Lines);
    }
}
