﻿/*
namespace HelpParser;
class Program
{
    private static void Main(string[] args)
    {
        try
        {
            var helpText = """
                = HelpParser

                == use

                **Run the use command.**

                Options:
                    -s, --something <thing>

                == buy
                Buy something on *amazon.co.uk* but only if [green]#you# want.

                Options:
                    -t, --thing <thing> - specify the thing to buy
                """;

            string exeName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            var parser = new HelpTextParser();
            var sections = parser.Parse(helpText, exeName);

            if (args.Length == 0)
            {
                HelpTextParser.PrintHelp(sections, "");
                return;
            }

            if (args.Length == 1 && args[0] == "--dump-help")
            {
                HelpTextParser.DumpAllHelp(sections, "all-help.txt");
                return;
            }

            // Case: prog help remote add
            if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                var helpCommandPath = string.Join(" ", args.Skip(1));
                HelpTextParser.PrintHelp(sections, helpCommandPath);
                return;
            }

            // Case: prog remote add → print options for command
            var commandPath = string.Join(" ", args);
            var section = sections.FirstOrDefault(s => s.CommandPath.Equals(commandPath, StringComparison.OrdinalIgnoreCase));

            if (section == null)
            {
                Console.WriteLine($"No help section found for '{commandPath}'");
                return;
            }

            var commandSection = sections.First(s => s.CommandPath == commandPath);
            Console.WriteLine($"\nOptions available for '{commandPath}':\n");
            foreach (var opt in commandSection.Options)
            {
                Console.WriteLine($"Option: -{opt.ShortOption}, --{opt.LongOption}");
            }
        }
        catch (Exception ex)
        {
            var fullname = System.Reflection.Assembly.GetEntryAssembly().Location;
            var progname = Path.GetFileNameWithoutExtension(fullname);
            Console.Error.WriteLine($"{progname} Error: {ex.Message}");
        }
    }
}
*/