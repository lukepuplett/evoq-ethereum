namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Signs a payload.
/// </summary>
public interface ISignPayload
{
    /// <summary>
    /// Signs the given payload.
    /// </summary>
    /// <param name="payload">The payload to sign.</param>
    /// <returns>The signature in RSV format.</returns>
    RsvSignature Sign(SigningPayload payload);
}
