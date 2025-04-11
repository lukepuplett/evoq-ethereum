using System;
using System.Text;
using Evoq.Ethereum.Crypto;

namespace Evoq.Ethereum.MessageSigning;

/// <summary>
/// A personal message which offers signing and verification of messages.
/// </summary>
public class PersonalSign
{
    private const string ESM = "Ethereum Signed Message:\n";
    private readonly IECSignPayload signer;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalSign"/> class.
    /// </summary>
    /// <param name="utf8">The message to sign.</param>
    /// <param name="signer">The signer to use.</param>
    public PersonalSign(string utf8, IECSignPayload signer)
    {
        this.MessageBytes = Encoding.UTF8.GetBytes(utf8);
        this.signer = signer;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalSign"/> class.
    /// </summary>
    /// <param name="message">The message to sign.</param>
    /// <param name="signer">The signer to use.</param>
    public PersonalSign(byte[] message, IECSignPayload signer)
    {
        this.MessageBytes = message;
        this.signer = signer;
    }

    //

    /// <summary>
    /// The message to sign.
    /// </summary>
    public byte[] MessageBytes { get; }

    //

    /// <summary>
    /// Gets the signature of the message.
    /// </summary>
    /// <returns>The signature.</returns>
    public byte[] GetSignature()
    {
        byte[] messageBuf = GetEIP191PrefixedBytes(this.MessageBytes);

        var hashedBytes = KeccakHash.ComputeHash(messageBuf);

        return this.signer.Sign(new SigningPayload { Data = hashedBytes }).ToByteArray();
    }

    //

    internal static byte[] GetEIP191PrefixedBytes(byte[] messageBytes)
    {
        // e.g. "0x19Ethereum Signed Message:\n12" as per EIP-191

        byte[] prefix19 = new byte[] { 0x19 };
        byte[] prefixString = Encoding.UTF8.GetBytes(ESM + messageBytes.Length);

        var messageBuf = new byte[prefix19.Length + prefixString.Length + messageBytes.Length];
        Buffer.BlockCopy(prefix19, 0, messageBuf, 0, prefix19.Length);
        Buffer.BlockCopy(prefixString, 0, messageBuf, prefix19.Length, prefixString.Length);
        Buffer.BlockCopy(messageBytes, 0, messageBuf, prefix19.Length + prefixString.Length, messageBytes.Length);

        return messageBuf;
    }
}
