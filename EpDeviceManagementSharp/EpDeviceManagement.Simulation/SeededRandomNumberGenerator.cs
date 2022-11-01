using System.Security.Cryptography;

namespace EpDeviceManagement.Simulation;

public class SeededRandomNumberGenerator : RandomNumberGenerator
{
    private readonly Random random;

    public SeededRandomNumberGenerator(int seed)
    {
        this.random = new Random(seed);
    }

    public override void GetBytes(byte[] data)
    {
        this.random.NextBytes(data);
    }

    public override void GetBytes(Span<byte> data)
    {
        this.random.NextBytes(data);
    }
}