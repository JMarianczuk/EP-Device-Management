using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace EpDeviceManagement.Control;

public static class RandomNumberGeneratorExtensions
{
    /// <summary>
    /// Integer in [fromInclusive, toExclusive)
    /// From https://source.dot.net/#System.Security.Cryptography/System/Security/Cryptography/RandomNumberGenerator.cs,ec2801e320ce31ea,references
    /// </summary>
    /// <param name="random"></param>
    /// <param name="fromInclusive"></param>
    /// <param name="toExclusive"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static int Next(this RandomNumberGenerator random, int fromInclusive, int toExclusive)
    {
        if (fromInclusive >= toExclusive)
        {
            throw new ArgumentException("invalid range");
        }

        uint range = (uint)toExclusive - (uint)fromInclusive - 1;
        if (range == 0)
        {
            return fromInclusive;
        }

        uint mask = range;
        mask |= mask >> 1;
        mask |= mask >> 2;
        mask |= mask >> 4;
        mask |= mask >> 8;
        mask |= mask >> 16;

        Span<uint> span = stackalloc uint[1];
        Span<byte> oneUintBytes = MemoryMarshal.AsBytes(span);

        uint result;
        do
        {
            random.GetBytes(oneUintBytes);
            result = mask & span[0];
        } while (result > range);

        return (int)result + fromInclusive;
    }

    /// <summary>
    /// Integer in [0, toExclusive)
    /// </summary>
    /// <param name="random"></param>
    /// <param name="toExclusive"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int Next(this RandomNumberGenerator random, int toExclusive)
    {
        if (toExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toExclusive), "upper bound needs to be positive");
        }
        return random.Next(0, toExclusive);
    }

    public static uint NextUint32(this RandomNumberGenerator random)
    {
        Span<uint> span = stackalloc uint[1];
        Span<byte> oneUintBytes = MemoryMarshal.AsBytes(span);
        random.GetBytes(oneUintBytes);
        return span[0];
    }

    public static ulong NextUint64(this RandomNumberGenerator random)
    {
        Span<ulong> span = stackalloc ulong[1];
        Span<byte> oneUlongBytes = MemoryMarshal.AsBytes(span);
        random.GetBytes(oneUlongBytes);
        return span[0];
    }
    
    public static float NextFloat(this RandomNumberGenerator random)
    {
        return (random.NextUint64() >> 40) * (1.0f / (1u << 24));
    }
    
    public static double NextDouble(this RandomNumberGenerator random)
    {
        return (random.NextUint64() >> 11) * (1.0 / (1ul << 53));
    }
}