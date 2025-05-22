using System.Collections.Generic;

namespace CliCommandLine;

internal record ParsedOption(int Index, bool IsShortOption, int OptionIndex, List<string>? Params);
