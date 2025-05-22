using CliOption;
using System.Text.RegularExpressions;

namespace CliHelpSystem;

public class HelpTextParser
{
    private static readonly Regex HeaderRegex = new(@"^(=+)\s*(.+)$");

    private static readonly Regex OptionLineRegex = new(
        """
            ^\s*                            # Leading whitespace
            (?:-(\w),?\s*)?                 # Optional short option (e.g. -f or -f,)
            (--[\w-]+)?                     # Optional long option (e.g. --file)
            (?:\s+(<[^>]+>|\[[^\]]+\]))?    # Optional parameter 1
            (?:\s+(<[^>]+>|\[[^\]]+\]))?    # Optional parameter 2
            (?:\s+(<[^>]+>|\[[^\]]+\]))?    # Optional parameter 3
            .*                              # Remainder (description)
        """,
        RegexOptions.IgnorePatternWhitespace
    );

    public static void DumpAllHelp(List<HelpSection> sections, string outputPath)
    {
        using var writer = new StreamWriter(outputPath);

        foreach (var section in sections)
        {
            writer.WriteLine($"== {section.CommandPath}");
            writer.WriteLine(section.HelpText.Trim());

            if (section.Options.Count > 0)
            {
                writer.WriteLine();
                writer.WriteLine("Options:");
                foreach (var opt in section.Options)
                {
                    var names = new List<string>();
                    if (opt.ShortOption != null) names.Add("-" + opt.ShortOption);
                    if (opt.LongOption != null) names.Add(opt.LongOption);

                    foreach (var p in opt.Parameters)
                    {
                        names.Add("<" + p.Name + (p.Type != null ? ":" + p.Type : "") + ">");
                    }

                    writer.WriteLine($"  {string.Join(" ", names),-20} {opt.Description}");
                }
            }

            writer.WriteLine(new string('-', 40));
        }

        Console.WriteLine($"Help dump written to {outputPath}");
    }

    public static List<Option> GetOptions(List<HelpSection> sections, string commandPath)
    {
        return sections
            .FirstOrDefault(s => s.CommandPath.Equals(commandPath, StringComparison.OrdinalIgnoreCase))
            ?.Options ?? [];
    }

    public static List<HelpSection> Parse(string rawText, string toolName)
    {
        bool foundFirstHeader = false;
        var definedGroups = new Dictionary<string, List<Option>>();
        var lines = rawText.Replace("\r", "").Split('\n');
        List<HelpSection> sections = [];

        HelpSection? currentSection = null;
        string? currentCommand = null;
        bool inOptions = false;
        string? currentGroup = null;
        OptionBuilder? currentBuilder = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (!foundFirstHeader)
            {
                if (HeaderRegex.IsMatch(line))
                {
                    foundFirstHeader = true;
                }
                else
                {
                    if (line.StartsWith("@group "))
                    {
                        currentGroup = line[7..].Trim();
                        definedGroups[currentGroup] = [];
                        continue;
                    }

                    if (currentGroup != null && OptionLineRegex.IsMatch(line))
                    {
                        definedGroups[currentGroup].Add(ParseOptionLine(line, "@group"));
                    }
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(line)) continue;

            var headerMatch = HeaderRegex.Match(line);
            if (headerMatch.Success)
            {
                int level = headerMatch.Groups[1].Value.Length;
                var headerContent = headerMatch.Groups[2].Value.Trim();
                var name = headerContent;
                var summary = "";
                var dashIndex = headerContent.IndexOf(" - ");
                if (dashIndex >= 0)
                {
                    name = headerContent.Substring(0, dashIndex).Trim();
                    summary = headerContent[(dashIndex + 3)..].Trim();
                }

                if (level == 1)
                {
                    if (string.Equals(name, toolName, StringComparison.OrdinalIgnoreCase))
                    {
                        currentCommand = "";
                        currentSection = new HelpSection { CommandPath = "", CommandSummary = summary };
                        sections.Add(currentSection);
                    }
                    else
                    {
                        currentCommand = null;
                        currentSection = null;
                    }
                    continue;
                }

                if (currentCommand != null)
                {
                    var fullCommand = name;
                    if (level > 2 && currentSection != null)
                        fullCommand = currentSection.CommandPath + " " + name;

                    currentSection = new HelpSection { CommandPath = fullCommand, CommandSummary = summary };
                    sections.Add(currentSection);
                }

                inOptions = false;
                continue;
            }

            if (currentSection == null) continue;

            if (line.Trim().Equals("Options:", StringComparison.OrdinalIgnoreCase))
            {
                inOptions = true;
                continue;
            }

            if (inOptions)
            {
                if (line.Trim().StartsWith("@include "))
                {
                    var groupName = line.Trim().Substring(9).Trim();
                    if (definedGroups.TryGetValue(groupName, out var groupOptions))
                    {
                        foreach (var opt in groupOptions)
                        {
                            var copied = new OptionBuilder()
                                .WithShortOption(opt.ShortOption)
                                .WithLongOption(opt.LongOption)
                                .WithGroup(currentSection.CommandPath)
                                .WithDescription(opt.Description);

                            foreach (var param in opt.Parameters)
                                copied.WithParameter(param.Name, param.Type);

                            currentSection.Options.Add(copied.Build());
                        }
                    }
                    continue;
                }

                if (OptionLineRegex.IsMatch(line))
                {
                    var option = ParseOptionLine(line, currentSection.CommandPath);
                    currentSection.Options.Add(option);
                    currentBuilder = new OptionBuilder()
                        .WithShortOption(option.ShortOption)
                        .WithLongOption(option.LongOption)
                        .WithGroup(option.Group)
                        .WithDescription(option.Description);

                    foreach (var param in option.Parameters)
                        currentBuilder.WithParameter(param.Name, param.Type);
                }
                else if (currentBuilder != null && line.StartsWith(" "))
                {
                    var last = currentSection.Options.Last();
                    currentSection.Options[^1] = last with { Description = last.Description + Environment.NewLine + line.Trim() };
                }
            }
            else if (line.TrimStart().StartsWith("#"))
            {
                continue; // skip comment lines
            }
            else
            {
                var cleanLine = line.StartsWith("\\#") ? line.Substring(1) : line;
                currentSection.HelpText += FormatAsciiDoc(cleanLine) + "\n";
            }
        }

        return sections;
    }

