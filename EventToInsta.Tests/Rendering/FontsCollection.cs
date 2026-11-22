namespace EventToInsta.Tests.Rendering;

/// <summary>Tests, die den prozessweiten FontMapper nutzen oder ändern, laufen nie parallel.</summary>
[CollectionDefinition("Fonts", DisableParallelization = true)]
public sealed class FontsCollection;
