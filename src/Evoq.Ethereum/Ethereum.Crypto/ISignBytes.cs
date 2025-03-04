namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Represents a type that can sign byte arrays.
/// </summary>
public interface ISignBytes
{
    /// <summary>
    /// Signs the given byte array.
    /// </summary>
    RsvSignature Sign(byte[] data);
}
