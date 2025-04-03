using System;
using Evoq.Blockchain;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Exception thrown when a transaction cannot be found within the specified timeout.
/// </summary>
public class ReceiptNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptNotFoundException"/> class.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction that was not found.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReceiptNotFoundException(Hex transactionHash, Exception? innerException = null)
        : base($"Transaction {transactionHash} was not found within the specified timeout.", innerException)
    {
        this.TransactionHash = transactionHash;
    }

    //

    /// <summary>
    /// Gets the transaction hash.
    /// </summary>
    public Hex TransactionHash { get; }
}
