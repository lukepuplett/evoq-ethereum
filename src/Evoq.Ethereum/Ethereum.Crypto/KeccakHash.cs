using Evoq.Blockchain;
using Org.BouncyCastle.Crypto.Digests;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Keccak-256 hashing utilities.
/// </summary>
public static class KeccakHash
{
    /// <summary>
    /// Computes the Keccak-256 hash of the input data.
    /// </summary>
    /// <param name="input">The input data to hash.</param>
    /// <returns>The 32-byte hash.</returns>
    public static byte[] ComputeHash(Hex input) => ComputeHash(input.ToByteArray());

    /// <summary>
    /// Computes the Keccak-256 hash of the input data.
    /// </summary>
    /// <param name="input">The input data to hash.</param>
    /// <returns>The 32-byte hash.</returns>
    public static byte[] ComputeHash(byte[] input)
    {
        var digest = new KeccakDigest(256);
        var output = new byte[digest.GetDigestSize()];

        digest.BlockUpdate(input, 0, input.Length);
        digest.DoFinal(output, 0);

        return output;
    }
}
