namespace CliFx.HelpSystem;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandHandlerAttribute : Attribute
{
    public string CommandPath { get; }

    public CommandHandlerAttribute(string commandPath)
    {
        CommandPath = commandPath;
    }
}
