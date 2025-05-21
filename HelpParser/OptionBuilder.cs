#nullable enable

using System;

public class OptionBuilder
{
    private char? _shortOption;
    private string? _longOption;
    private int _maxOccurs = 1;
    private int _numberOfParams = 0;
    private string? _group;

    public OptionBuilder WithShortOption(char? c)
    {
        _shortOption = c;
        return this;
    }

    public OptionBuilder WithLongOption(string? s)
    {
        _longOption = s;
        return this;
    }

    public OptionBuilder WithMaxOccurs(int max)
    {
        _maxOccurs = max;
        return this;
    }

    public OptionBuilder WithNumberOfParams(int n)
    {
        _numberOfParams = n;
        return this;
    }

    public OptionBuilder WithGroup(string group)
    {
        _group = group;
        return this;
    }

    public Option Build()
    {
        if (_shortOption == '\0' && string.IsNullOrWhiteSpace(_longOption))
            throw new InvalidOperationException("Must specify either short or long option.");

        if (_maxOccurs < 1)
            throw new InvalidOperationException("MaxOccurs must be at least 1.");

        if (_numberOfParams < 0)
            throw new InvalidOperationException("NumberOfParams cannot be negative.");

        return new Option(_shortOption, _longOption, _maxOccurs, _numberOfParams, _group);
    }

    public OptionBuilder Reset()
    {
        _shortOption = '\0';
        _longOption = null;
        _maxOccurs = 1;
        _numberOfParams = 0;
        _group = null;
        return this;
    }
}

