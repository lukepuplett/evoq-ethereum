using System;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Defines the features that an Ethereum transaction may have.
/// </summary>
[Flags]
public enum TransactionFeatures
{
    /// <summary>
    /// No special features.
    /// </summary>
    None = 0,

    /// <summary>
    /// Transaction has a signature.
    /// </summary>
    Signed = 1 << 0,

    /// <summary>
    /// Transaction has EIP-155 replay protection.
    /// </summary>
    EIP155ReplayProtection = 1 << 1,

    /// <summary>
    /// Transaction is an EIP-2930 access list transaction (type 1).
    /// </summary>
    AccessList = 1 << 2,

    /// <summary>
    /// Transaction is an EIP-1559 fee market transaction (type 2).
    /// </summary>
    FeeMarket = 1 << 3,

    /// <summary>
    /// Transaction is a contract creation (no 'to' address).
    /// </summary>
    ContractCreation = 1 << 4,

    /// <summary>
    /// Transaction has zero value transfer.
    /// </summary>
    ZeroValue = 1 << 5,

    /// <summary>
    /// Transaction has data payload.
    /// </summary>
    HasData = 1 << 6,

    /// <summary>
    /// Legacy transaction format (pre-EIP-2718).
    /// </summary>
    Legacy = 1 << 7,

    /// <summary>
    /// Transaction uses the EIP-2718 typed transaction envelope.
    /// </summary>
    TypedTransaction = 1 << 8
}