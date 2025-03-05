namespace Evoq.Ethereum.RLP;

/// <summary>
/// Types implementing this interface can get transaction features.
/// </summary>
public interface ITransactionFeatures
{
    /// <summary>
    /// Gets the features of the transaction.
    /// </summary>
    /// <param name="chainId">Optional chain ID to check for EIP-155 replay protection.</param>
    /// <returns>The transaction features as flags.</returns>
    TransactionFeatures GetFeatures(ulong? chainId = null);
}
