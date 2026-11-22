using EventToInsta.Planning;

namespace EventToInsta.Tests.Planning;

public class ChunkerTests
{
    private static int[] Sizes(int n, int max) =>
        Chunker.SplitEvenly(Enumerable.Range(1, n).ToList(), max).Select(p => p.Count).ToArray();

    [Fact] public void Seven_Max6_FourAndThree() => Assert.Equal(new[] { 4, 3 }, Sizes(7, 6));
    [Fact] public void Six_Max6_Single() => Assert.Equal(new[] { 6 }, Sizes(6, 6));
    [Fact] public void Thirteen_Max6_FiveFourFour() => Assert.Equal(new[] { 5, 4, 4 }, Sizes(13, 6));
    [Fact] public void Zero_Empty() => Assert.Empty(Sizes(0, 6));

    [Fact]
    public void PreservesOrder()
    {
        var parts = Chunker.SplitEvenly(Enumerable.Range(1, 7).ToList(), 6);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, parts.SelectMany(p => p).ToArray());
    }
}
