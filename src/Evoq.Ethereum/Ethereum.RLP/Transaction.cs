using System;
using Evoq.Ethereum.Crypto;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Represents a legacy Ethereum transaction (pre-EIP-1559).
/// </summary>
public struct Transaction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> struct.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <param name="gasPrice">The gas price of the transaction.</param>
    /// <param name="gasLimit">The gas limit of the transaction.</param>
    /// <param name="to">The to address of the transaction.</param>
    /// <param name="value">The value of the transaction.</param>
    /// <param name="data">The data of the transaction.</param>
    /// <param name="signature">The signature of the transaction. Null if the transaction is unsigned.</param>
    public Transaction(
        ulong nonce,
        BigInteger gasPrice,
        ulong gasLimit,
        byte[] to,
        BigInteger value,
        byte[] data,
        RsvSignature? signature)
    {
        Nonce = nonce;
        GasPrice = gasPrice;
        GasLimit = gasLimit;
        To = to ?? throw new ArgumentNullException(nameof(to));
        Value = value;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Signature = signature;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> struct with separate signature components.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <param name="gasPrice">The gas price of the transaction.</param>
    /// <param name="gasLimit">The gas limit of the transaction.</param>
    /// <param name="to">The to address of the transaction.</param>
    /// <param name="value">The value of the transaction.</param>
    /// <param name="data">The data of the transaction.</param>
    /// <param name="v">The V component of the signature.</param>
    /// <param name="r">The R component of the signature.</param>
    /// <param name="s">The S component of the signature.</param>
    public Transaction(
        ulong nonce,
        BigInteger gasPrice,
        ulong gasLimit,
        byte[] to,
        BigInteger value,
        byte[] data,
        byte v,
        byte[] r,
        byte[] s)
        : this(nonce, gasPrice, gasLimit, to, value, data, new RsvSignature(v, r, s))
    {
    }

    //

    /// <summary>
    /// The nonce of the transaction.
    /// </summary>
    public ulong Nonce;

    /// <summary>
    /// The gas price of the transaction.
    /// </summary>
    public BigInteger GasPrice;

    /// <summary>
    /// The gas limit of the transaction.
    /// </summary>
    public ulong GasLimit;

    /// <summary>
    /// The to address of the transaction.
    /// </summary>
    public byte[] To; // 20-byte address

    /// <summary>
    /// The value of the transaction.
    /// </summary>
    public BigInteger Value;

    /// <summary>
    /// The data of the transaction.
    /// </summary>
    public byte[] Data;

    /// <summary>
    /// The signature of the transaction. Null if the transaction is unsigned.
    /// </summary>
    public RsvSignature? Signature;

    /// <summary>
    /// Determines whether this transaction is signed.
    /// </summary>
    /// <returns>True if the transaction is signed; otherwise, false.</returns>
    public bool IsSigned => Signature.HasValue;

    /// <summary>
    /// Gets the V component of the signature, or 0 if the transaction is unsigned.
    /// </summary>
    public byte V => Signature?.V ?? 0;

    /// <summary>
    /// Gets the R component of the signature, or empty array if the transaction is unsigned.
    /// </summary>
    public byte[] R => Signature?.R ?? Array.Empty<byte>();

    /// <summary>
    /// Gets the S component of the signature, or empty array if the transaction is unsigned.
    /// </summary>
    public byte[] S => Signature?.S ?? Array.Empty<byte>();

    //

    /// <summary>
    /// Creates a signed transaction by adding a signature to an unsigned transaction.
    /// </summary>
    /// <param name="signature">The signature to add.</param>
    /// <returns>A signed transaction.</returns>
    public Transaction WithSignature(RsvSignature signature)
    {
        return new Transaction(
            Nonce,
            GasPrice,
            GasLimit,
            To,
            Value,
            Data,
            signature
        );
    }

    /// <summary>
    /// Creates a signed transaction by adding signature components to an unsigned transaction.
    /// </summary>
    /// <param name="v">The V component of the signature.</param>
    /// <param name="r">The R component of the signature.</param>
    /// <param name="s">The S component of the signature.</param>
    /// <returns>A signed transaction.</returns>
    public Transaction WithSignature(byte v, byte[] r, byte[] s)
    {
        return WithSignature(new RsvSignature(v, r, s));
    }

    /// <summary>
    /// Creates a signed transaction by adding a signature created from a recovery ID and chain ID.
    /// </summary>
    /// <param name="recoveryId">The recovery ID (0 or 1).</param>
    /// <param name="r">The R component of the signature.</param>
    /// <param name="s">The S component of the signature.</param>
    /// <param name="chainId">The chain ID for EIP-155 replay protection.</param>
    /// <returns>A signed transaction.</returns>
    public Transaction WithSignatureFromRecoveryId(byte recoveryId, byte[] r, byte[] s, ulong chainId = 0)
    {
        return WithSignature(RsvSignature.FromRecoveryId(recoveryId, r, s, chainId));
    }

    //

    /// <summary>
    /// Creates an unsigned transaction.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <param name="gasPrice">The gas price of the transaction.</param>
    /// <param name="gasLimit">The gas limit of the transaction.</param>
    /// <param name="to">The to address of the transaction.</param>
    /// <param name="value">The value of the transaction.</param>
    /// <param name="data">The data of the transaction.</param>
    /// <returns>An unsigned transaction.</returns>
    public static Transaction CreateUnsigned(
        ulong nonce,
        BigInteger gasPrice,
        ulong gasLimit,
        byte[] to,
        BigInteger value,
        byte[] data)
    {
        return new Transaction(
            nonce,
            gasPrice,
            gasLimit,
            to,
            value,
            data,
            null // No signature
        );
    }

    /// <summary>
    /// Gets an empty transaction instance.
    /// </summary>
    public static Transaction Empty => new Transaction(
        nonce: 0,
        gasPrice: BigInteger.Zero,
        gasLimit: 0,
        to: new byte[20], // Empty address (all zeros)
        value: BigInteger.Zero,
        data: new byte[0],
        signature: null
    );
}