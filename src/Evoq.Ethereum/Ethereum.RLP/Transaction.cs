using System;
using System.Numerics;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Represents an Ethereum transaction.
/// </summary>
public struct TransactionEIP1559
{
    /// <summary>
    /// The chain ID of the network.
    /// </summary>
    public ulong ChainId;

    /// <summary>
    /// The nonce of the transaction.
    /// </summary>
    public ulong Nonce;

    /// <summary>
    /// The maximum priority fee per gas (tip for miners/validators).
    /// </summary>
    public BigInteger MaxPriorityFeePerGas;

    /// <summary>
    /// The maximum fee per gas (total fee cap).
    /// </summary>
    public BigInteger MaxFeePerGas;

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
    /// The access list for gas optimization.
    /// </summary>
    public AccessListItem[] AccessList;

    /// <summary>
    /// The signature of the transaction. Null if the transaction is unsigned.
    /// </summary>
    public RsvSignature? Signature;

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
        byte v,
        BigInteger r,
        BigInteger s)
        : this(chainId, nonce, maxPriorityFeePerGas, maxFeePerGas, gasLimit, to, value, data, accessList, new RsvSignature(v, r, s))
    {
    }

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
    public TransactionEIP1559 WithSignature(byte v, BigInteger r, BigInteger s)
    {
        return WithSignature(new RsvSignature(v, r, s));
    }

    /// <summary>
    /// Creates a signed transaction by adding a signature created from a recovery ID.
    /// </summary>
    /// <param name="recoveryId">The recovery ID (0 or 1).</param>
    /// <param name="r">The R component of the signature.</param>
    /// <param name="s">The S component of the signature.</param>
    /// <returns>A signed transaction.</returns>
    public TransactionEIP1559 WithSignatureFromRecoveryId(byte recoveryId, BigInteger r, BigInteger s)
    {
        // For EIP-1559, the v value is just the recovery ID (0 or 1)
        return WithSignature(recoveryId, r, s);
    }

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
    /// Gets the R component of the signature, or 0 if the transaction is unsigned.
    /// </summary>
    public BigInteger R => Signature?.R ?? BigInteger.Zero;

    /// <summary>
    /// Gets the S component of the signature, or 0 if the transaction is unsigned.
    /// </summary>
    public BigInteger S => Signature?.S ?? BigInteger.Zero;
}

/// <summary>
/// Represents an item in an EIP-2930 access list.
/// </summary>
public struct AccessListItem
{
    /// <summary>
    /// The address to access.
    /// </summary>
    public byte[] Address; // 20-byte address

    /// <summary>
    /// The storage keys to access.
    /// </summary>
    public byte[][] StorageKeys; // Each key is 32 bytes

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessListItem"/> struct.
    /// </summary>
    /// <param name="address">The address to access.</param>
    /// <param name="storageKeys">The storage keys to access.</param>
    public AccessListItem(byte[] address, byte[][] storageKeys)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        StorageKeys = storageKeys ?? Array.Empty<byte[]>();
    }
}

/// <summary>
/// Represents a legacy Ethereum transaction (pre-EIP-1559).
/// </summary>
public struct Transaction
{
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
        BigInteger r,
        BigInteger s)
        : this(nonce, gasPrice, gasLimit, to, value, data, new RsvSignature(v, r, s))
    {
    }

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
    public Transaction WithSignature(byte v, BigInteger r, BigInteger s)
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
    public Transaction WithSignatureFromRecoveryId(byte recoveryId, BigInteger r, BigInteger s, ulong chainId = 0)
    {
        return WithSignature(RsvSignature.FromRecoveryId(recoveryId, r, s, chainId));
    }

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
    /// Gets the R component of the signature, or 0 if the transaction is unsigned.
    /// </summary>
    public BigInteger R => Signature?.R ?? BigInteger.Zero;

    /// <summary>
    /// Gets the S component of the signature, or 0 if the transaction is unsigned.
    /// </summary>
    public BigInteger S => Signature?.S ?? BigInteger.Zero;
}