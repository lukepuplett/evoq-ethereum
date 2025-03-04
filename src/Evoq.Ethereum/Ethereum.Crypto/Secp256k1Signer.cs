using System;
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
/// Default implementation of the secp256k1 curve.
/// </summary>
public class Secp256k1Signer
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

        return new RsvSignature(v, r, s);
    }

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

    private (byte[] R, byte[] S, byte V) SignImpl(SigningPayload payload)
    {
        var bytes = payload.Data;

        var curve = SecNamedCurves.GetByName("secp256k1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var d = new BigInteger(1, _privateKey);
        var privKey = new ECPrivateKeyParameters(d, domain);
        var Q = domain.G.Multiply(d);

        // Use deterministic ECDSA with RFC 6979
        var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
        signer.Init(true, privKey);
        var signature = signer.GenerateSignature(bytes);
        var r = signature[0];
        var s = signature[1];

        // Rest of your logic (e.g., canonical s, recovery ID, etc.) remains the same
        var n = domain.N;
        var halfN = n.ShiftRight(1);
        if (s.CompareTo(halfN) > 0)
        {
            s = n.Subtract(s);
        }

        int recoveryId = -1;
        for (int recId = 0; recId < 2; recId++)
        {
            var Q_recovered = RecoverPublicKey(r, s, bytes, recId, domain);
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

        byte v = payload.IsEIP155
            ? (byte)(35 + 2 * payload.ChainId! + recoveryId)    // EIP-155
            : (byte)(27 + recoveryId);                          // Legacy

        var rBytes = To32ByteArray(r);
        var sBytes = To32ByteArray(s);

        return (rBytes, sBytes, v);
    }

    private static byte[] To32ByteArray(BigInteger bi)
    {
        var bytes = bi.ToByteArrayUnsigned();
        if (bytes.Length > 32)
        {
            throw new InvalidOperationException("BigInteger exceeds 32 bytes.");
        }
        var result = new byte[32];
        Array.Copy(bytes, 0, result, 32 - bytes.Length, bytes.Length);
        return result;
    }

    /// <summary>
    /// Recovers the public key from the signature components and message hash.
    /// </summary>
    private static ECPoint RecoverPublicKey(BigInteger r, BigInteger s, byte[] hash, int recoveryId, ECDomainParameters domain)
    {
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