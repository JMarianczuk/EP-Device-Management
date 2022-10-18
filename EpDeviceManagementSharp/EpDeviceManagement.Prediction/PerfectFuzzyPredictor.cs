using System.Security.Cryptography;
using EpDeviceManagement.Control;
using EpDeviceManagement.Control.Contracts;

namespace EpDeviceManagement.Prediction;

public class PerfectFuzzyPredictor<TValue> : IValuePredictor<TValue>
{
    private readonly IReadOnlyList<TValue> values;
    private readonly Func<TValue, double> toNumeric;
    private readonly Func<double, TValue> toValue;
    private readonly double fuzzyFactor;
    private readonly double fuzzyOffset;
    private readonly RandomNumberGenerator random;

    public PerfectFuzzyPredictor(
        IReadOnlyList<TValue> values,
        Func<TValue, double> toNumeric,
        Func<double, TValue> toValue,
        double fuzzyFactor,
        TValue fuzzyOffset,
        RandomNumberGenerator random)
    {
        this.values = values;
        this.toNumeric = toNumeric;
        this.toValue = toValue;
        this.fuzzyFactor = fuzzyFactor;
        this.fuzzyOffset = toNumeric(fuzzyOffset);
        this.random = random;
    }

    public IEnumerable<TValue> Predict(int steps, int currentDataPoint)
    {
        var firstPredictedIndex = currentDataPoint + 1;
        for (int i = firstPredictedIndex; i < firstPredictedIndex + steps; i += 1)
        {
            if (i < this.values.Count)
            {
                yield return Fuzzy(values[i]);
            }
            else
            {
                yield return this.toValue(0);
            }
        }
    }

    private TValue Fuzzy(TValue value)
    {
        var asNumeric = this.toNumeric(value);
        var fuzzyDelta = (this.random.NextDouble() - 0.5) * 2 * this.fuzzyFactor;
        var offset = (this.random.NextDouble() - 0.5) * 2 * this.fuzzyOffset;
        var fuzziedValue = asNumeric * (1 + fuzzyDelta) + offset;
        return this.toValue(Math.Max(fuzziedValue, 0));
    }
}