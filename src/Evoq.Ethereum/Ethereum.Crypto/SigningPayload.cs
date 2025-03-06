using System;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// A payload to sign.
/// </summary>
public class SigningPayload
{
    /// <summary>
    /// Whether the payload is an EIP-155 transaction.
    /// </summary>
    public bool IsEIP155 { get; set; } = true;

    /// <summary>
    /// The data to sign, e.g. the RLP-encoded transaction.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The chain ID.
    /// </summary>
    public BigInteger? ChainId { get; set; }
}
