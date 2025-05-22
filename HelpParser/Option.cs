namespace HelpParser;
public record Option(char? ShortOption, string? LongOption, int MaxOccurs, int NumberOfParams, string? Group, string? Description, List<ParameterSpec> Parameters);
