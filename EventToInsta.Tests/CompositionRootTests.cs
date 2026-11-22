namespace EventToInsta.Tests;

public class CompositionRootTests
{
    private static AppConfig Config(params (string Key, string Value)[] vars) =>
        AppConfig.FromEnvironment(key => vars.FirstOrDefault(v => v.Key == key).Value);

    [Fact]
    public void Build_WithoutApiKeyAndWithoutDryRun_ThrowsClearError()
    {
        var e = Assert.Throws<InvalidOperationException>(() =>
            CompositionRoot.Build(Config(), new RunOptions("preview", DryRun: false, Sample: false, Now: null), _ => { }));

        Assert.Contains("UPLOAD_POST_API_KEY", e.Message);
    }

    [Fact]
    public void Build_SampleWithoutDryRun_Throws()
    {
        var e = Assert.Throws<ArgumentException>(() =>
            CompositionRoot.Build(Config(("UPLOAD_POST_API_KEY", "key")),
                new RunOptions("preview", DryRun: false, Sample: true, Now: null), _ => { }));

        Assert.Contains("--sample", e.Message);
    }

    [Fact]
    public void Build_RequireStateBucketWithoutBucket_Throws()
    {
        var e = Assert.Throws<InvalidOperationException>(() =>
            CompositionRoot.Build(Config(("UPLOAD_POST_API_KEY", "key")),
                new RunOptions("results", DryRun: false, Sample: false, Now: null), _ => { }, requireStateBucket: true));

        Assert.Contains("STATE_BUCKET ist nicht gesetzt", e.Message);
    }
}
