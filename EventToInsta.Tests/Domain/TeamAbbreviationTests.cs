using EventToInsta.Domain;

namespace EventToInsta.Tests.Domain;

public class TeamAbbreviationTests
{
    private static string For(string name, string shortName) => TeamAbbreviation.For(new Team(1, name, shortName, 9));

    [Theory]
    [InlineData("SC Melle II", "SCM2", "SCM2")]
    [InlineData("SV Bad Laer", "SVBL", "SVBL")]
    [InlineData("Osnabrücker SC", "OSC", "OSC")]
    [InlineData("Irgendwer", "ABCDEF", "ABCDEF")]
    public void ShortShortName_UsedAsIs(string name, string shortName, string expected) =>
        Assert.Equal(expected, For(name, shortName));

    [Fact]
    public void LongShortName_FallsBackToInitialsOfName() =>
        Assert.Equal("TSGWO", For("Turn- und Sportverein Grün-Weiß Osnabrück-Haste II", "Osnabrück-Haste"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyShortName_UsesInitials(string shortName) =>
        Assert.Equal("BO", For("Basketball Osnabrück", shortName));

    [Fact]
    public void TrailingNumeral_KeptWhenItFits() =>
        Assert.Equal("TBLII", For("TV Bad Laer II", ""));

    [Fact]
    public void TrailingNumeral_DroppedWhenTooLong() =>
        Assert.Equal("TSGWO", For("Turn- und Sportverein Grün-Weiß Osnabrück-Haste II", ""));

    [Fact]
    public void FillerWords_AreSkipped() =>
        Assert.Equal("SOLNB", For("Spielgemeinschaft Osnabrücker Land Nordwest Basketball e. V.", ""));

    [Fact]
    public void Umlauts_ArePreserved() =>
        Assert.Equal("ÖB", For("Östliche Basketballer", ""));
}
