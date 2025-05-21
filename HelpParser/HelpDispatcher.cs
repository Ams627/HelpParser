using System;
using System.Collections.Generic;
using System.Linq;

namespace HelpParser;

public static class HelpDispatcher
{
    public static void Dispatch(string[] args, List<HelpSection> sections, string toolName)
    {
        if (args.Length == 0 || args[0] != "help")
        {
            Console.WriteLine("Usage: help [command [subcommand ...]]");
            return;
        }

        // Skip "help"
        var commandPath = string.Join(" ", args.Skip(1));
        HelpTextParser.PrintHelp(sections, commandPath);
    }
}
