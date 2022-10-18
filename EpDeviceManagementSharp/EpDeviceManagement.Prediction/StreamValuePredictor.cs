using EpDeviceManagement.Control.Contracts;

namespace EpDeviceManagement.Prediction;

public abstract class StreamValuePredictor<TValue> : IStreamValueReporter<TValue>
{
    private int index;

    protected StreamValuePredictor(int dataStorageLength)
    {
        this.Entries = new OverwritingArray<TValue>(dataStorageLength);
        this.index = -1;
    }

    protected OverwritingArray<TValue> Entries { get; }

    public virtual void ReportCurrentValue(TValue value)
    {
        this.index += 1;
        this.Entries[this.index] = value;
    }
}