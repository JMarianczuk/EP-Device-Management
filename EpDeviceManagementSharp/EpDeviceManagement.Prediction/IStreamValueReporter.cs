using EpDeviceManagement.Control.Contracts;

namespace EpDeviceManagement.Prediction;

public interface IStreamValueReporter<in TValue>
{
    void ReportCurrentValue(TValue value);
}