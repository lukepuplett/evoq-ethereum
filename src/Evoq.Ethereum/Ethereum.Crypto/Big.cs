using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// A static class containing useful BigInteger constants.
/// </summary>
public static class Big
{
    public static readonly BigInteger Zero = BigInteger.ValueOf(0);
    public static readonly BigInteger One = BigInteger.ValueOf(1);
    public static readonly BigInteger Two = BigInteger.ValueOf(2);
    public static readonly BigInteger Three = BigInteger.ValueOf(3);
    public static readonly BigInteger Four = BigInteger.ValueOf(4);
    public static readonly BigInteger Seven = BigInteger.ValueOf(7);
}
