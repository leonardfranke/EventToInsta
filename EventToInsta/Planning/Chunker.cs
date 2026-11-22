namespace EventToInsta.Planning;

public static class Chunker
{
    /// <summary>Teilt in ceil(n/max) Teile, deren Groessen sich hoechstens um 1 unterscheiden (vordere sind groesser).</summary>
    public static IReadOnlyList<IReadOnlyList<T>> SplitEvenly<T>(IReadOnlyList<T> items, int maxPerPart)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPerPart, 1);
        var parts = (items.Count + maxPerPart - 1) / maxPerPart;
        var result = new List<IReadOnlyList<T>>(parts);
        var index = 0;
        for (var i = 0; i < parts; i++)
        {
            var size = items.Count / parts + (i < items.Count % parts ? 1 : 0);
            result.Add(items.Skip(index).Take(size).ToList());
            index += size;
        }
        return result;
    }
}
