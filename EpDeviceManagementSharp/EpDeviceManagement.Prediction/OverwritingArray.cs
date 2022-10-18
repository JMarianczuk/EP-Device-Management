using System.Collections;

namespace EpDeviceManagement.Prediction;

public class OverwritingArray<TElement> : IEnumerable<TElement>
{
    private TElement[] array;
    private int largestIndex = -1;

    public OverwritingArray(int size)
    {
        if (size == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "cannot be zero");
        }
        this.array = new TElement[size];
    }

    public TElement this[int index]
    {
        get
        {
            this.largestIndex = Math.Max(this.largestIndex, index);
            return array[index % array.Length];
        }
        set
        {
            this.largestIndex = Math.Max(this.largestIndex, index);
            array[index % array.Length] = value;
        }
    }

    public int Count => Math.Min(this.largestIndex + 1, this.array.Length);

    public IEnumerator<TElement> GetEnumerator()
    {
        var isFull = this.Count == this.array.Length;
        return new IndexEnumerator<TElement>(
            this.array,
            isFull
                ? (this.largestIndex + 1) % this.array.Length
                : 0,
            this.largestIndex % array.Length,
            isFull);
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}