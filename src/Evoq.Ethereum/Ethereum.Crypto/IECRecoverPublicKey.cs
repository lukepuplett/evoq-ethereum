
namespace Evoq.Ethereum.Crypto;

/// <summary>
/// A message verifier.
/// </summary>
public interface IECRecoverPublicKey
{
    /// <summary>
    /// Recovers an address from a message.
    /// </summary>
    /// <param name="recoveryId">The recovery ID.</param>
    /// <param name="rsv">The RsvSignature.</param>
    /// <param name="messageHash">The original message.</param>
    /// <param name="shouldCompress">Whether the public key should be compressed.</param>
    /// <returns>The recovered public key.</returns>
    byte[] RecoverPublicKey(int recoveryId, IRsvSignature rsv, byte[] messageHash, bool shouldCompress);
}