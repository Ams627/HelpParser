using CliOption;
using System.Collections.Immutable;

namespace CliCommandLine;

public class OptionsProcessor
{
    private readonly List<IllegalOption> _illegalOptions = [];
    private readonly IDictionary<string, Option> _longOptions;
    private readonly IDictionary<string, int> _stringToAllowedOptionsIndex;

    private readonly List<NonOption> _nonOptions = [];

    /// <summary>
    /// mapping between the index into the allowed options array and any options found on the command line:
    /// </summary>
    private readonly Dictionary<int, List<ParsedOption>> _parsedOptions = [];
    private readonly IDictionary<char, Option> _shortOptions;
    private readonly IDictionary<char, int> _charToAllowedOptionsIndex;

    /// <summary>
    /// supported options specified to the constructor
    /// </summary>
    private readonly Option[] _supportedOptions;
    private HashSet<string> _allowedGroups = [];
    private RewindableQueue<string>? _rewindableQueue;
    public OptionsProcessor(Option[] options)
    {
        _supportedOptions = options;

        var validShortOptions = options.Where(x => x.ShortOption.HasValue);
        var validLongOptions = options.Where(x => !string.IsNullOrEmpty(x.LongOption));

        var shortDups = validShortOptions.GroupBy(x => x.ShortOption).Where(x => x.Count() > 1);
        var longDups = validLongOptions.GroupBy(x => x.LongOption).Where(x => x.Count() > 1);

        if (shortDups.Any() || longDups.Any())
        {
            var smessages = shortDups.Select(x => $"option ={x} specified more than once.")
                .Concat(longDups.Select(x => $"option --{x} specified more than once"));
            throw new ArgumentException(string.Join("\r\n", smessages), nameof(options));
        }

        // short option (char) to the index of the option in the options array:
        _charToAllowedOptionsIndex = validShortOptions.Select((x, i) => new { x.ShortOption, Index = i }).ToImmutableDictionary(x => x.ShortOption!.Value, x => x.Index);

        // long option (string) to the index of the option in the options array:
        _stringToAllowedOptionsIndex = options.Select((x, i) => new { x.LongOption, Index = i }).ToImmutableDictionary(x => x.LongOption!, x => x.Index);

        _shortOptions = validShortOptions.ToImmutableDictionary(x => x.ShortOption!.Value);
        _longOptions = validLongOptions.ToImmutableDictionary(x => x.LongOption!);
    }

    public object[] AllowedGroups => [.. _allowedGroups];
    public IllegalOption[] IllegalOptions => [.. _illegalOptions];
    public NonOption[] NonOptions => [.. _nonOptions];

    /// <summary>
    /// Get the number of occurrences of a particular option specified on the command line sent to ParseOptions
    /// </summary>
    /// <typeparam name="T">should be string (double-letter option) or char (single letter option)</typeparam>
    /// <param name="optionName">a letter or a string for which to return the count</param>
    /// <returns>the number of occurrences of the option</returns>
    public int GetOptionCount<T>(T optionName)
    {
        int optionIndex = -1;

        if (optionName is char c)
        {
            _charToAllowedOptionsIndex.TryGetValue(c, out optionIndex);
        }
        else if (optionName is string s)
        {
            _stringToAllowedOptionsIndex.TryGetValue(s, out optionIndex);
        }

        if (optionIndex == -1)
        {
            return 0;
        }

        var isPresent = _parsedOptions.TryGetValue(optionIndex, out var optionList);
        if (!isPresent)
        {
            return 0;
        }
        return optionList!.Count;
    }

    /// <summary>
    /// GetParam - get the parameter list for an option. The option is either a single letter (single-dash option)
    ///            or a string (double-dash or long option).
    /// </summary>
    /// <typeparam name="T">Either char or string</typeparam>
    /// <param name="optionName">Either a single character or a string</param>
    /// <param name="result">the value of the option parameter - e.g. if the option is --file=f1.c, then result is f1.c</param>
    /// <param name="offset">Where more than one option by the same letter or string is allowed, this is the zero-based offset of the option</param>
    /// <returns>true if the parameter was found</returns>
    public bool TryGetParam<T>(T optionName, out List<string>? result, int offset = 0)
    {
        int optionIndex = -1;
        bool found = false;

        // is it a long (double-dash) option (a string) or a short option (a single char)?
        // which ever one it is, we'll get the index of the option in the allowed option array.
        // Obviously, if there IS an index the option is valid
        if (optionName is string strOptionName)
        {
            found = _stringToAllowedOptionsIndex.TryGetValue(strOptionName, out optionIndex);
        }
        else if (optionName is char optionChar)
        {
            found = _charToAllowedOptionsIndex.TryGetValue(optionChar, out optionIndex);
        }

        if (!found)
        {
            result = default;
            return false;
        }

        if (!_parsedOptions.TryGetValue(optionIndex, out List<ParsedOption>? parsedOptions))
        {
            result = default;
            return false;
        }

        result = parsedOptions[offset].Params;
        return true;
    }

