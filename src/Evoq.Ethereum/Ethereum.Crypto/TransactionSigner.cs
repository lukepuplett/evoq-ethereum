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
    Transaction GetSignedTransaction(Transaction transaction);

    /// <summary>
    /// Signs the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signed transaction.</returns>
    TransactionEIP1559 GetSignedTransaction(TransactionEIP1559 transaction);
}

/// <summary>
/// Default implementation of ISignBytes that uses the secp256k1 curve.
/// </summary>
public class TransactionSigner : ITransactionSigner
{
    private readonly ISignPayload signer;
    private readonly IRlpTransactionEncoder encoder;
    private readonly ITransactionHasher hasher;

    //

    /// <summary>
    /// Initializes a new instance of the DefaultTransactionSigner class.
    /// </summary>
    /// <param name="signer">The signer to use.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="hasher">The hasher to use.</param>
    public TransactionSigner(
        ISignPayload signer,
        IRlpTransactionEncoder encoder,
        ITransactionHasher hasher)
    {
        this.signer = signer;
        this.encoder = encoder;
        this.hasher = hasher;
    }

    //

    /// <summary>
    /// Signs the given byte array.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signature.</returns>
    public RsvSignature GetSignature(Transaction transaction)
    {
        var hash = this.hasher.Hash(this.encoder.Encode(transaction));

        return this.signer.Sign(new SigningPayload
        {
            Data = hash.ToByteArray(),
            IsEIP155 = false,
            ChainId = null
        });
    }

    /// <summary>
    /// Signs the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signed transaction.</returns>
    public Transaction GetSignedTransaction(Transaction transaction)
    {
        return transaction.WithSignature(GetSignature(transaction));
    }

    /// <summary>
    /// Signs the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signature.</returns>
    public RsvSignature GetSignature(TransactionEIP1559 transaction)
    {
        if (transaction.ChainId < 0)
        {
            throw new ArgumentException("ChainId must be greater than 0", nameof(transaction));
        }

        var hash = this.hasher.Hash(this.encoder.Encode(transaction));

        return this.signer.Sign(new SigningPayload
        {
            Data = hash.ToByteArray(),
            IsEIP155 = true,
            ChainId = BigInteger.ValueOf((long)transaction.ChainId)
        });
    }
    /// <summary>
    /// Signs the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signed transaction.</returns>
    public TransactionEIP1559 GetSignedTransaction(TransactionEIP1559 transaction)
    {
        return transaction.WithSignature(GetSignature(transaction));
    }

    //

    /// <summary>
    /// Creates a new TransactionSigner instance.
    /// </summary>
    /// <param name="privateKey">The private key to use.</param>
    /// <returns>The TransactionSigner instance.</returns>
    public static TransactionSigner Create(byte[] privateKey)
    {
        return new TransactionSigner(
            new Secp256k1Signer(privateKey),
            new RlpEncoder(),
            new DefaultTransactionHasher());
    }
}
