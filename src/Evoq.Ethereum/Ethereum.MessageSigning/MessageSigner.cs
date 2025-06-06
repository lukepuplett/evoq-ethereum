using System;
using System.Text;
using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;

namespace Evoq.Ethereum.MessageSigning;

/// <summary>
/// A payload to sign which is prefixed with the EIP-191 personal sign prefix.
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
    private readonly IECSignPayload signer;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageSigner"/> class.
    /// </summary>
    public MessageSigner(IECSignPayload signer)
    {
        this.signer = signer;
    }

    //

    /// <summary>
    /// Signs a message.
    /// </summary>
    /// <param name="message">The message to sign.</param>
    /// <returns>The signature.</returns>
    public byte[] GetPersonalSignSignature(string message)
    {
        var payload = new PersonalSignSigningPayload(message);

        return GetSignature(this.signer, payload);
    }

    /// <summary>
    /// Signs a message.
    /// </summary>
    /// <param name="payload">The payload to sign.</param>
    /// <returns>The signature.</returns>
    public byte[] GetSignature(SigningPayload payload)
    {
        return this.signer.Sign(payload).ToByteArray();
    }

    //

    /// <summary>
    /// Creates a new instance of the <see cref="MessageSigner"/> class.
    /// </summary>
    /// <param name="privateKey">The private key to use.</param>
    /// <returns>The new instance.</returns>
    public static MessageSigner CreateDefault(byte[] privateKey)
    {
        return new MessageSigner(new Secp256k1Signer(privateKey));
    }

    /// <summary>
    /// Signs a message.
    /// </summary>
    /// <param name="signer">The signer to use.</param>
    /// <param name="message">The message to sign.</param>
    /// <returns>The signature.</returns>
    public static byte[] GetPersonalSignSignature(IECSignPayload signer, string message)
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
    public static byte[] GetSignature(IECSignPayload signer, SigningPayload payload)
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
    public static bool VerifyMessage(SigningPayload payload, IRsvSignature rsv, EthereumAddress expectedAddress)
    {
        return VerifyMessage(payload, rsv, expectedAddress, out _);
    }

    /// <summary>
    /// Verifies a message.
    /// </summary>
    /// <param name="payload">The payload to verify.</param>
    /// <param name="rsv">The signature to verify.</param>
    /// <param name="expectedAddress">The expected address.</param>
    /// <param name="message">The message that was signed.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool VerifyMessage(SigningPayload payload, IRsvSignature rsv, EthereumAddress expectedAddress, out string message)
    {
        var recovery = new Secp256k1Recovery();
        try
        {
            var recoveryId = Signing.GetRecoveryId(rsv.V);
            var publicKey = recovery.RecoverPublicKey(recoveryId, rsv, payload.Data, false);

            var recoveredAddress = EthereumAddress.FromPublicKey(new Hex(publicKey));

            if (recoveredAddress == expectedAddress)
            {
                message = "Signed by expected address: " + expectedAddress;
                return true;
            }
            else
            {
                message = "Signed by different address: " + recoveredAddress;
                return false;
            }
        }
        catch (InvalidOperationException invalidOp)
        {
            message = "Invalid or malformed signature: " + invalidOp.Message;
            return false;
        }
        catch (ArgumentException badArg)
        {
            message = "Invalid or malformed signature: " + badArg.Message;
            return false;
        }
        catch (NotImplementedException notImplemented) when (notImplemented.Message.Contains("Compressed public key support not yet implemented"))
        {
            message = "Invalid or malformed signature: " + notImplemented.Message;
            return false;
        }

        // allow other exceptions to bubble up
    }
}
