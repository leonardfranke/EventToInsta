namespace EventToInsta.Tests;

public class ProgramHostTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("EventToInsta.CloudFunction", true)]
    public void RunsAsFunction_DependsOnFunctionTarget(string? target, bool expected)
    {
        var env = new Dictionary<string, string?> { ["FUNCTION_TARGET"] = target };

        Assert.Equal(expected, Program.RunsAsFunction(name => env.GetValueOrDefault(name)));
    }
}
