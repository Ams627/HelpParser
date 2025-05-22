namespace CliCommandLine;

public record ParsedOption(int Index, bool IsShortOption, int OptionIndex, List<string>? Params);
