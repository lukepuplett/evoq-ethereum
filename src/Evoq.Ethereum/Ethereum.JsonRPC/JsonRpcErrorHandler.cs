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
        public const int ParseError = -32700;           // Invalid JSON
        public const int InvalidRequest = -32600;       // Malformed request or method not available
        public const int MethodNotFound = -32601;       // Method not available with provider
        public const int InvalidParams = -32602;        // Invalid request parameters
        public const int InternalErrorOrRevert = -32603;// Node revert or malformed request

        // Ethereum specific
        public const int InvalidInput = -32000;         // Missing or invalid parameters
        public const int ResourceNotFound = -32001;     // Method not supported by provider
        public const int ResourceUnavailable = -32002;  // Requested resource not available
        public const int TransactionRejected = -32003;  // Transaction creation failed
        public const int MethodNotSupported = -32004;   // Method is not implemented
        public const int LimitExceeded = -32005;        // Request exceeds defined limit
        public const int VersionNotSupported = -32006;  // JSON-RPC version not supported
        public const int GasTooLow = -32015;           // Insufficient gas limit
        public const int NonceTooLow = -32016;         // Transaction nonce too low
        public const int NonceTooHigh = -32017;        // Transaction nonce too high
        public const int InsufficientFunds = -32020;    // Not enough ETH for transaction
        public const int GasPriceTooLow = -32021;      // Gas price below node minimum
        public const int TransactionUnderpriced = -32023; // Gas price too low for network
        public const int InvalidSignature = -32030;     // Invalid transaction signature

        // Additional Ethereum-specific error codes
        public const int UnknownError = -32007;        // Unspecified issue occurred
        public const int ServerError = -32008;         // Generic server-side error
        public const int NetworkError = -32009;        // Network-related error
        public const int InvalidBlock = -32011;        // Invalid block number or hash
        public const int InvalidTransaction = -32012;   // Invalid transaction data
        public const int InvalidAccount = -32013;      // Invalid account address
        public const int InvalidContract = -32014;     // Invalid contract address or code
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
            if (current is JsonRpcProviderErrorException rpcError)
            {
                if (IsCodeBasedException(rpcError, out result))
                {
                    return true;
                }
            }

            if (IsMessageBasedException(current, out result))
            {
                return true;
            }

            current = current.InnerException;
        }

        result = null;
        return false;
    }

    //

    private static bool IsCodeBasedException(JsonRpcProviderErrorException ex, out Exception? result)
    {
        // First check error codes for broad categorization
        switch (ex.JsonRpcErrorCode)
        {
            case ErrorCodes.InvalidInput:  // -32000
            case ErrorCodes.TransactionRejected:
                return IsExecutionException(ex, out result);

            case ErrorCodes.LimitExceeded:
                result = new LimitExceededException(ex.Message, ex);
                return true;

            case ErrorCodes.ResourceUnavailable:
                result = new ResourceUnavailableException(ex.Message, ex);
                return true;

            case ErrorCodes.NonceTooLow:
            case ErrorCodes.NonceTooHigh:
                result = new InvalidNonceException(ex.Message, ex);
                return true;

            case ErrorCodes.GasTooLow:
                result = new OutOfGasException(ex.Message, ex);
                return true;

            case ErrorCodes.InsufficientFunds:
                result = new InsufficientFundsException(ex.Message, ex);
                return true;

            case ErrorCodes.GasPriceTooLow:
            case ErrorCodes.TransactionUnderpriced:
                result = new GasPriceTooLowException(ex.Message, ex);
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