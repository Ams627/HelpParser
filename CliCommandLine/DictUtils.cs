namespace CliCommandLine;

internal class DictUtils
{
    /// <summary>
    /// Add an entry to a List contained within a Dictionary. Add a new List containing the item if there isn't already one in the Dictionary.
    /// </summary>
    /// <typeparam name="T1">Type of the Dictionary key</typeparam>
    /// <typeparam name="T2">Type of the data stored in the List</typeparam>
    /// <param name="d">Dictionary to add to</param>
    /// <param name="key">Dictionary key of the List to add to</param>
    /// <param name="listEntry">Value to add to the List</param>
    public static void AddEntryToList<T1, T2>(Dictionary<T1, List<T2>> d, T1 key, T2 listEntry)
    {
        if (!d.TryGetValue(key, out var list))
        {
            list = [];
            d.Add(key, list);
        }

        list.Add(listEntry);
    }
}
