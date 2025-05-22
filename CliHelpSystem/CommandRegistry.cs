using System.Reflection;

namespace CliHelpSystem;

public static class CommandRegistry
{
    public static Dictionary<string, Type> DiscoverCommands(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();

        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ICommandHandler).IsAssignableFrom(t))
            .Select(t => new
            {
                Type = t,
                Attr = t.GetCustomAttribute<CommandHandlerAttribute>()
            })
            .Where(x => x.Attr != null)
            .ToDictionary(x => x.Attr!.CommandPath, x => x.Type);
    }

    public static ICommandHandler? Resolve(string commandPath, Assembly? assembly = null)
    {
        var commands = DiscoverCommands(assembly);
        return commands.TryGetValue(commandPath, out var type)
            ? (ICommandHandler?)Activator.CreateInstance(type)
            : null;
    }
}
