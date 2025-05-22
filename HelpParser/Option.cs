#nullable enable

using System.Collections.Generic;


public record Option(char? ShortOption, string? LongOption, int MaxOccurs, int NumberOfParams, string? Group, string? Description, List<ParameterSpec> Parameters);

// public record Option
// {
//     public char? ShortOption { get; }
//     public string? LongOption { get; }
//     public int MaxOccurs { get; }
//     public int NumberOfParams { get; }
//     public string? Group { get; }
// 
//     public string? Description { get; }
// 
//     public List<ParameterSpec> Parameters { get; }
// 
//     // internal constructor so we can use a builder
//     internal Option(char? shortOption, string? longOption, int maxOccurs, int numberOfParams, string? group, string? description, List<ParameterSpec> parameters)
//     {
//         ShortOption = shortOption;
//         LongOption = longOption;
//         MaxOccurs = maxOccurs;
//         NumberOfParams = numberOfParams;
//         Group = group;
//         Description = description;
//         Parameters = parameters;
//     }
// }