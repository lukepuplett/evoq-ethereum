using Evoq.Ethereum.Transactions;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Interface for RLP encoding.
/// </summary>
public interface IRlpTransactionEncoder
{
    /// <summary>
    /// Encodes an EIP-1559 transaction into RLP format.    
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <returns>The RLP encoded transaction.</returns>
    byte[] Encode(TransactionEIP1559 tx);

    /// <summary>
    /// Encodes a legacy transaction into RLP format.
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <param name="chainId">The chain ID to use for EIP-155 replay protection when signing an unsigned transaction.</param>
    /// <returns>The RLP encoded transaction.</returns>
    byte[] Encode(Transaction tx, ulong chainId = 0);

    /// <summary>
    /// Encodes a transaction for signing.
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <param name="chainId">The chain ID to use for EIP-155 replay protection.</param>
    /// <returns>The RLP encoded transaction for signing.</returns>
    byte[] EncodeForSigning(Transaction tx, ulong chainId = 0);

    /// <summary>
    /// Encodes a transaction for signing (excludes signature components).
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <returns>The RLP encoded transaction for signing.</returns>
    byte[] EncodeForSigning(TransactionEIP1559 tx);

}
