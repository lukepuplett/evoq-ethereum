using System;
using Evoq.Ethereum.RLP;
using Evoq.Ethereum.Transactions;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Default implementation of ISignBytes that uses the secp256k1 curve.
/// </summary>
public class TransactionSigner
{
    private readonly ITransactionSigner _signer;
    private readonly IRlpTransactionEncoder _encoder;
    private readonly ITransactionHasher _hasher;
    private readonly byte[] _privateKey;

    //

    /// <summary>
    /// Initializes a new instance of the DefaultTransactionSigner class.
    /// </summary>
    /// <param name="signer">The signer to use.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="hasher">The hasher to use.</param>
    /// <param name="privateKey">The private key to use.</param>
    public TransactionSigner(
        ITransactionSigner signer,
        IRlpTransactionEncoder encoder,
        ITransactionHasher hasher,
        byte[] privateKey)
    {
        _signer = signer;
        _encoder = encoder;
        _hasher = hasher;
        _privateKey = privateKey;
    }

    //

    /// <summary>
    /// Signs the given byte array.
    /// </summary>
    /// <param name="transaction">The transaction to sign.</param>
    /// <returns>The signature.</returns>
    public RsvSignature GetSignature(Transaction transaction)
    {
        var hash = _hasher.Hash(_encoder.Encode(transaction));

        return _signer.Sign(new SigningPayload
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

        var hash = _hasher.Hash(_encoder.Encode(transaction));

        return _signer.Sign(new SigningPayload
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
            new TransactionHasher(),
            privateKey);
    }
}
