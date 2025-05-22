namespace CliOption;
public record Option(char? ShortOption, string? LongOption, int MaxOccurs, string? Group, string? Description, List<ParameterSpec> Parameters)
{
    public int ParameterCount => Parameters?.Count ?? 0;
}
