using System;
using Evoq.Ethereum.Transactions;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Handles Ethereum JSON-RPC error responses and converts them to appropriate exceptions.
/// </summary>
internal static class JsonRpcErrorHandler
{
    /// <summary>
    /// Standard JSON-RPC error codes
    /// </summary>
    private static class ErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;

        // Ethereum specific
        public const int ExecutionError = 3;
        public const int ResourceNotFound = -32001;
        public const int ResourceUnavailable = -32002;
        public const int TransactionRejected = -32003;
        public const int MethodNotSupported = -32004;
        public const int LimitExceeded = -32005;
        public const int VersionNotSupported = -32006;
    }

    /// <summary>
    /// Attempts to convert any exception into a more specific exception type.
    /// </summary>
    /// <param name="ex">The exception to examine.</param>
    /// <param name="result">The specific exception if one could be determined.</param>
    /// <returns>True if a specific exception was identified, false otherwise.</returns>
    public static bool IsExpectedException(Exception ex, out Exception? result)
    {
        // Check the exception chain
        var current = ex;
        while (current != null)
        {
            // Handle provider errors with error codes
            if (current is JsonRpcProvidedErrorException rpcError)
            {
                if (IsCodeBasedException(rpcError, out result))
                {
                    return true;
                }
            }

            // Handle all exceptions (including JsonRpcRequestFailedException) with message-based detection
            if (IsMessageBasedException(current, out result))
            {
                return true;
            }

            current = current.InnerException;
        }

        result = null;
        return false;
    }

    private static bool IsCodeBasedException(JsonRpcProvidedErrorException ex, out Exception? result)
    {
        // First check error codes for broad categorization
        switch (ex.JsonRpcErrorCode)
        {
            case ErrorCodes.ExecutionError:
            case ErrorCodes.TransactionRejected:
                return IsExecutionException(ex, out result);

            case ErrorCodes.LimitExceeded:
                result = new LimitExceededException(ex.Message, ex);
                return true;

            case ErrorCodes.ResourceUnavailable:
                result = new ResourceUnavailableException(ex.Message, ex);
                return true;

            default:
                result = null;
                return false;
        }
    }

    private static bool IsExecutionException(Exception ex, out Exception? result)
    {
        var messages = string.Join(", ", ex.GetAllMessages());

        if (messages.Contains("out of gas", StringComparison.OrdinalIgnoreCase))
        {
            result = new OutOfGasException(messages, ex);
            return true;
        }

        if (messages.Contains("nonce too low", StringComparison.OrdinalIgnoreCase))
        {
            result = new InvalidNonceException(messages, ex);
            return true;
        }

        if (messages.Contains("reverted", StringComparison.OrdinalIgnoreCase))
        {
            result = new RevertedTransactionException(messages, ex);
            return true;
        }

        if (messages.Contains("insufficient funds", StringComparison.OrdinalIgnoreCase))
        {
            result = new InsufficientFundsException(messages, ex);
            return true;
        }

        result = null;
        return false;
    }

    private static bool IsMessageBasedException(Exception ex, out Exception? result)
    {
        var messages = string.Join(", ", ex.GetAllMessages());

        // Handle any cases that might appear with different error codes
        if (messages.Contains("gas price too low", StringComparison.OrdinalIgnoreCase))
        {
            result = new GasPriceTooLowException(messages, ex);
            return true;
        }

        result = null;
        return false;
    }
}