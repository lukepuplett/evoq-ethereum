using System;
using System.Text;
using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;

namespace Evoq.Ethereum.MessageSigning;

/// <summary>
/// A payload to sign.
/// </summary>
public class PersonalSignSigningPayload : SigningPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalSignSigningPayload"/> class.
    /// </summary>
    /// <param name="message">The message to sign.</param>
    public PersonalSignSigningPayload(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);

        this.Data = KeccakHash.ComputeHash(messageBytes);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalSignSigningPayload"/> class.
    /// </summary>
    /// <param name="messageBytes">The message to sign.</param>
    public PersonalSignSigningPayload(byte[] messageBytes)
    {
        var prefixedBytes = PersonalSign.GetEIP191PrefixedBytes(messageBytes);

        this.Data = KeccakHash.ComputeHash(prefixedBytes);
    }
}

/// <summary>
/// A class that can sign and verify messages.
/// </summary>
public class MessageSigner
{
    /// <summary>
    /// Signs a message.
    /// </summary>
    /// <param name="signer">The signer to use.</param>
    /// <param name="message">The message to sign.</param>
    /// <returns>The signature.</returns>
    public byte[] GetPersonalSignSignature(IECSignPayload signer, string message)
    {
        var payload = new PersonalSignSigningPayload(message);

        return GetSignature(signer, payload);
    }

    /// <summary>
    /// Signs a message.
    /// </summary>
    /// <param name="signer">The signer to use.</param>
    /// <param name="payload">The payload to sign.</param>
    /// <returns>The signature.</returns>
    public byte[] GetSignature(IECSignPayload signer, SigningPayload payload)
    {
        return signer.Sign(payload).ToByteArray();
    }

    /// <summary>
    /// Verifies a message.
    /// </summary>
    /// <param name="payload">The payload to verify.</param>
    /// <param name="rsv">The signature to verify.</param>
    /// <param name="expectedAddress">The expected address.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public bool VerifyMessage(SigningPayload payload, IRsvSignature rsv, EthereumAddress expectedAddress)
    {
        var recovery = new Secp256k1Recovery();
        try
        {
            var recoveryId = Signing.GetRecoveryId(rsv.V);
            var publicKey = recovery.RecoverPublicKey(recoveryId, rsv, payload.Data, false);

            var recoveredAddress = EthereumAddress.FromPublicKey(new Hex(publicKey));

            return recoveredAddress == expectedAddress;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotImplementedException notImplemented) when (notImplemented.Message.Contains("Compressed public key support not yet implemented"))
        {
            return false;
        }

        // allow other exceptions to bubble up
    }
}