    public static void PrintHelp(List<HelpSection> sections, string commandPath)
    {
        var section = sections.FirstOrDefault(s => s.CommandPath.Equals(commandPath, StringComparison.OrdinalIgnoreCase));
        if (section == null)
        {
            Console.WriteLine($"No help found for '{commandPath}'");
            return;
        }

        var helpText = section.HelpText;
        if (section.CommandPath == "" && helpText.Contains("$(subcommands)"))
        {
            var children = sections
                .Where(s => s.CommandPath.Split(' ').Length == 1 && s.CommandPath != "")
                .Select(s => $"  {s.CommandPath,-12} {s.CommandSummary}");
            var subList = string.Join(Environment.NewLine, children);
            helpText = helpText.Replace("$(subcommands)", subList);
        }
        Console.WriteLine(helpText.Trim());

        if (section.Options.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Options:");
            foreach (var opt in section.Options)
            {
                List<string> names = [];
                if (opt.ShortOption != null && opt.LongOption != null)
                    names.Add($"-{opt.ShortOption}, --{opt.LongOption}");
                else if (opt.ShortOption != null)
                    names.Add($"-{opt.ShortOption}");
                else if (opt.LongOption != null)
                    names.Add($"--{opt.LongOption}");

                foreach (var p in opt.Parameters)
                {
                    names.Add("<" + p.Name + (p.Type != null ? ":" + p.Type : "") + ">");
                }

                Console.WriteLine($"  {string.Join(" ", names),-20} {FormatAsciiDoc(opt.Description!)}");
            }
        }
    }
    private static string FormatAsciiDoc(string input)
    {
        input = Regex.Replace(input, @"\[(\w+)\]#(.*?)#", m =>
        {
            string color = m.Groups[1].Value.ToLower();
            string text = m.Groups[2].Value;
            string code = color switch
            {
                "red" => "\u001b[31m",
                "green" => "\u001b[32m",
                "yellow" => "\u001b[33m",
                "blue" => "\u001b[34m",
                "magenta" => "\u001b[35m",
                "cyan" => "\u001b[36m",
                "white" => "\u001b[37m",
                _ => ""
            };
            return code + text + "\u001b[0m";
        });

        input = Regex.Replace(input, "\\*\\*(.+?)\\*\\*", "\u001b[1m$1\u001b[22m");  // bold
        input = Regex.Replace(input, "_(.+?)_", "\u001b[4m$1\u001b[24m");              // underline
        input = Regex.Replace(input, "\\*(.+?)\\*", "\u001b[3m$1\u001b[23m");          // italic

        return input;
    }

    private static Option ParseOptionLine(string line, string group)
    {
        var match = OptionLineRegex.Match(line);
        var shortName = match.Groups[1].Success ? match.Groups[1].Value[0] : (char?)null;
        var longName = match.Groups[2].Success ? match.Groups[2].Value.Substring(2) : null;

        var parameters = new List<(string Name, string? Type)>();
        for (int i = 3; i < match.Groups.Count; i++)
        {
            var g = match.Groups[i];
            if (g.Success)
            {
                var paramMatch = Regex.Match(g.Value, "<(?<name>[^:>]+)(:(?<type>[^>]+))?>");
                var name = paramMatch.Success ? paramMatch.Groups["name"].Value : g.Value;
                var type = paramMatch.Groups["type"].Success ? paramMatch.Groups["type"].Value : null;
                parameters.Add((name, type));
            }
        }

        var key = match.Groups.Cast<Group>().LastOrDefault(g => g.Success)?.Value ?? longName ?? shortName?.ToString() ?? "";
        var descStart = line.IndexOf(key);
        var desc = descStart >= 0 ? line[(descStart + key.Length)..].Trim() : "";

        var builder = new OptionBuilder()
            .WithShortOption(shortName)
            .WithLongOption(longName)
            .WithGroup(group)
            .WithDescription(desc);

        foreach (var (name, type) in parameters)
            builder.WithParameter(name, type);

        return builder.Build();
    }
}