    /// <summary>
    /// Convenience method to get the first parameter of an option
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="optionName"></param>
    /// <param name="offset"></param>
    /// <returns></returns>

    public string? GetFirstParam<T>(T optionName, int offset = 0)
    {
        if (TryGetParam(optionName, out var list, offset) && list!.Count > 0)
            return list[0];
        return null;
    }

    /// <summary>
    /// Determine if a specified single character option is present on the command line. If the option
    /// is present on the command line and has been specified as a valid option, then return true; else
    /// return false.
    /// </summary>
    /// <param name="c">The single character option</param>
    /// <returns>true if the option is present, otherwise false</returns>
    public bool IsOptionPresent(char c) => _charToAllowedOptionsIndex.TryGetValue(c, out var optionIndex) && _parsedOptions.TryGetValue(optionIndex, out _);

    /// <summary>
    /// Determine if a specified long (string) option is present on the command line. If the option
    /// is present on the command line and has been specified as a valid option, then return true; else
    /// return false.
    /// </summary>
    /// <param name="s">The long double-dashed option</param>
    /// <returns>true if the option is present, otherwise false</returns>
    public bool IsOptionPresent(string s) => _stringToAllowedOptionsIndex.TryGetValue(s, out var optionIndex) && _parsedOptions.TryGetValue(optionIndex, out _);

    /// <summary>
    /// Parses a command line passed as an array and fills in internal structures. Options can then be checked via various get accessors.
    /// </summary>
    /// <param name="args">The full command line</param>
    /// <param name="offset">offset within the args array at which to start processing</param>
    /// <param name="allowedGroups">option groups to allow</param>
    public bool ParseCommandLine(string[] args, int offset = 0, string[]? allowedGroups = null)
    {
        _rewindableQueue = new RewindableQueue<string>(args, offset);
        _allowedGroups = allowedGroups == null ? [] : [.. allowedGroups];

        while (!_rewindableQueue.Empty)
        {
            var (arg, index) = _rewindableQueue.PopFront();

            // check for a double-dash (long) option:
            if (arg[0] == '-' && arg[1] == '-')
            {
                if (arg.Length == 2)
                {
                    // here we have found an end-of=options marker (just two dashes) with nothing else:
                    break;
                }

                bool continueArgProcessing = ProcessDoubleDashOption(arg.Substring(2), index);
                if (!continueArgProcessing)
                {
                    break;
                }
            }
            else if (arg[0] == '-')
            {
                // this means stdin and we don't know what to do yet here
                if (arg.Length == 1)
                {
                    _nonOptions.Add(new(arg, index));
                }
                else
                {
                    bool continueArgProcessing = ProcessSingleDashOptions(arg.Substring(1), index);
                    if (!continueArgProcessing)
                    {
                        break;
                    }
                }
            }
            else
            {
                _nonOptions.Add(new(arg, index));
            }
        }
        return _illegalOptions.Count == 0;
    }


    /// <summary>
    /// TryGetAllParams - try to get all the parameters from all occurrences of the option.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="optionName"></param>
    /// <param name="result"></param>
    /// <returns>true if any parameters were present for any occurrence of the option;false otherwise</returns>
    public bool TryGetAllParams<T>(T optionName, out List<string>? result)
    {
        result = [];

        int optionIndex = -1;
        if (optionName is string s)
        {
            _stringToAllowedOptionsIndex.TryGetValue(s, out optionIndex);
        }
        else if (optionName is char c)
        {
            _charToAllowedOptionsIndex.TryGetValue(c, out optionIndex);
        }

        if (optionIndex == -1 || !_parsedOptions.TryGetValue(optionIndex, out List<ParsedOption>? parsedOptions))
            return false;

        if (parsedOptions is null)
        {
            result = default;
            return false;
        }

        result = [.. from parsedOption in parsedOptions
                 from parameter in parsedOption!.Params!
                 where parameter is not null
                 select parameter];

        return true;
    }

    private ParsedOption ConsumeOptionParameters(ParsedOption option)
    {
        var paramSpecs = _supportedOptions[option.OptionIndex].Parameters;
        var list = new List<string>();

        for (int i = 0; i < paramSpecs!.Count; i++)
        {
            list.Add(_rewindableQueue!.PopFront().item);
        }

        return option with { Params = list };
    }


