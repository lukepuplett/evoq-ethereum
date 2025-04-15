using System;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace Evoq.Ethereum.Crypto;

/*
    https://github.com/ethereum/EIPs/blob/master/EIPS/eip-155.md

    If block.number >= FORK_BLKNUM and CHAIN_ID is available, then when computing the hash of
    a transaction for the purposes of signing, instead of hashing only six rlp encoded
    elements (nonce, gasprice, startgas, to, value, data), you SHOULD hash nine rlp encoded
    elements (nonce, gasprice, startgas, to, value, data, chainid, 0, 0). If you do, then the
    v of the signature MUST be set to {0,1} + CHAIN_ID * 2 + 35 where {0,1} is the parity of
    the y value of the curve point for which r is the x-value in the secp256k1 signing
    process. If you choose to only hash 6 values, then v continues to be set to {0,1} + 27
    as previously.

    If block.number >= FORK_BLKNUM and v = CHAIN_ID * 2 + 35 or v = CHAIN_ID * 2 + 36, then
    when computing the hash of a transaction for purposes of recovering, instead of hashing
    six rlp encoded elements (nonce, gasprice, startgas, to, value, data), hash nine rlp
    encoded elements (nonce, gasprice, startgas, to, value, data, chainid, 0, 0). The
    currently existing signature scheme using v = 27 and v = 28 remains valid and continues
    to operate under the same rules as it did previously.
*/

/// <summary>
/// Signs a payload using the secp256k1 curve.
/// </summary>
internal class Secp256k1Signer : IECSignPayload
{
    private readonly byte[] privateKey;

    /// <summary>
    /// Initializes a new instance of the Secp256k1Signer class.
    /// </summary>
    /// <param name="privateKey">The private key to use for signing (32 bytes).</param>
    public Secp256k1Signer(byte[] privateKey)
    {
        if (privateKey == null || privateKey.Length != 32)
        {
            throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));
        }

        this.privateKey = privateKey;
    }

    /// <summary>
    /// Signs the given payload using ECDSA with the secp256k1 curve.
    /// </summary>
    /// <param name="payload">The payload to sign.</param>
    /// <returns>The signature in RSV format.</returns>
    /// <exception cref="ArgumentException">Thrown when a chain ID is required for EIP-155 signatures but is not provided.</exception>
    public RsvSignature Sign(SigningPayload payload)
    {
        var (r, s, v) = this.SignPayloadInternal(payload);

        return new RsvSignature(v, r, s);
    }

    //

    private (BigInteger R, BigInteger S, BigInteger V) SignPayloadInternal(SigningPayload payload)
    {
        var payloadBytes = payload.Data;

        // Create curve parameters
        var curve = SecNamedCurves.GetByName("secp256k1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        // Create private key parameters
        var d = new BigInteger(1, privateKey);
        var privKey = new ECPrivateKeyParameters(d, domain);
        var Q = domain.G.Multiply(d);

        // Use deterministic ECDSA with RFC 6979
        var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
        signer.Init(true, privKey);

        // Generate signature
        var signature = signer.GenerateSignature(payloadBytes);
        var r = signature[0];
        var s = signature[1];

        // Canonicalize s; this means that if s is greater than half the order
        // of the curve, we subtract it from the order of the curve
        var n = domain.N;
        var halfN = n.ShiftRight(1);
        if (s.CompareTo(halfN) > 0)
        {
            s = n.Subtract(s);
        }

        // Recover the public key from the signature; the recovery ID is the
        // index of the public key that matches the signature; the index is
        // determined by the signer; the signer chooses the public key that
        // matches the signature; the index is either 0 or 1

        int recoveryId = -1;

        for (int recId = 0; recId < 2; recId++)
        {
            var Q_recovered = Secp256k1Recovery.RecoverPublicKey(r, s, payloadBytes, recId);
            if (Q_recovered.Equals(Q))
            {
                recoveryId = recId;
                break;
            }
        }

        if (recoveryId == -1)
        {
            throw new InvalidOperationException("Could not determine recovery ID.");
        }

        // Calculate the V value; this is the recovery ID plus 27 for legacy
        // transactions and 35 plus 2 times the chain ID plus the recovery ID
        // for EIP-155 transactions

        BigInteger bigRecoveryId = BigInteger.ValueOf(recoveryId);
        BigInteger v;

        if (payload is ChainAssociatedSigningPayload cp)
        {
            if (cp.ChainId == null)
            {
                throw new ArgumentException("Chain ID is required for EIP-155 signatures", nameof(payload));
            }

            v = Constants.Eip155BaseValue35
                .Add(cp.ChainId.Multiply(Constants.Eip155ChainIdMultiplier2))
                .Add(bigRecoveryId); // 35 + chainId * 2 + recoveryId
        }
        else
        {
            v = Constants.LegacyBaseValue27.Add(bigRecoveryId);
        }

        return (r, s, v);
    }
}

/// <summary>
/// Recovers a public key from a signature.
/// </summary>
internal class Secp256k1Recovery : IECRecoverPublicKey
{
    /// <summary>
    /// Initializes a new instance of the Secp256k1Recovery class.
    /// </summary>
    public Secp256k1Recovery()
    {
    }

    /// <summary>
    /// Recovers an address from a message.
    /// </summary>
    /// <param name="recoveryId">The recovery ID.</param>
    /// <param name="rsv">The RsvSignature.</param>
    /// <param name="messageHash">The original message.</param>
    /// <param name="shouldCompress">Whether the public key should be compressed.</param>
    /// <returns>The recovered public key.</returns>
    public byte[] RecoverPublicKey(int recoveryId, IRsvSignature rsv, byte[] messageHash, bool shouldCompress)
    {
        if (recoveryId < 0 || recoveryId > 1)
        {
            throw new ArgumentException("Invalid recovery ID");
        }

        var ecpoint = RecoverPublicKey(rsv.R, rsv.S, messageHash, recoveryId);

        return ecpoint.GetEncoded(shouldCompress);
    }

    //

    internal static ECPoint RecoverPublicKey(BigInteger r, BigInteger s, byte[] messageHash, int recoveryId)
    {
        var c = SecNamedCurves.GetByName("secp256k1");
        var domain = new ECDomainParameters(c.Curve, c.G, c.N, c.H);

        // Get curve parameters
        var curve = domain.Curve;
        var n = domain.N;
        var G = domain.G;
        var p = curve.Field.Characteristic;

        // Compute the x-coordinate (r)
        var x = r;

        // Compute y^2 = x^3 + 7 mod p (secp256k1 equation: y^2 = x^3 + ax + b, where a = 0, b = 7)
        var ySquared = x.ModPow(Big.Three, p).Add(Big.Seven).Mod(p);

        // Compute y using p ≡ 3 mod 4 property: y = (y^2)^((p+1)/4) mod p
        var y = ySquared.ModPow((p.Add(Big.One)).Divide(Big.Four), p);

        // Choose y based on recoveryId (0 for even, 1 for odd)
        BigInteger yRecovered = (Signing.Equals(y.Mod(Big.Two), Big.Zero) == (recoveryId % 2 == 0)) ? y : p.Subtract(y);

        // Create the point R
        var R = curve.CreatePoint(x, yRecovered);

        // Recover Q = r^(-1) * (s * R - z * G)
        var z = new BigInteger(1, messageHash);
        var rInv = r.ModInverse(n);
        var sR = R.Multiply(s);
        var zG = G.Multiply(z);
        var temp = sR.Add(zG.Negate());
        var Q_recovered = temp.Multiply(rInv);

        return Q_recovered;
    }
}