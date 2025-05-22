namespace CliCommandLine;

public record ParsedOptionsResult(
    Dictionary<int, List<ParsedOption>> Parsed,
    List<IllegalOption> Illegal,
    List<NonOption> NonOption,
    IDictionary<string, int> LongNameToIndex,
    IDictionary<char, int> ShortNameToIndex
);