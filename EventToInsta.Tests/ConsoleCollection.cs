namespace EventToInsta.Tests;

/// <summary>Tests, die Console.Error umleiten oder darauf schreiben, laufen nicht parallel zueinander.</summary>
[CollectionDefinition(Name)]
public sealed class ConsoleCollection
{
    public const string Name = "Console";
}
