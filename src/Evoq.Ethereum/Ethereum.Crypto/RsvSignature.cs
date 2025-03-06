using System;
using Evoq.Ethereum.Transactions;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Represents an Ethereum transaction signature with R, S, and V components.
/// </summary>
public struct RsvSignature
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RsvSignature"/> struct.
    /// </summary>
    /// <param name="v">The V component of the signature.</param>
    /// <param name="r">The R component of the signature as a byte array.</param>
    /// <param name="s">The S component of the signature as a byte array.</param>
    public RsvSignature(BigInteger v, BigInteger r, BigInteger s)
    {
        V = v;
        R = r;
        S = s;
    }

    //

    /// <summary>
    /// The R component of the signature as a byte array.
    /// </summary>
    public BigInteger R { get; }

    /// <summary>
    /// The S component of the signature as a byte array.
    /// </summary>
    public BigInteger S { get; }

    /// <summary>
    /// The V component of the signature (recovery ID).
    /// </summary>
    public BigInteger V { get; }

    //

    /// <summary>
    /// Gets the recovery ID from the V value.
    /// </summary>
    /// <param name="chainId">The chain ID for EIP-155 transactions.</param>
    /// <returns>The recovery ID (0 or 1).</returns>
    /// <remarks>
    /// See <see cref="Signing.ExtractRecoveryId"/> for detailed information about 
    /// how recovery ID is extracted from V values.
    /// </remarks>
    public byte GetRecoveryId(BigInteger chainId)
    {
        return Signing.ExtractRecoveryId(this.V, chainId);
    }

    /// <summary>
    /// Computes whether this signature is for an EIP-155 transaction.
    /// </summary>
    /// <param name="chainId">The chain ID to check against.</param>
    /// <returns>True if this is an EIP-155 signature for the specified chain ID; otherwise, false.</returns>
    /// <remarks>
    /// See <see cref="Signing.HasEIP155ReplayProtection"/> for detailed information about 
    /// how EIP-155 replay protection is determined.
    /// </remarks>
    public bool HasEIP155ReplayProtection(BigInteger chainId)
    {
        return Signing.HasEIP155ReplayProtection(this.V, chainId);
    }

    /// <summary>
    /// Gets the y-parity bit (0 or 1) from the V value.
    /// </summary>
    /// <remarks>
    /// The y-parity bit is used to determine which of the two possible public keys
    /// should be recovered from the signature. In Ethereum, this information is encoded
    /// in the V value of the signature, but the encoding has evolved over time.
    /// 
    /// See <see cref="Signing.VToYParity"/> for detailed information about y-parity calculation.
    /// </remarks>
    /// <returns>The y-parity bit (0 or 1).</returns>
    public byte GetYParity()
    {
        return Signing.VToYParity(this.V);
    }

    /// <summary>
    /// Gets the appropriate y-parity value based on the transaction features.
    /// </summary>
    /// <param name="features">The transaction features.</param>
    /// <returns>The y-parity value (0 or 1) appropriate for the transaction type.</returns>
    /// <remarks>
    /// See <see cref="Signing.GetYParityForFeatures"/> for detailed information about 
    /// how y-parity is calculated for different transaction types.
    /// </remarks>
    public byte GetYParity(ITransactionFeatures features)
    {
        var flags = features.GetFeatures();

        return Signing.GetYParityForFeatures(this.V, flags);
    }
}