using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum;

/// <summary>
/// Base exception for Ethereum-related errors.
/// </summary>
[Serializable]
public abstract class EthereumException : Exception
{
    /// <summary>
    /// Initializes a new instance of the EthereumException class with a specified error message.
    /// </summary>
    public EthereumException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the EthereumException class with a specified error message and inner exception.
    /// </summary>
    public EthereumException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Initializes a new instance of the EthereumException class with serialized data.
    /// </summary>
    public EthereumException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    /// Gets or sets the hash of the transaction that caused the exception.
    /// </summary>
    public string? TransactionHash { get; init; }
}
