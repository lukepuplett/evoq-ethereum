using System;
using Evoq.Blockchain;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace Evoq.Ethereum.Crypto;

public class SigningPayload
{
    public bool IsEIP155 { get; set; } = true;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long? ChainId { get; set; }
}

/// <summary>
/// Signs a payload.
/// </summary>
public interface ITransactionSigner
{
    /// <summary>
    /// Signs the given payload.
    /// </summary>
    /// <param name="payload">The payload to sign.</param>
    /// <returns>The signature in RSV format.</returns>
    RsvSignature Sign(SigningPayload payload);
}

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
/// Default implementation of the secp256k1 curve.
/// </summary>
public class Secp256k1Signer : ITransactionSigner
{
    private readonly byte[] _privateKey;
    private static readonly BigInteger ONE = BigInteger.ValueOf(1);
    private static readonly BigInteger THREE = BigInteger.ValueOf(3);
    private static readonly BigInteger FOUR = BigInteger.ValueOf(4);
    private static readonly BigInteger SEVEN = BigInteger.ValueOf(7);

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

        _privateKey = privateKey;
    }

    /// <summary>
    /// Signs the given payload using ECDSA with the secp256k1 curve.
    /// </summary>
    /// <param name="payload">The payload to sign.</param>
    /// <returns>The signature in RSV format.</returns>
    /// <exception cref="ArgumentException">Thrown when a chain ID is required for EIP-155 signatures but is not provided.</exception>
    public RsvSignature Sign(SigningPayload payload)
    {
        if (payload.IsEIP155 && payload.ChainId == null)
        {
            throw new ArgumentException("Chain ID is required for EIP-155 signatures", nameof(payload));
        }

        var (r, s, v) = SignImpl(payload);

        // need to convert r and s to hex values with padding
        Hex paddedR = r.ToHex(true).ToPaddedHex(32);
        Hex paddedS = s.ToHex(true).ToPaddedHex(32);

        return new RsvSignature(v, paddedR, paddedS);
    }

    [Obsolete("Needs to return a Hex but creating a byte array from ECPoint is not trivial")]
    public ECPoint RecoverPublicKey(byte[] hash, byte[] r, byte[] s, byte v, long? chainId)
    {
        var curve = SecNamedCurves.GetByName("secp256k1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        int recoveryId = chainId.HasValue ? v - (int)(chainId.Value * 2 + 35) : v - 27;
        if (recoveryId < 0 || recoveryId > 1)
        {
            throw new ArgumentException("Invalid recovery ID");
        }

        return RecoverPublicKey(new BigInteger(1, r), new BigInteger(1, s), hash, recoveryId, domain);
    }

    //

    private (BigInteger R, BigInteger S, byte V) SignImpl(SigningPayload payload)
    {
        var payloadBytes = payload.Data;

        // Create curve parameters
        var curve = SecNamedCurves.GetByName("secp256k1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        // Create private key parameters
        var d = new BigInteger(1, _privateKey);
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
            var Q_recovered = RecoverPublicKey(r, s, payloadBytes, recId, domain);
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
        byte v = payload.IsEIP155
            ? (byte)(35 + 2 * payload.ChainId! + recoveryId)    // EIP-155
            : (byte)(27 + recoveryId);                          // Legacy

        return (r, s, v);
    }

    // private static byte[] To32ByteArray(BigInteger bi)
    // {
    //     var bytes = bi.ToByteArrayUnsigned();
    //     if (bytes.Length > 32)
    //     {
    //         throw new InvalidOperationException("BigInteger exceeds 32 bytes.");
    //     }
    //     var result = new byte[32];
    //     Array.Copy(bytes, 0, result, 32 - bytes.Length, bytes.Length);
    //     return result;
    // }

    private static ECPoint RecoverPublicKey(BigInteger r, BigInteger s, byte[] hash, int recoveryId, ECDomainParameters domain)
    {
        // Get curve parameters
        var curve = domain.Curve;
        var n = domain.N;
        var G = domain.G;
        var p = curve.Field.Characteristic;

        // Compute the x-coordinate (r)
        var x = r;

        // Compute y^2 = x^3 + 7 mod p (secp256k1 equation: y^2 = x^3 + ax + b, where a = 0, b = 7)
        var ySquared = x.ModPow(THREE, p).Add(SEVEN).Mod(p);

        // Compute y using p ≡ 3 mod 4 property: y = (y^2)^((p+1)/4) mod p
        var y = ySquared.ModPow((p.Add(ONE)).Divide(FOUR), p);

        // Choose y based on recoveryId (0 for even, 1 for odd)
        BigInteger yRecovered = (y.Mod(BigInteger.Two).Equals(BigInteger.Zero) == (recoveryId % 2 == 0)) ? y : p.Subtract(y);

        // Create the point R
        var R = curve.CreatePoint(x, yRecovered);

        // Recover Q = r^(-1) * (s * R - z * G)
        var z = new BigInteger(1, hash);
        var rInv = r.ModInverse(n);
        var sR = R.Multiply(s);
        var zG = G.Multiply(z);
        var temp = sR.Add(zG.Negate());
        var Q_recovered = temp.Multiply(rInv);

        return Q_recovered;
    }
}