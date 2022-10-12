using EpDeviceManagement.Control.Contracts;

namespace EpDeviceManagement.Prediction;

public class LastValuePredictor<TValue> : StreamValuePredictor<TValue>, IValuePredictor<TValue>
{
    public LastValuePredictor()
        : base(1)
    {
    }

    public IEnumerable<TValue> Predict(int steps, int currentDataPoint)
    {
        var lastValue = this.Entries.Last();
        return Enumerable.Repeat(lastValue, steps);
    }
}