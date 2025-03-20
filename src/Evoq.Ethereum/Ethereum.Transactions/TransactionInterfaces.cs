using Evoq.Ethereum.Crypto;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Defines the common properties and methods shared between Ethereum transaction types.
/// </summary>
public interface IWithSignature
{
    /// <summary>
    /// Creates a signed transaction by adding a signature to an unsigned transaction.
    /// </summary>
    /// <param name="signature">The signature to add.</param>
    /// <returns>A signed transaction.</returns>
    IEthereumTransaction WithSignature(RsvSignature signature);

    /// <summary>
    /// Creates a signed transaction by adding signature components to an unsigned transaction.
    /// </summary>
    /// <param name="v">The V component of the signature.</param>
    /// <param name="r">The R component of the signature.</param>
    /// <param name="s">The S component of the signature.</param>
    /// <returns>A signed transaction.</returns>
    IEthereumTransaction WithSignature(BigInteger v, BigInteger r, BigInteger s);
}

/// <summary>
/// Defines the common properties and methods shared between Ethereum transaction types.
/// </summary>
public interface ISigned
{
    /// <summary>
    /// The signature of the transaction. Null if the transaction is unsigned.
    /// </summary>
    RsvSignature? Signature { get; }

    /// <summary>
    /// Determines whether this transaction is signed.
    /// </summary>
    /// <returns>True if the transaction is signed; otherwise, false.</returns>
    bool IsSigned(out RsvSignature signature);
}

/// <summary>
/// Defines the common properties and methods shared between Ethereum transaction types.
/// </summary>
public interface IEthereumTransaction : ITransactionFeatures, IWithSignature, ISigned
{
    /// <summary>
    /// The nonce of the transaction.
    /// </summary>
    ulong Nonce { get; }

    /// <summary>
    /// The gas limit of the transaction.
    /// </summary>
    ulong GasLimit { get; }

    /// <summary>
    /// The to address of the transaction.
    /// </summary>
    byte[] To { get; }

    /// <summary>
    /// The value of the transaction.
    /// </summary>
    BigInteger Value { get; }

    /// <summary>
    /// The data of the transaction.
    /// </summary>
    byte[] Data { get; }

    /// <summary>
    /// Validates that the transaction has the required fields and data to be considered valid.
    /// </summary>
    void Validate();
}

/// <summary>
/// Defines the common properties and methods shared between Ethereum transaction types.
/// </summary>
public interface IEthereumTransactionType0 : IEthereumTransaction
{
    /// <summary>
    /// The gas price of the transaction.
    /// </summary>
    BigInteger GasPrice { get; }
}

/// <summary>
/// Defines the common properties and methods shared between Ethereum transaction types.
/// </summary>
public interface IEthereumTransactionType1 : IEthereumTransactionType0
{
    /// <summary>
    /// The chain ID of the network.
    /// </summary>
    public ulong ChainId { get; }

    /// <summary>
    /// The access list for gas optimization.
    /// </summary>
    public AccessListItem[] AccessList { get; }
}

/// <summary>
/// Defines the common properties and methods shared between Ethereum transaction types.
/// </summary>
public interface IEthereumTransactionType2 : IEthereumTransaction
{
    /// <summary>
    /// The chain ID of the network.
    /// </summary>
    public ulong ChainId { get; }

    /// <summary>
    /// The access list for gas optimization.
    /// </summary>
    public AccessListItem[] AccessList { get; }

    /// <summary>
    /// The maximum priority fee per gas (tip for miners/validators).
    /// </summary>
    public BigInteger MaxPriorityFeePerGas { get; }

    /// <summary>
    /// The maximum fee per gas (total fee cap).
    /// </summary>
    public BigInteger MaxFeePerGas { get; }
}