using System;
using System.Text;
using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;

namespace Evoq.Ethereum.MessageSigning;

/// <summary>
/// A personal message which offers signing and verification of messages.
/// </summary>
public class PersonalSign
{
    private const string OneNine = "0x19";
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
        byte[] messageBuf = GetEIP191PrefixedBytes();

        var hashedBytes = KeccakHash.ComputeHash(messageBuf);

        return this.signer.Sign(new SigningPayload { Data = hashedBytes }).ToByteArray();
    }

    // public bool VerifyMessage(byte[] signature)
    // {
    //     var hashedBytes = KeccakHash.ComputeHash(this.GetPrefixedBytes());
    //     var rsvSignature = RsvSignature.FromBytes(signature);

    //     var recoveredAddress = rsvSignature.RecoverMessage(hashedBytes);

    //     return recoveredAddress == this.signer.Address;
    // }

    //

    private byte[] GetEIP191PrefixedBytes()
    {
        // e.g. "0x19Ethereum Signed Message:\n12" as per EIP-191

        byte[] prefix19 = Hex.Parse(OneNine).ToByteArray();
        byte[] prefixString = Encoding.UTF8.GetBytes(ESM + this.MessageBytes.Length);

        var messageBuf = new byte[prefix19.Length + prefixString.Length + this.MessageBytes.Length];
        Buffer.BlockCopy(prefix19, 0, messageBuf, 0, prefix19.Length);
        Buffer.BlockCopy(prefixString, 0, messageBuf, prefix19.Length, prefixString.Length);
        Buffer.BlockCopy(this.MessageBytes, 0, messageBuf, prefix19.Length + prefixString.Length, this.MessageBytes.Length);

        return messageBuf;
    }
}
