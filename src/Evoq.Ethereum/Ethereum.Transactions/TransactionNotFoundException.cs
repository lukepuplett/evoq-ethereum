using System;
using Evoq.Blockchain;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Exception thrown when a transaction cannot be found within the specified timeout.
/// </summary>
public class TransactionNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionNotFoundException"/> class.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction that was not found.</param>
    public TransactionNotFoundException(Hex transactionHash)
        : base($"Transaction {transactionHash} was not found within the specified timeout.") { }
}
