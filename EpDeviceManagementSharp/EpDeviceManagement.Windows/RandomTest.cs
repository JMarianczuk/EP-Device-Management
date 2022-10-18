using EpDeviceManagement.Simulation;

namespace EpDeviceManagement.Windows;

public class RandomTest
{
    public static void Test()
    {
        var rand = new Random(13254);
        var results = new long[256];
        for (int i = 0; i < results.Length; i += 1)
        {
            results[i] = 0;
        }
        var array = new byte[256];
        using (var prog = new ConsoleProgressBar())
        {
            var length = 10000000;
            prog.Setup(length / 1024, "generating random numbers");
            for (int i = 1; i < length; i += 1)
            {
                rand.NextBytes(array);
                foreach (var b in array)
                {
                    results[b] += 1;
                }

                if (i % 1024 == 0)
                {
                    prog.FinishOne();
                }
            }
        }

        var min = results.Min();
        var max = results.Max();
        var average = results.Average();
        var sumOfSquaresOfDifference = results.Select(x => (x - average) * (x - average)).Sum();
        var standardDeviation = Math.Sqrt(sumOfSquaresOfDifference / results.Length);
        Console.Write($"min: {min}, max: {max}, average: {average}, stddev: {standardDeviation}");
    }
}