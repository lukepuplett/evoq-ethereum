using System;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Flags representing features of a transaction.
/// </summary>
[Flags]
public enum TransactionFeatures
{
    /// <summary>
    /// No features.
    /// </summary>
    None = 0,

    /// <summary>
    /// Legacy transaction format (pre-EIP-1559).
    /// </summary>
    Legacy = 1 << 0,

    /// <summary>
    /// Transaction is signed.
    /// </summary>
    Signed = 1 << 1,

    /// <summary>
    /// Transaction has EIP-155 replay protection.
    /// </summary>
    EIP155ReplayProtection = 1 << 2,

    /// <summary>
    /// Transaction is a contract creation (no 'to' address).
    /// </summary>
    ContractCreation = 1 << 3,

    /// <summary>
    /// Transaction has a zero value.
    /// </summary>
    ZeroValue = 1 << 4,

    /// <summary>
    /// Transaction has data payload.
    /// </summary>
    HasData = 1 << 5,

    /// <summary>
    /// Transaction is a typed transaction (EIP-2718).
    /// </summary>
    TypedTransaction = 1 << 6,

    /// <summary>
    /// Transaction uses the fee market (EIP-1559).
    /// </summary>
    FeeMarket = 1 << 7,

    /// <summary>
    /// Transaction has an access list (EIP-2930).
    /// </summary>
    AccessList = 1 << 8,
}