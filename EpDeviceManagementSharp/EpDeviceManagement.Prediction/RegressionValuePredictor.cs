using EpDeviceManagement.Control.Contracts;
using MathNet.Numerics.LinearAlgebra.Double;
using RDotNet;

namespace EpDeviceManagement.Prediction;

public class RegressionValuePredictor<TValue> : StreamValuePredictor<double>, IStreamValueReporter<TValue>
{
    private const int NumberOfInputsPerPrediction = 5;

    private readonly int forecastingHorizon;
    private readonly Func<TValue, double> toNumeric;
    private readonly Func<double, TValue> toValue;
    private readonly (int, double)[,] factors;
    private readonly double[] scalarTerms;
    private int reportedValues;
    private readonly REngine rEngine;
    private NumericVector valueVector;
    private readonly string valueVectorName;
    private readonly string fitName;
    private readonly NumericVector predictionVector;
    private readonly string predictionVectorName;

    public RegressionValuePredictor(
        int historySize,
        int forecastingHorizon,
        Func<TValue, double> toNumeric,
        Func<double, TValue> toValue)
        : base(historySize)
    {
        if (historySize <= 10 * forecastingHorizon)
        {
            throw new ArgumentException(nameof(historySize), "history too small compared to the required horizon");
        }
        this.forecastingHorizon = forecastingHorizon;
        this.toNumeric = toNumeric;
        this.toValue = toValue;
        this.factors = new (int, double)[forecastingHorizon, NumberOfInputsPerPrediction];
        this.scalarTerms = new double[forecastingHorizon];
        this.reportedValues = 0;
        for (int i = 0; i < historySize; i += 1)
        {
            // flush entries with zeros for proper index usage at prediction
            base.ReportCurrentValue(0d);
        }

        this.rEngine = REngine.GetInstance();
        this.valueVector = this.rEngine.CreateNumericVector(0);
        var id = Guid.NewGuid().ToString().Replace('-', '_');
        this.valueVectorName = $"values_{id}";
        this.fitName = $"fit_{id}";
        this.predictionVector = this.rEngine.CreateNumericVector(forecastingHorizon);
        this.predictionVectorName = $"prediction_{id}";
        this.rEngine.SetSymbol(this.predictionVectorName, this.predictionVector);
    }

    public void ReportCurrentValue(TValue value)
    {
        this.reportedValues += 1;
        if (this.reportedValues % this.forecastingHorizon == 0 && this.reportedValues > this.forecastingHorizon)
        {
            this.CalculateLm();
        }
        base.ReportCurrentValue(this.toNumeric(value));
    }

    private void CalculateLm()
    {
        this.SetValueVector();
        this.rEngine.Evaluate($"{this.fitName} <- lm({this.valueVectorName} ~ trend)");
    }

    private void SetValueVector()
    {
        if (this.valueVector.Length != this.Entries.Count)
        {
            this.valueVector = this.rEngine.CreateNumericVector(this.Entries);
            this.rEngine.SetSymbol(this.valueVectorName, valueVector);
        }
        else
        {
            for (int i = 0; i < this.Entries.Count; i += 1)
            {
                this.valueVector[i] = this.Entries[i];
            }
        }
    }

    private void CalculateRegressionFactors()
    {
        var values = rEngine.CreateNumericVector(this.Entries);
        this.rEngine.SetSymbol(nameof(values), values);
        this.rEngine.Evaluate($"fit <- tbats({nameof(values)}");
        var parameters = this.rEngine.GetSymbol("fit$parameters").AsNumeric();

        var historyCount = this.Entries.Count - this.forecastingHorizon;
        var f_array = new double[2, historyCount];
        for (int i = 0; i < historyCount; i += 1)
        {
            f_array[0, i] = 1d;
            f_array[1, i] = this.Entries[i];
        }

        var y_array = new double[this.forecastingHorizon];
        for (int i = 0; i < this.forecastingHorizon; i += 1)
        {
            y_array[i] = this.Entries[historyCount + i];
        }

        var F = DenseMatrix.OfArray(f_array);
        var F_Transposed = F.Transpose();
        var y = DenseVector.OfArray(y_array);
        // â = (F^T x F)^(-1) x F^T x y
        // <=> (F^T x F) x â = F^T x y
        // <=> left x â = right
        var left = F * F_Transposed;
        var right = F_Transposed * y;
        var a = left.Solve(right);
    }

    public IEnumerable<TValue> Predict(int steps)
    {
        if (this.reportedValues < 2 * this.forecastingHorizon)
        {
            var average = this.Entries.Average();
            return Enumerable.Repeat(this.toValue(average), steps);
        }

        //return this.PredictFromFactors(steps);
        return this.PredictFromLm(steps);
    }

    private IEnumerable<TValue> PredictFromFactors(int steps)
    {
        for (int step = 0; step < steps; step += 1)
        {
            var result = this.scalarTerms[step];
            for (int input = 0; input < NumberOfInputsPerPrediction; input += 1)
            {
                var (index, factor) = this.factors[step, input];
                result += this.Entries[index] * factor;
            }

            yield return this.toValue(result);
        }
    }

    private IEnumerable<TValue> PredictFromLm(int steps)
    {
        this.SetValueVector();
        this.rEngine.Evaluate($"{this.predictionVectorName} <- predict({this.fitName}, {this.valueVectorName})");
        return this.predictionVector.Select(this.toValue).ToList();
    }
}