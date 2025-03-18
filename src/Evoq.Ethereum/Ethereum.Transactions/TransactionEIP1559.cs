using System;
using System.Linq;
using Evoq.Ethereum.Crypto;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// An EIP-1559 Ethereum transaction with a signature.
/// </summary>
public struct TransactionEIP1559 : IEthereumTransaction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionEIP1559"/> struct.
    /// </summary>
    /// <param name="chainId">The chain ID of the network.</param>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <param name="maxPriorityFeePerGas">The maximum priority fee per gas (tip for miners/validators).</param>
    /// <param name="maxFeePerGas">The maximum fee per gas (total fee cap).</param>
    /// <param name="gasLimit">The gas limit of the transaction.</param>
    /// <param name="to">The to address of the transaction.</param>
    /// <param name="value">The value of the transaction.</param>
    /// <param name="data">The data of the transaction.</param>
    /// <param name="accessList">The access list for gas optimization.</param>
    /// <param name="signature">The signature of the transaction. Null if the transaction is unsigned.</param>
    public TransactionEIP1559(
        ulong chainId,
        ulong nonce,
        BigInteger maxPriorityFeePerGas,
        BigInteger maxFeePerGas,
        ulong gasLimit,
        byte[] to,
        BigInteger value,
        byte[] data,
        AccessListItem[]? accessList,
        RsvSignature? signature)
    {
        ChainId = chainId;
        Nonce = nonce;
        MaxPriorityFeePerGas = maxPriorityFeePerGas;
        MaxFeePerGas = maxFeePerGas;
        GasLimit = gasLimit;
        To = to ?? throw new ArgumentNullException(nameof(to));
        Value = value;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        AccessList = accessList ?? Array.Empty<AccessListItem>();
        Signature = signature;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionEIP1559"/> struct with separate signature components.
    /// </summary>
    /// <param name="chainId">The chain ID of the network.</param>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <param name="maxPriorityFeePerGas">The maximum priority fee per gas (tip for miners/validators).</param>
    /// <param name="maxFeePerGas">The maximum fee per gas (total fee cap).</param>
    /// <param name="gasLimit">The gas limit of the transaction.</param>
    /// <param name="to">The to address of the transaction.</param>
    /// <param name="value">The value of the transaction.</param>
    /// <param name="data">The data of the transaction.</param>
    /// <param name="accessList">The access list for gas optimization.</param>
    /// <param name="v">The V component of the signature.</param>
    /// <param name="r">The R component of the signature.</param>
    /// <param name="s">The S component of the signature.</param>
    public TransactionEIP1559(
        ulong chainId,
        ulong nonce,
        BigInteger maxPriorityFeePerGas,
        BigInteger maxFeePerGas,
        ulong gasLimit,
        byte[] to,
        BigInteger value,
        byte[] data,
        AccessListItem[]? accessList,
        BigInteger v,
        BigInteger r,
        BigInteger s)
        : this(chainId, nonce, maxPriorityFeePerGas, maxFeePerGas, gasLimit, to, value, data, accessList, new RsvSignature(v, r, s))
    {
    }

    //

    /// <summary>
    /// The chain ID of the network.
    /// </summary>
    public ulong ChainId { get; }

    /// <summary>
    /// The nonce of the transaction.
    /// </summary>
    public ulong Nonce { get; }

    /// <summary>
    /// The maximum priority fee per gas (tip for miners/validators).
    /// </summary>
    public BigInteger MaxPriorityFeePerGas { get; }

    /// <summary>
    /// The maximum fee per gas (total fee cap).
    /// </summary>
    public BigInteger MaxFeePerGas { get; }

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
    /// The access list for gas optimization.
    /// </summary>
    public AccessListItem[] AccessList { get; }

    /// <summary>
    /// The signature of the transaction. Null if the transaction is unsigned.
    /// </summary>
    public RsvSignature? Signature { get; }

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

    //

    /// <summary>
    /// Validates that the transaction has the required fields to be considered valid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the transaction is missing required fields.</exception>
    public void Validate()
    {
        if (ChainId == 0)
        {
            throw new ArgumentException("Chain ID cannot be zero for EIP-1559 transactions.");
        }

        if (MaxPriorityFeePerGas.SignValue == 0 && MaxFeePerGas.SignValue == 0)
        {
            throw new ArgumentException("Both MaxPriorityFeePerGas and MaxFeePerGas cannot be zero.");
        }

        if (GasLimit == 0)
        {
            throw new ArgumentException("Gas limit cannot be zero.");
        }
    }

    /// <summary>
    /// Creates a signed transaction by adding a signature to an unsigned transaction.
    /// </summary>
    /// <param name="signature">The signature to add.</param>
    /// <returns>A signed transaction.</returns>
    public TransactionEIP1559 WithSignature(RsvSignature signature)
    {
        return new TransactionEIP1559(
            ChainId,
            Nonce,
            MaxPriorityFeePerGas,
            MaxFeePerGas,
            GasLimit,
            To,
            Value,
            Data,
            AccessList,
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
    public TransactionEIP1559 WithSignature(BigInteger v, BigInteger r, BigInteger s)
    {
        return WithSignature(new RsvSignature(v, r, s));
    }

    IEthereumTransaction IEthereumTransaction.WithSignature(RsvSignature signature)
    {
        return WithSignature(signature);
    }

    IEthereumTransaction IEthereumTransaction.WithSignature(BigInteger v, BigInteger r, BigInteger s)
    {
        return WithSignature(v, r, s);
    }

    /// <summary>
    /// Gets an empty EIP-1559 transaction instance.
    /// </summary>
    public static TransactionEIP1559 Empty => new TransactionEIP1559(
        chainId: 0,
        nonce: 0,
        maxPriorityFeePerGas: BigInteger.Zero,
        maxFeePerGas: BigInteger.Zero,
        gasLimit: 0,
        to: new byte[20], // Empty address (all zeros)
        value: BigInteger.Zero,
        data: new byte[0],
        accessList: null,
        signature: null
    );

    /// <summary>
    /// Creates an unsigned transaction.
    /// </summary>
    /// <param name="chainId">The chain ID of the network.</param>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <param name="maxPriorityFeePerGas">The maximum priority fee per gas (tip for miners/validators).</param>
    /// <param name="maxFeePerGas">The maximum fee per gas (total fee cap).</param>
    /// <param name="gasLimit">The gas limit of the transaction.</param>
    /// <param name="to">The to address of the transaction.</param>
    /// <param name="value">The value of the transaction.</param>
    /// <param name="data">The data of the transaction.</param>
    /// <param name="accessList">The access list for gas optimization.</param>
    /// <returns>An unsigned transaction.</returns>
    public static TransactionEIP1559 CreateUnsigned(
        ulong chainId,
        ulong nonce,
        BigInteger maxPriorityFeePerGas,
        BigInteger maxFeePerGas,
        ulong gasLimit,
        byte[] to,
        BigInteger value,
        byte[] data,
        AccessListItem[]? accessList = null)
    {
        return new TransactionEIP1559(
            chainId,
            nonce,
            maxPriorityFeePerGas,
            maxFeePerGas,
            gasLimit,
            to,
            value,
            data,
            accessList,
            null // No signature
        );
    }

    /// <summary>
    /// Gets the features of this transaction.
    /// </summary>  
    /// <returns>The transaction features as flags.</returns>
    public TransactionFeatures GetFeatures(ulong? _ = null)
    {
        var features = TransactionFeatures.TypedTransaction | TransactionFeatures.FeeMarket;

        // EIP-1559 always has EIP-155 replay protection via the chain ID field
        features |= TransactionFeatures.EIP155ReplayProtection;

        // Check if signed
        if (this.IsSigned(out var _))
        {
            features |= TransactionFeatures.Signed;
        }

        // Check for access list
        if (this.AccessList != null && this.AccessList.Length > 0)
        {
            features |= TransactionFeatures.AccessList;
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
}
