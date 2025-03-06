namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Interface for transaction types that can report their features.
/// </summary>
public interface ITransactionFeatures
{
    /// <summary>
    /// Gets the features of this transaction.
    /// </summary>
    /// <param name="chainId">Optional chain ID to check for EIP-155 replay protection on the signature.</param>
    /// <returns>The transaction features as flags.</returns>
    TransactionFeatures GetFeatures(ulong? chainId = null);
}
