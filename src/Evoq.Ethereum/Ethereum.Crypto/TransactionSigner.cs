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
    IEthereumTransaction GetSignedTransaction(IEthereumTransaction transaction);
}

/// <summary>
/// Default implementation of ISignBytes that uses the secp256k1 curve.
/// </summary>
public class TransactionSigner : ITransactionSigner
{
    private readonly ISignPayload signer;
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
        ISignPayload signer,
        IRlpTransactionEncoder rlpEncoder,
        ITransactionHasher hasher)
    {
        this.signer = signer;
        this.rlpEncoder = rlpEncoder;
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
        var hash = this.hasher.Hash(this.rlpEncoder.Encode(transaction));

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

        var hash = this.hasher.Hash(this.rlpEncoder.Encode(transaction));

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

    IEthereumTransaction ITransactionSigner.GetSignedTransaction(IEthereumTransaction transaction)
    {
        if (transaction is TransactionEIP1559 eip1559Transaction)
        {
            return GetSignedTransaction(eip1559Transaction);
        }
        else if (transaction is Transaction legacyTransaction)
        {
            return GetSignedTransaction(legacyTransaction);
        }

        throw new ArgumentException("Unsupported transaction type", nameof(transaction));
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
