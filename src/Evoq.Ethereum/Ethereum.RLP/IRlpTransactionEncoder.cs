using Evoq.Ethereum.Transactions;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Interface for RLP encoding.
/// </summary>
public interface IRlpTransactionEncoder
{
    /// <summary>
    /// Encodes a legacy transaction into RLP format.
    /// </summary>
    /// <param name="type0">The transaction to encode.</param>
    /// <param name="chainId">The chain ID to use for EIP-155 replay protection when signing an unsigned transaction.</param>
    /// <returns>The RLP encoded transaction.</returns>
    byte[] Encode(IEthereumTransactionType0 type0, ulong chainId = 0);

    /// <summary>
    /// Encodes a transaction for signing.
    /// </summary>
    /// <param name="type0">The transaction to encode.</param>
    /// <param name="chainId">The chain ID to use for EIP-155 replay protection.</param>
    /// <returns>The RLP encoded transaction for signing.</returns>
    byte[] EncodeForSigning(IEthereumTransactionType0 type0, ulong chainId = 0);

    //

    /// <summary>
    /// Encodes an EIP-1559 transaction into RLP format.    
    /// </summary>
    /// <param name="type2">The transaction to encode.</param>
    /// <returns>The RLP encoded transaction.</returns>
    byte[] Encode(IEthereumTransactionType2 type2);

    /// <summary>
    /// Encodes a transaction for signing (excludes signature components).
    /// </summary>
    /// <param name="type2">The transaction to encode.</param>
    /// <returns>The RLP encoded transaction for signing.</returns>
    byte[] EncodeForSigning(IEthereumTransactionType2 type2);

}
