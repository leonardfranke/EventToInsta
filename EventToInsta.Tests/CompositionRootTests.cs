namespace EventToInsta.Tests;

public class CompositionRootTests
{
    [Fact]
    public void Build_WithoutApiKeyAndWithoutDryRun_ThrowsClearError()
    {
        var cfg = AppConfig.FromEnvironment(_ => null);

        var e = Assert.Throws<InvalidOperationException>(() =>
            CompositionRoot.Build(cfg, new RunOptions("preview", DryRun: false, Sample: true, Now: null), _ => { }));

        Assert.Contains("UPLOAD_POST_API_KEY", e.Message);
    }
}
