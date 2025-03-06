using Evoq.Blockchain;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Default implementation of ITransactionHasher.
/// </summary>
internal class DefaultTransactionHasher : ITransactionHasher
{
    /// <summary>
    /// Hashes the given encoded transaction.
    /// </summary>
    /// <param name="encodedTransaction">The encoded transaction.</param>
    /// <returns>The hash.</returns>
    public Hex Hash(byte[] encodedTransaction) => new Hex(KeccakHash.ComputeHash(encodedTransaction));
}
