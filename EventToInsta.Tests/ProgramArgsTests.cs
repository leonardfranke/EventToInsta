namespace EventToInsta.Tests;

[Collection(ConsoleCollection.Name)]
public class ProgramArgsTests
{
    [Fact]
    public void Parse_PreviewDryRunSampleNow()
    {
        var opt = RunOptions.Parse(["preview", "--dry-run", "--sample", "--now", "2026-11-23T08:30"]);

        Assert.Equal("preview", opt.Job);
        Assert.True(opt.DryRun);
        Assert.True(opt.Sample);
        Assert.Equal(new DateTime(2026, 11, 23, 8, 30, 0), opt.Now);
        Assert.Equal(DateTimeKind.Unspecified, opt.Now!.Value.Kind);
    }

    [Fact]
    public void Parse_ResultsWithoutFlags()
    {
        var opt = RunOptions.Parse(["results"]);

        Assert.Equal(new RunOptions("results", false, false, null), opt);
    }

    [Theory]
    [InlineData("posten")]
    [InlineData("--dry-run")]
    public void Parse_UnknownJob_Throws(string job)
    {
        var e = Assert.Throws<ArgumentException>(() => RunOptions.Parse([job]));
        Assert.Contains("preview", e.Message);
        Assert.Contains("results", e.Message);
    }

    [Fact]
    public void Parse_NoArgs_Throws() =>
        Assert.Throws<ArgumentException>(() => RunOptions.Parse([]));

    [Theory]
    [InlineData("--now")]
    [InlineData("--now", "23.11.2026")]
    [InlineData("--unbekannt")]
    [InlineData("results")]
    [InlineData("--sample")]
    public void Parse_InvalidOptions_Throw(params string[] rest) =>
        Assert.Throws<ArgumentException>(() => RunOptions.Parse(["preview", .. rest]));

    [Fact]
    public async Task Main_BadArgs_ReturnsOne()
    {
        var originalError = Console.Error;
        using var error = new StringWriter();
        Console.SetError(error);
        try
        {
            Assert.Equal(1, await Program.Main(["posten"]));
        }
        finally
        {
            Console.SetError(originalError);
        }
        Assert.Contains("preview", error.ToString());
    }
}
