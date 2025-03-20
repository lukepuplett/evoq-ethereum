using System;
using Evoq.Ethereum.RLP;
using Evoq.Ethereum.Transactions;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Interface for signing transactions.
/// </summary>
public interface ITransactionSigner
{
    /// <summary>
    /// Signs the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signed transaction.</returns>
    T GetSignedTransaction<T>(T transaction) where T : IEthereumTransaction;
}

/// <summary>
/// Default implementation of ITransactionSigner that uses the secp256k1 curve.
/// </summary>
public class TransactionSigner : ITransactionSigner
{
    private readonly IECSignPayload signer;
    private readonly IRlpTransactionEncoder rlpEncoder;
    private readonly ITransactionHasher hasher;

    //

    /// <summary>
    /// Initializes a new instance of the DefaultTransactionSigner class.
    /// </summary>
    /// <param name="signer">The signer to use.</param>
    /// <param name="rlpEncoder">The RLP encoder to use.</param>
    /// <param name="hasher">The hasher to use.</param>
    public TransactionSigner(
        IECSignPayload signer,
        IRlpTransactionEncoder rlpEncoder,
        ITransactionHasher hasher)
    {
        this.signer = signer;
        this.rlpEncoder = rlpEncoder;
        this.hasher = hasher;
    }

    //

    /// <summary>
    /// Signs the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signed transaction.</returns>
    public T GetSignedTransaction<T>(T transaction) where T : IEthereumTransaction
    {
        if (transaction is IEthereumTransactionType0 type0)
        {
            var sig = this.GetSignature(type0);

            return (T)transaction.WithSignature(sig);
        }
        else if (transaction is IEthereumTransactionType2 type2)
        {
            var sig = this.GetSignature(type2);

            return (T)transaction.WithSignature(sig);
        }

        throw new ArgumentException($"Invalid transaction type '{transaction.GetType().Name}'", nameof(transaction));
    }

    //

    /// <summary>
    /// Signs the given byte array.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signature.</returns>
    private RsvSignature GetSignature(IEthereumTransactionType0 transaction)
    {
        var hash = this.hasher.Hash(this.rlpEncoder.Encode(transaction));

        return this.signer.Sign(new SigningPayload
        {
            Data = hash
        });
    }

    /// <summary>
    /// Signs the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signature.</returns>
    private RsvSignature GetSignature(IEthereumTransactionType2 transaction)
    {
        if (transaction.ChainId < 0)
        {
            throw new ArgumentException("ChainId must be greater than 0", nameof(transaction));
        }

        var hash = this.hasher.Hash(this.rlpEncoder.Encode(transaction));

        return this.signer.Sign(new ChainAssociatedSigningPayload
        {
            Data = hash,
            ChainId = BigInteger.ValueOf((long)transaction.ChainId)
        });
    }

    T ITransactionSigner.GetSignedTransaction<T>(T transaction)
    {
        return GetSignedTransaction(transaction);
    }

    //

    /// <summary>
    /// Creates a new TransactionSigner instance using the default secp256k1 curve.
    /// </summary>
    /// <param name="privateKey">The private key to use.</param>
    /// <returns>The TransactionSigner instance.</returns>
    public static TransactionSigner CreateDefault(byte[] privateKey)
    {
        return new TransactionSigner(
            new Secp256k1Signer(privateKey),
            new RlpEncoder(),
            new DefaultTransactionHasher());
    }
}
