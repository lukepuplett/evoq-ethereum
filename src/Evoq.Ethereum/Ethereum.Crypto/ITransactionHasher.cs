using Evoq.Blockchain;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Hashes a transaction.
/// </summary>
public interface ITransactionHasher
{
    /// <summary>
    /// Hashes the given encoded transaction.
    /// </summary>
    /// <param name="encodedTransaction">The encoded transaction.</param>
    /// <returns>The hash.</returns>
    Hex Hash(byte[] encodedTransaction);
}
