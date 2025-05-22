using CliHelpSystem;
using System.Reflection;

var toolName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

var helpText = $"""
                = {toolName}

                *Some general tool description here*.

                List of commands supported:
                $(subcommands)

                == use - use a credit voucher you received

                **Run the use command.**

                Options:
                    -s, --something <thing> - use the specified thing!

                == buy - buy goods
                Buy something on *amazon.co.uk* but only if [green]#you# want.

                Options:
                    -t, --thing <thing> - specify the thing to buy
                """;


var sections = HelpTextParser.Parse(helpText, toolName);

var isHelp = args.Length == 0
          || args[0] == "help"
          || args[0] == "--help";

var commandPath = isHelp
    ? string.Join(" ", args.Skip(1).TakeWhile(arg => !arg.StartsWith("-")))
    : string.Join(" ", args.TakeWhile(arg => !arg.StartsWith("-")));

if (isHelp && string.IsNullOrWhiteSpace(commandPath))
{
    HelpTextParser.PrintHelp(sections, "");
    return 0;
}
else if (isHelp)
{
    HelpTextParser.PrintHelp(sections, commandPath);
    return 0;
}

Console.WriteLine($"command path is {commandPath}");

var availableOptions = HelpTextParser.GetOptions(sections, commandPath);

foreach (var option in availableOptions)
{
    Console.WriteLine($"{option.ShortOption} {option.LongOption} {option.Description}");
}

var handler = CommandRegistry.Resolve(commandPath, Assembly.GetExecutingAssembly());

if (handler is null)
{
    Console.WriteLine($"Unknown command: {commandPath}");
    HelpTextParser.PrintHelp(HelpTextParser.Parse(helpText, toolName), commandPath);
    return 1;
}

return await handler.ExecuteAsync(args.Skip(commandPath.Split(' ').Length).ToArray());