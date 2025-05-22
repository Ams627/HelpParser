using System;

namespace CliCommandLine;

/// <summary>
/// Initialises an instance of the RewindableQueue class. This class allows removal of an entity from the front of a queue
/// only. The queue is initialised with an array and an offset from which to start popping.
///     It's not possible to add to the queue
///     You can only pop from the front
///     You can undo pops up to the point specified by the original offset
/// </summary>
/// <typeparam name="T"></typeparam>
internal class RewindableQueue<T>
{
    private readonly T[] _array;
    private int _remaining;
    private readonly int _originalCount;

    public RewindableQueue(T[] array, int offset = 0)
    {
        _array = array;
        _originalCount = array.Length - offset;
        _remaining = _originalCount;
    }

    /// <summary>
    /// the ORIGINAL number of items in the queue - never changes after construction
    /// </summary>
    public int OriginalCount => _originalCount;

    /// <summary>
    /// The remaining number of items in the queue:
    /// </summary>
    public int Remaining => _remaining;

    /// <summary>
    /// true if the queue is empty:
    /// </summary>
    public bool Empty => _remaining == 0;

    public (T item, int index) PopFront()
    {
        if (_remaining == 0)
        {
            throw new InvalidOperationException($"No items remaining in the RewindableQueue");
        }
        var result = (_array[^_remaining], _array.Length - Remaining);
        _remaining--;
        return result;
    }

    /// <summary>
    /// Undo the last pop
    /// </summary>
    public void Undo()
    {
        if (_remaining == _originalCount)
        {
            throw new InvalidOperationException($"Cannot undo in RewindableQueue - the RewindableQueue is at its original fill level.");
        }
        _remaining++;
    }
}