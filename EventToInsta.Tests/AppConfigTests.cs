namespace EventToInsta.Tests;

public class AppConfigTests
{
    private static Func<string, string?> Env(params (string Key, string Value)[] vars) =>
        key => vars.FirstOrDefault(v => v.Key == key).Value;

    [Fact]
    public void FromEnvironment_Defaults()
    {
        var cfg = AppConfig.FromEnvironment(Env());

        Assert.Equal(621, cfg.ClubId);
        Assert.Null(cfg.UploadPostApiKey);
        Assert.Equal("Dachse", cfg.UploadPostUser);
        Assert.Null(cfg.StateBucket);
        Assert.Equal("Mann.png", cfg.Sponsors["U14"]);
        Assert.Equal("Böhnke.png", cfg.Sponsors["U12"]);
        Assert.Equal("Stockmeyer.webp", cfg.Sponsors["U16"]);
        Assert.Equal("Mittelstedt.png", cfg.Sponsors["U18"]);
        Assert.Equal(4, cfg.Sponsors.Count);
    }

    [Fact]
    public void FromEnvironment_ReadsAllVariables()
    {
        var cfg = AppConfig.FromEnvironment(Env(
            ("CLUB_ID", "42"), ("UPLOAD_POST_API_KEY", "secret"),
            ("UPLOAD_POST_USER", "Andere"), ("STATE_BUCKET", "bucket")));

        Assert.Equal(42, cfg.ClubId);
        Assert.Equal("secret", cfg.UploadPostApiKey);
        Assert.Equal("Andere", cfg.UploadPostUser);
        Assert.Equal("bucket", cfg.StateBucket);
    }

    [Fact]
    public void FromEnvironment_EmptyValuesCountAsUnset()
    {
        var cfg = AppConfig.FromEnvironment(Env(("UPLOAD_POST_API_KEY", " "), ("STATE_BUCKET", ""), ("CLUB_ID", "")));

        Assert.Null(cfg.UploadPostApiKey);
        Assert.Null(cfg.StateBucket);
        Assert.Equal(621, cfg.ClubId);
    }

    [Fact]
    public void FromEnvironment_SponsorsOverride()
    {
        var cfg = AppConfig.FromEnvironment(Env(("SPONSORS", " U10 = Hilgo.png ; U14=Neu.png ")));

        Assert.Equal(2, cfg.Sponsors.Count);
        Assert.Equal("Hilgo.png", cfg.Sponsors["U10"]);
        Assert.Equal("Neu.png", cfg.Sponsors["U14"]);
        Assert.False(cfg.Sponsors.ContainsKey("U12"));
    }

    [Fact]
    public void FromEnvironment_InvalidSponsorEntry_Throws()
    {
        var e = Assert.Throws<ArgumentException>(() => AppConfig.FromEnvironment(Env(("SPONSORS", "U12=Böhnke.png;Kaputt"))));
        Assert.Contains("SPONSORS", e.Message);
    }

    [Fact]
    public void FromEnvironment_InvalidClubId_Throws()
    {
        var e = Assert.Throws<ArgumentException>(() => AppConfig.FromEnvironment(Env(("CLUB_ID", "abc"))));
        Assert.Contains("CLUB_ID", e.Message);
    }
}
