namespace CliCommandLine;

public enum ErrorCodes
{
    /// <summary>
    /// Option not specified in the list passed to the ProgOpts constructor
    /// </summary>
    OptionNotSpecified,

    /// <summary>
    /// It's invalid to have --message="hello world" if the number of parameters specified for the option is not 1.
    /// </summary>
    EqualOptionNotSingleParam,

    /// <summary>
    /// --= is not allowed
    /// </summary>
    EqualFirstChar,

    /// <summary>
    /// we reached the end of the argument list while adding parameters for this option:
    /// </summary>
    OptionNotEnoughParams,

    /// <summary>
    /// a parameter was found immediately adjoining a single character option, but the option takes more than one parameter
    /// </summary>
    AdjoiningOptionNotSingleParam,

    /// <summary>
    /// An equals appeared in an arg which specified a valid long option but there were no more characters after the equals
    /// </summary>
    EqualOptionEmptyParameter
}
