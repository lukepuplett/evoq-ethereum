using System;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Represents an Ethereum transaction signature with R, S, and V components.
/// </summary>
public struct RsvSignature
{
    /// <summary>
    /// The R component of the signature as a byte array.
    /// </summary>
    public byte[] R { get; }

    /// <summary>
    /// The S component of the signature as a byte array.
    /// </summary>
    public byte[] S { get; }

    /// <summary>
    /// The V component of the signature (recovery ID).
    /// </summary>
    public byte V { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RsvSignature"/> struct.
    /// </summary>
    /// <param name="v">The V component of the signature.</param>
    /// <param name="r">The R component of the signature as a byte array.</param>
    /// <param name="s">The S component of the signature as a byte array.</param>
    public RsvSignature(byte v, byte[] r, byte[] s)
    {
        V = v;
        R = r ?? throw new ArgumentNullException(nameof(r));
        S = s ?? throw new ArgumentNullException(nameof(s));
    }

    /// <summary>
    /// Creates a signature from a recovery ID and EIP-155 chain ID.
    /// </summary>
    /// <param name="recoveryId">The recovery ID (0 or 1).</param>
    /// <param name="r">The R component of the signature as a byte array.</param>
    /// <param name="s">The S component of the signature as a byte array.</param>
    /// <param name="chainId">The chain ID for EIP-155 replay protection.</param>
    /// <returns>A signature with the V value calculated according to EIP-155.</returns>
    public static RsvSignature FromRecoveryId(byte recoveryId, byte[] r, byte[] s, ulong chainId = 0)
    {
        // For EIP-155: v = recoveryId + chainId * 2 + 35
        // For pre-EIP-155: v = recoveryId + 27
        byte v = chainId > 0
            ? (byte)(recoveryId + chainId * 2 + 35)
            : (byte)(recoveryId + 27);

        return new RsvSignature(v, r, s);
    }

    /// <summary>
    /// Gets the recovery ID from the V value.
    /// </summary>
    /// <param name="chainId">The chain ID for EIP-155 transactions.</param>
    /// <returns>The recovery ID (0 or 1).</returns>
    public byte GetRecoveryId(ulong chainId = 0)
    {
        if (chainId > 0 && V >= 35 + chainId * 2)
        {
            // EIP-155: v = recoveryId + chainId * 2 + 35
            return (byte)(V - chainId * 2 - 35);
        }

        // Pre-EIP-155: v = recoveryId + 27
        return (byte)(V - 27);
    }

    /// <summary>
    /// Determines whether this signature is for an EIP-155 transaction.
    /// </summary>
    /// <param name="chainId">The chain ID to check against.</param>
    /// <returns>True if this is an EIP-155 signature for the specified chain ID; otherwise, false.</returns>
    public bool IsEIP155(ulong chainId)
    {
        return V == chainId * 2 + 35 || V == chainId * 2 + 36;
    }
}