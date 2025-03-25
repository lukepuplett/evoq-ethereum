using Evoq.Ethereum.Transactions;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// A result of an interaction with the Ethereum blockchain.
/// </summary>
/// <typeparam name="T">The type of the result. This is the type of the value returned by the method.</typeparam>
public class TransactionResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionResult{T}"/> class.
    /// </summary>
    /// <param name="receipt">The receipt of the transaction.</param>
    /// <param name="result">The result of the interaction.</param>
    public TransactionResult(TransactionReceipt receipt, T result)
    {
        this.Receipt = receipt;
        this.Result = result;
        this.Success = receipt.Success;
        this.Message = receipt.Success ? "OK" : "Failed";
    }

    //

    /// <summary>
    /// The receipt of the transaction.
    /// </summary>
    public TransactionReceipt Receipt { get; }

    /// <summary>
    /// The result of the interaction.
    /// </summary>
    public T Result { get; }

    /// <summary>
    /// Whether the interaction was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The message of the interaction.
    /// </summary>
    public string Message { get; init; } = "OK";
}
