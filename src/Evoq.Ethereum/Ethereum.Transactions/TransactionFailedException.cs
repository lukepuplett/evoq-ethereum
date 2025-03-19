using System;
using Evoq.Blockchain;

namespace Evoq.Ethereum.Transactions;


/// <summary>
/// Exception thrown when a transaction fails (has status 0).
/// </summary>
public class TransactionFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionFailedException"/> class.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction that failed.</param>
    /// <param name="receipt">The receipt of the transaction that failed.</param>
    public TransactionFailedException(Hex transactionHash, TransactionReceipt receipt)
        : base($"Transaction {transactionHash} failed.")
    {
        Receipt = receipt;
    }

    /// <summary>
    /// Gets the receipt of the transaction that failed.
    /// </summary>
    public TransactionReceipt Receipt { get; }
}
