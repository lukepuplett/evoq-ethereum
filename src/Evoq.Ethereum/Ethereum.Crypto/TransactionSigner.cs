using Evoq.Ethereum.RLP;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Default implementation of ISignBytes that uses the secp256k1 curve.
/// </summary>
public class TransactionSigner
{
    private readonly ISignBytes _signer;
    private readonly IRlpTransactionEncoder _encoder;
    private readonly byte[] _privateKey;

    //

    /// <summary>
    /// Initializes a new instance of the DefaultTransactionSigner class.
    /// </summary>
    /// <param name="signer">The signer to use.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="privateKey">The private key to use.</param>
    public TransactionSigner(ISignBytes signer, IRlpTransactionEncoder encoder, byte[] privateKey)
    {
        _signer = signer;
        _encoder = encoder;
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
        return _signer.Sign(_encoder.Encode(transaction));
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
        return _signer.Sign(_encoder.Encode(transaction));
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
        return new TransactionSigner(new Secp256k1Signer(privateKey), new RlpEncoder(), privateKey);
    }
}
