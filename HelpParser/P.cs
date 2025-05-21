#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class HelpSection
{
    public string CommandPath { get; set; } = ""; // e.g. "remote add"
    public string HelpText { get; set; } = "";
    public List<ParsedOption> Options { get; set; } = new();
    public string OptionGroup => CommandPath; // using path string as grouping key
}

public class ParsedOption
{
    public char? ShortName { get; set; }
    public string? LongName { get; set; }
    public List<string> Parameters { get; set; } = new();
    public bool IsVariadic => Parameters.Any(p => p.EndsWith("..."));
    public string Description { get; set; } = "";
    public string OptionGroup { get; set; } = ""; // name of the group/command this belongs to
}

public class HelpTextParser
{
    private static readonly Regex HeaderRegex = new(@"^(=+)\s*(.+)$");
    private static readonly Regex OptionLineRegex = new(
        @"^\s*(?:-(\w),?\s*)?(--[\w-]+)?(?:\s+(<[^>]+>|\[[^\]]+\]))?(?:\s+(<[^>]+>|\[[^\]]+\]))?(?:\s+(<[^>]+>|\[[^\]]+\]))?.*"
    );

    public List<HelpSection> Parse(string rawText, string toolName)
    {
        var lines = rawText.Replace("\r", "").Split('\n');
        var sections = new List<HelpSection>();

        HelpSection? currentSection = null;
        string? currentCommand = null;
        bool inOptions = false;
        ParsedOption? currentOption = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var headerMatch = HeaderRegex.Match(line);
            if (headerMatch.Success)
            {
                int level = headerMatch.Groups[1].Value.Length;
                var name = headerMatch.Groups[2].Value.Trim();

                if (level == 1)
                {
                    currentCommand = name == toolName ? "" : null;
                    continue;
                }

                if (currentCommand != null)
                {
                    var fullCommand = name;
                    if (level > 2 && currentSection != null)
                        fullCommand = currentSection.CommandPath + " " + name;

                    currentSection = new HelpSection { CommandPath = fullCommand };
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
                if (OptionLineRegex.IsMatch(line))
                {
                    var match = OptionLineRegex.Match(line);
                    var shortName = match.Groups[1].Success ? match.Groups[1].Value[0] : (char?)null;
                    var longName = match.Groups[2].Success ? match.Groups[2].Value : null;

                    var parameters = new List<string>();
                    for (int i = 3; i < match.Groups.Count; i++)
                    {
                        var g = match.Groups[i];
                        if (g.Success)
                            parameters.Add(g.Value);
                    }

                    var key = parameters.LastOrDefault() ?? longName ?? (shortName != null ? "-" + shortName : "");
                    var descStart = line.IndexOf(key);
                    var desc = descStart >= 0 ? line[(descStart + (parameters.LastOrDefault()?.Length ?? 0))..].Trim() : "";

                    currentOption = new ParsedOption
                    {
                        ShortName = shortName,
                        LongName = longName,
                        Parameters = parameters,
                        Description = desc,
                        OptionGroup = currentSection.CommandPath
                    };
                    currentSection.Options.Add(currentOption);
                }
                else if (currentOption != null && line.StartsWith(" "))
                {
                    currentOption.Description += "\n" + line.Trim();
                }
            }
            else
            {
                currentSection.HelpText += FormatAsciiDoc(line) + "\n";
            }
        }

        return sections;
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

    public static void PrintHelp(List<HelpSection> sections, string commandPath)
    {
        var section = sections.FirstOrDefault(s => s.CommandPath.Equals(commandPath, StringComparison.OrdinalIgnoreCase));
        if (section == null)
        {
            Console.WriteLine($"No help found for '{commandPath}'");
            return;
        }

        Console.WriteLine(section.HelpText.Trim());
        if (section.Options.Count > 0)
        {
            Console.WriteLine("\nOptions:");
            foreach (var opt in section.Options)
            {
                var names = new List<string>();
                if (opt.ShortName != null) names.Add("-" + opt.ShortName);
                if (opt.LongName != null) names.Add(opt.LongName);
                names.AddRange(opt.Parameters);

                Console.WriteLine($"  {string.Join(" ", names),-20} {FormatAsciiDoc(opt.Description)}");
            }
        }
    }

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
                    if (opt.ShortName != null) names.Add("-" + opt.ShortName);
                    if (opt.LongName != null) names.Add(opt.LongName);
                    names.AddRange(opt.Parameters);

                    writer.WriteLine($"  {string.Join(" ", names),-20} {opt.Description}");
                }
            }

            writer.WriteLine(new string('-', 40));
        }

        Console.WriteLine($"Help dump written to {outputPath}");
    }

    public static List<Option> BuildOptionsFromHelp(HelpSection section)
    {
        return section.Options.Select(po => new OptionBuilder()
            .WithShortOption(po.ShortName)
            .WithLongOption(po.LongName)
            .WithGroup(po.OptionGroup)
            .WithNumberOfParams(po.Parameters.Count)
            .Build()).ToList();
    }
}
