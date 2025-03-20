using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// A static class containing useful BigInteger constants.
/// </summary>
internal static class Big
{
    /// <summary>
    /// The zero BigInteger.
    /// </summary>
    public static readonly BigInteger Zero = BigInteger.ValueOf(0);

    /// <summary>
    /// The one BigInteger.
    /// </summary>
    public static readonly BigInteger One = BigInteger.ValueOf(1);

    /// <summary>
    /// The two BigInteger.
    /// </summary>
    public static readonly BigInteger Two = BigInteger.ValueOf(2);

    /// <summary>
    /// The three BigInteger.
    /// </summary>
    public static readonly BigInteger Three = BigInteger.ValueOf(3);

    /// <summary>
    /// The four BigInteger.
    /// </summary>
    public static readonly BigInteger Four = BigInteger.ValueOf(4);

    /// <summary>
    /// The seven BigInteger.
    /// </summary>
    public static readonly BigInteger Seven = BigInteger.ValueOf(7);
}
