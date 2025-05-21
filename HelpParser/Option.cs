#nullable enable

public record Option
{
    public char? ShortOption { get; }
    public string? LongOption { get; }
    public int MaxOccurs { get; }
    public int NumberOfParams { get; }
    public string? Group { get; }

    // internal constructor so we can use a builder
    internal Option(char? shortOption, string? longOption, int maxOccurs, int numberOfParams, string? group)
    {
        ShortOption = shortOption;
        LongOption = longOption;
        MaxOccurs = maxOccurs;
        NumberOfParams = numberOfParams;
        Group = group;
    }
}