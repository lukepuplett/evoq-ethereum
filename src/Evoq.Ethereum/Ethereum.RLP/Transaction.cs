using System;
using System.Numerics;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Represents an Ethereum transaction.
/// </summary>
public struct Transaction
{
    /// <summary>
    /// The nonce of the transaction.
    /// </summary>
    public ulong Nonce;

    /// <summary>
    /// The gas price of the transaction.
    /// </summary>
    public BigInteger GasPrice;

    /// <summary>
    /// The gas limit of the transaction.
    /// </summary>
    public ulong GasLimit;

    /// <summary>
    /// The to address of the transaction.
    /// </summary>
    public byte[] To; // 20-byte address

    /// <summary>
    /// The value of the transaction.
    /// </summary>
    public BigInteger Value;

    /// <summary>
    /// The data of the transaction.
    /// </summary>
    public byte[] Data;

    /// <summary>
    /// The V component of the signature.
    /// </summary>
    public byte V; // Signature V (recovery ID, typically 27 or 28, or chain-adjusted)

    /// <summary>
    /// The R component of the signature.
    /// </summary>
    public BigInteger R; // Signature R

    /// <summary>
    /// The S component of the signature.
    /// </summary>
    public BigInteger S; // Signature S

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> struct.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <param name="gasPrice">The gas price of the transaction.</param>
    /// <param name="gasLimit">The gas limit of the transaction.</param>
    /// <param name="to">The to address of the transaction.</param>
    /// <param name="value">The value of the transaction.</param>
    /// <param name="data">The data of the transaction.</param>
    /// <param name="v">The V component of the signature.</param>
    /// <param name="r">The R component of the signature.</param>
    /// <param name="s">The S component of the signature.</param>
    public Transaction(
        ulong nonce,
        BigInteger gasPrice,
        ulong gasLimit,
        byte[] to,
        BigInteger value,
        byte[] data,
        byte v,
        BigInteger r,
        BigInteger s)
    {
        Nonce = nonce;
        GasPrice = gasPrice;
        GasLimit = gasLimit;
        To = to ?? throw new ArgumentNullException(nameof(to));
        Value = value;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        V = v;
        R = r;
        S = s;
    }
}