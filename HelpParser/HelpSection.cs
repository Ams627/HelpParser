namespace CliFx.HelpSystem;

public class HelpSection
{
    public string CommandSummary { get; set; } = "";
    public string CommandPath { get; set; } = ""; // e.g. "remote add"
    public string HelpText { get; set; } = "";
    public List<Option> Options { get; set; } = new();
    public string OptionGroup => CommandPath; // using path string as grouping key
}
