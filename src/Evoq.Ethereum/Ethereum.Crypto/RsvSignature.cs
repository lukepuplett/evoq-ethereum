using System;
using Evoq.Blockchain;
using Evoq.Ethereum.Transactions;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Represents an Ethereum transaction signature with R, S, and V components.
/// </summary>
public interface IRsvSignature
{
    /// <summary>
    /// The V component of the signature.
    /// </summary>
    BigInteger V { get; }

    /// <summary>
    /// The R component of the signature.
    /// </summary>
    BigInteger R { get; }

    /// <summary>
    /// The S component of the signature.
    /// </summary>
    BigInteger S { get; }
}

/// <summary>
/// Represents an Ethereum transaction signature with R, S, and V components.
/// </summary>
public struct RsvSignature : IRsvSignature
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
    public ulong GetYParity(ITransactionFeatures features)
    {
        var flags = features.GetFeatures();

        return Signing.GetYParityForFeatures(this.V, flags);
    }

    /// <summary>
    /// Converts the signature to a 65-byte array in RSV format.
    /// </summary>
    /// <returns>A byte array containing R (32 bytes), S (32 bytes), and V (1 byte).</returns>
    public byte[] ToByteArray()
    {
        var result = new byte[65];

        var rBytes = this.R.ToByteArrayUnsigned();
        var sBytes = this.S.ToByteArrayUnsigned();

        // Pad R to 32 bytes if necessary
        Array.Copy(rBytes, 0, result, 32 - rBytes.Length, rBytes.Length);

        // Pad S to 32 bytes if necessary
        Array.Copy(sBytes, 0, result, 64 - sBytes.Length, sBytes.Length);

        // V is stored in the last byte
        result[64] = (byte)this.V.IntValue;

        return result;
    }

    //

    /// <summary>
    /// Creates an RSV signature from a 65-byte signature array.
    /// </summary>
    /// <param name="signatureArray">The 65-byte signature array containing R (32 bytes), S (32 bytes), and V (1 byte).</param>
    /// <returns>A new RsvSignature instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the signature array length is not 65 bytes.</exception>
    public static RsvSignature FromBytes(byte[] signatureArray)
    {
        if (signatureArray.Length != 65)
        {
            throw new ArgumentException("Signature must be exactly 65 bytes", nameof(signatureArray));
        }

        var r = new byte[32];
        var s = new byte[32];
        Array.Copy(signatureArray, r, 32);
        Array.Copy(signatureArray, 32, s, 0, 32);

        var v = signatureArray[64];
        if (v == 0 || v == 1)
        {
            v = (byte)(v + 27);
        }

        return new RsvSignature(
            v: new BigInteger(new[] { v }),
            r: new BigInteger(1, r),
            s: new BigInteger(1, s)
        );
    }

    /// <summary>
    /// Creates an RSV signature from a hex string.
    /// </summary>
    /// <param name="hex">The hex string to convert.</param>
    /// <returns>A new RsvSignature instance.</returns>
    public static RsvSignature FromHex(string hex) => FromBytes(Hex.Parse(hex).ToByteArray());
}