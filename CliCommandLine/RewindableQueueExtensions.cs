namespace CliCommandLine;

internal static class RewindableQueueExtensions
{
    internal static List<string> PopN(this RewindableQueue<string> queue, int n)
    {
        var result = new List<string>(n);
        for (int i = 0; i < n; i++)
        {
            result.Add(queue.PopFront().item);
        }
        return result;
    }
}
