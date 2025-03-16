using System;
using System.Linq;
using Evoq.Ethereum.Crypto;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Represents a legacy Ethereum transaction (pre-EIP-1559).
/// </summary>
public struct Transaction : IEthereumTransaction
{
    /// <summary>
    /// An empty transaction instance.
    /// </summary>
    public static Transaction Empty = new Transaction(
        nonce: 0,
        gasPrice: BigInteger.Zero,
        gasLimit: 0,
        to: new byte[20], // Empty address (all zeros)
        value: BigInteger.Zero,
        data: new byte[0],
        signature: null
    );

    //

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
        BigInteger v,
        BigInteger r,
        BigInteger s)
        : this(nonce, gasPrice, gasLimit, to, value, data, new RsvSignature(v, r, s))
    {
    }

    //

    /// <summary>
    /// The nonce of the transaction.
    /// </summary>
    public ulong Nonce { get; }

    /// <summary>
    /// The gas price of the transaction.
    /// </summary>
    public BigInteger GasPrice { get; }

    /// <summary>
    /// The gas limit of the transaction.
    /// </summary>
    public ulong GasLimit { get; }

    /// <summary>
    /// The to address of the transaction.
    /// </summary>
    public byte[] To { get; } // 20-byte address

    /// <summary>
    /// The value of the transaction.
    /// </summary>
    public BigInteger Value { get; }

    /// <summary>
    /// The data of the transaction.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// The signature of the transaction. Null if the transaction is unsigned.
    /// </summary>
    public RsvSignature? Signature { get; }

    //

    /// <summary>
    /// Determines whether this transaction is signed.
    /// </summary>
    /// <returns>True if the transaction is signed; otherwise, false.</returns>
    public bool IsSigned(out RsvSignature signature)
    {
        if (this.Signature.HasValue)
        {
            signature = this.Signature.Value;
            return true;
        }

        signature = default;
        return false;
    }

    /// <summary>
    /// Gets the features of this transaction.
    /// </summary>
    /// <param name="chainId">Optional chain ID to check for EIP-155 replay protection on the signature.</param>
    /// <returns>The transaction features as flags.</returns>
    public TransactionFeatures GetFeatures(ulong? chainId = null)
    {
        var features = TransactionFeatures.Legacy;

        // Check if signed
        if (this.IsSigned(out var signature))
        {
            features |= TransactionFeatures.Signed;

            // Check for EIP-155 replay protection
            if (chainId.HasValue &&
                signature.HasEIP155ReplayProtection(new BigInteger(chainId.Value.ToString())))
            {
                features |= TransactionFeatures.EIP155ReplayProtection;
            }
        }

        // Check for contract creation
        if (this.To == null || this.To.Length == 0 || this.To.All(b => b == 0))
        {
            features |= TransactionFeatures.ContractCreation;
        }

        // Check for zero value
        if (this.Value.SignValue == 0)
        {
            features |= TransactionFeatures.ZeroValue;
        }

        // Check for data payload
        if (this.Data != null && this.Data.Length > 0)
        {
            features |= TransactionFeatures.HasData;
        }

        return features;
    }

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
    public Transaction WithSignature(BigInteger v, BigInteger r, BigInteger s)
    {
        return WithSignature(new RsvSignature(v, r, s));
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

    // Also need to add explicit interface implementations for WithSignature
    IEthereumTransaction IEthereumTransaction.WithSignature(RsvSignature signature)
    {
        return WithSignature(signature);
    }

    IEthereumTransaction IEthereumTransaction.WithSignature(BigInteger v, BigInteger r, BigInteger s)
    {
        return WithSignature(v, r, s);
    }
}