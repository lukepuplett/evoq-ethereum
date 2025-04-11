using System;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// A payload to sign.
/// </summary>
public class SigningPayload
{
    /// <summary>
    /// The data to sign, e.g. the RLP-encoded transaction.
    /// </summary>
    public byte[] Data { get; init; } = Array.Empty<byte>();
}

/// <summary>
/// A payload to sign.
/// </summary>
public class ChainAssociatedSigningPayload : SigningPayload
{
    /// <summary>
    /// The chain ID.
    /// </summary>
    public BigInteger? ChainId { get; set; }
}
