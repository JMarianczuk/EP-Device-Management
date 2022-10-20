using System.Collections;
using System.Security.Cryptography.X509Certificates;

namespace EpDeviceManagement.Prediction;

public class IndexEnumerator<TValue> : IEnumerator<TValue>
{
    private readonly IReadOnlyList<TValue> indexable;
    private readonly int startIndex;
    private readonly int endIndex;
    private readonly bool loop;
    private int currentIndex;
    private bool hasLooped = false;

    public IndexEnumerator(
        IReadOnlyList<TValue> indexable,
        int startIndex,
        int endIndex,
        bool loop)
    {
        this.indexable = indexable;
        if (startIndex < 0 || startIndex >= indexable.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (endIndex < 0 || endIndex >= indexable.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(endIndex));
        }
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.currentIndex = startIndex - 1;
        if (endIndex == indexable.Count - 1)
        {
            this.loop = false;
        }
        else
        {
            this.loop = loop;
        }

        if (this.loop && endIndex >= startIndex)
        {
            throw new ArgumentOutOfRangeException(
                $"{nameof(endIndex)} cannot be after {nameof(startIndex)} when looping");
        }
    }

    public bool MoveNext()
    {
        this.currentIndex += 1;
        if (!this.loop)
        {
            return this.currentIndex <= this.endIndex;
        }

        if (this.hasLooped)
        {
            return this.currentIndex <= this.endIndex;
        }

        if (this.currentIndex == this.indexable.Count)
        {
            this.hasLooped = true;
            this.currentIndex = 0;
        }

        return this.currentIndex <= this.endIndex;
    }

    public TValue Current => this.indexable[this.currentIndex];

    public void Reset() => throw new NotImplementedException();

    object? IEnumerator.Current => Current;

    public void Dispose()
    {

    }
}