    private bool ProcessDoubleDashOption(string arg, int index)
    {
        // check for a double-dash option with equals in it - the value after equals is the option parameter.
        // subsequent equals after the first ones are allowed - they become part of the option parameter:
        var equalsPosition = arg.IndexOf('=');
        if (equalsPosition == 0)
        {
            _illegalOptions.Add(new("--=", index, ErrorCodes.EqualFirstChar));
        }
        else if (equalsPosition > 0)
        {
            var option = arg.Substring(0, equalsPosition);
            var found = _longOptions.TryGetValue(option, out var optionSpec);
            var optionGroupAllowed = found && (optionSpec!.Group == null || _allowedGroups!.Contains(optionSpec.Group));

            if (!found || !optionGroupAllowed)
            {
                _illegalOptions.Add(new(option, index, ErrorCodes.OptionNotSpecified));
                return true;
            }

            // only a single parameter is allowed after the equals:
            if (optionSpec!.ParameterCount != 1)
            {
                _illegalOptions.Add(new(option, index, ErrorCodes.EqualOptionNotSingleParam));
                return true;
            }

            var parameter = arg[(equalsPosition + 1)..];
            if (parameter.Length == 0)
            {
                _illegalOptions.Add(new(option, index, ErrorCodes.EqualOptionEmptyParameter));
            }

            var parsedOption = new ParsedOption(index, false, _stringToAllowedOptionsIndex[option], [parameter]);
            DictUtils.AddEntryToList(_parsedOptions, parsedOption.OptionIndex, parsedOption);
        }
        else
        {
            // no equals in arg
            var found = _longOptions.TryGetValue(arg, out var optionSpec);
            var optionGroupAllowed = found && (optionSpec!.Group == null || _allowedGroups!.Contains(optionSpec.Group));

            if (!found || !optionGroupAllowed)
            {
                _illegalOptions.Add(new(arg, index, ErrorCodes.OptionNotSpecified));
                return true;
            }

            if (optionSpec!.ParameterCount > _rewindableQueue!.Remaining)
            {
                _illegalOptions.Add(new(arg, index, ErrorCodes.OptionNotEnoughParams));
                // can't do anything else here as we don't have enough args to process:
                return false;
            }

            var optionIndex = _stringToAllowedOptionsIndex[arg];
            var parsedOption = new ParsedOption(index, false, optionIndex, default);
            if (optionSpec.ParameterCount > 0)
            {
                parsedOption = ConsumeOptionParameters(parsedOption);
            }
            DictUtils.AddEntryToList(_parsedOptions, parsedOption.OptionIndex, parsedOption);
        }
        return true;
    }

    private bool ProcessSingleDashOptions(string arg, int index)
    {
        for (int i = 0; i < arg.Length; i++)
        {
            var isLast = i == arg.Length - 1;
            var c = arg[i];

            var found = _shortOptions.TryGetValue(c, out var optionSpec);
            var optionGroupAllowed = found && (optionSpec!.Group == null || _allowedGroups!.Contains(optionSpec.Group));

            if (!found || !optionGroupAllowed)
            {
                // store illegal option, but continue processing:
                _illegalOptions.Add(new($"{c}", index, ErrorCodes.OptionNotSpecified));
                continue;
            }

            if (isLast)
            {
                if (optionSpec!.ParameterCount > _rewindableQueue!.Remaining)
                {
                    // not enough args left as option parameters:
                    _illegalOptions.Add(new($"{c}", index, ErrorCodes.OptionNotEnoughParams));

                    // can't parse anything else here so return false:
                    return false;
                }
                else
                {
                    var optionIndex = _charToAllowedOptionsIndex[c];
                    var parsedOpt = new ParsedOption(index, false, optionIndex, default);
                    parsedOpt = ConsumeOptionParameters(parsedOpt);
                    DictUtils.AddEntryToList(_parsedOptions, optionIndex, parsedOpt);
                }
            }
            else if (optionSpec!.ParameterCount == 0)
            {
                // it's a "boolean" option at this point (it's just a flag and has no parameters) - there may be more options
                // in this arg as in grep -iPo pattern *.txt

                var optionIndex = _charToAllowedOptionsIndex[c];
                var parsedOpt = new ParsedOption(index, true, optionIndex, default);
                DictUtils.AddEntryToList(_parsedOptions, optionIndex, parsedOpt);
            }
            else if (optionSpec.ParameterCount == 1)
            {
                // the single parameter is the rest of the arg - store it and stop scanning this arg:
                var optionIndex = _charToAllowedOptionsIndex[c];
                var parsedOpt = new ParsedOption(index, true, optionIndex, [arg[(i + 1)..]]);
                DictUtils.AddEntryToList(_parsedOptions, optionIndex, parsedOpt);
                break;
            }
            else
            {
                // you can't have an option with more than one parameter right next to the single char option:
                _illegalOptions.Add(new($"{c}", index, ErrorCodes.AdjoiningOptionNotSingleParam));

                // can't parse anything else here so return:
                return false;
            }
        }

        return true;
    }
}