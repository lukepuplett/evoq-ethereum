using System.Threading.Tasks;

namespace Evoq.Ethereum;

/// <summary>
/// The response from the nonce store communicating the result of a potential nonce rollback.
/// </summary>
public enum NonceRollbackResponse
{
    /// <summary>
    /// The nonce record was not found.
    /// </summary>
    NonceNotFound,
    /// <summary>
    /// The nonce record was removed and no gap was detected.
    /// </summary>
    RemovedOkay,
    /// <summary>
    /// The nonce record was removed and this left a gap.
    /// </summary>
    RemovedGapDetected,
    /// <summary>
    /// The nonce record was not removed this time and the transaction should be retried with the same nonce.
    /// </summary>
    NotRemovedShouldRetry,
    /// <summary>
    /// The nonce record was not removed this time due to an error.
    /// </summary>
    NotRemovedDueToError,
    /// <summary>
    /// The nonce record was not removed because the transaction executed to some extent.
    /// </summary>
    NotRemovedGasSpent,
}

/// <summary>
/// Provides a store for nonces for a given sender address.
/// </summary>
public interface INonceStore
{
    /// <summary>
    /// Reserves the next nonce for the given sender address and returns it for immediate use.
    /// </summary>
    /// <remarks>
    /// This should be called just before sending a transaction to get the nonce to use immediately. Implementers
    /// should ensure that the stored nonce is synchronised or shared among all processes and incremented atomically.
    /// </remarks>
    /// <returns>The new nonce value which should be used immediately.</returns>
    Task<uint> BeforeSubmissionAsync();

    /// <summary>
    /// Call this when a transaction fails to submit due to a bug, network or unknown error.
    /// </summary>
    /// <remarks>
    /// Callers should impose a wait period before retrying. This method should not wait.
    /// This should be called if there is a failure to send a transaction, but not when the failure is due to
    /// a nonce too low. Implementers should consider whether to remove the nonce file or to signal that the
    /// submission should be retried with the same nonce.
    /// Implementers should log a warning upon removing the nonce, and should log a critical error
    /// if the nonce cannot be removed since it may lead to the next nonce being too high and creating a gap.
    /// </remarks>
    /// <param name="nonce">The nonce to consider removing.</param>
    /// <returns>An indication of what to do next.</returns>
    Task<NonceRollbackResponse> AfterSubmissionFailureAsync(uint nonce);

    /// <summary>
    /// Call this when a transaction fails to submit due to a transaction reverted.
    /// </summary>
    /// <param name="nonce">The nonce that was submitted.</param>
    /// <returns>An indication of what to do next.</returns>
    Task<NonceRollbackResponse> AfterTransactionRevertedAsync(uint nonce);

    /// <summary>
    /// Call this when a transaction fails to submit due to a transaction out of gas.
    /// </summary>
    /// <param name="nonce">The nonce that was submitted.</param>
    /// <returns>An indication of what to do next.</returns>
    Task<NonceRollbackResponse> AfterTransactionOutOfGas(uint nonce);

    /// <summary>
    /// Call this when a transaction succeeds to submit.
    /// </summary>
    /// <param name="nonce">The nonce that was submitted.</param>
    Task AfterSubmissionSuccessAsync(uint nonce);

    /// <summary>
    /// Call this when a transaction fails to submit due to a nonce too low to reserve the next nonce.
    /// </summary>
    /// <param name="nonce">The nonce that was submitted.</param>
    /// <returns>The next nonce to use.</returns>
    Task<uint> AfterNonceTooLowAsync(uint nonce);
}

// I think we need to insert a new file for each nonce into a container, and remove really old ones.
//
// Then remove the nonce file if the transaction fails so it can be retried or a new nonce can be used.

// If two transactions are sent at the same time, we can use an Etag of zero to ensure that the nonce
// file does not already exist. If two transactions are sent at the same time, say 9 and 10, and number
// 9 fails to submit due to a .NET application bug, but 10 submits, then the nonce file for 9 will be
// removed leaving 10, but that transaction will be stuck in the blockchain pool because there is a gap.

// Gap detection should be done on revert nonce. If there is a file for any number higher than the revert
// nonce, then we know there is a gap.

// We'll need to fill the gap by submitting a no-op transaction with the nonce of the revert nonce.

//

// IncrementNonce:
//
// Get all the files in the container.
// Order by name, which is the nonce.
// Read the name into an int.
// Increment the int.
// Check if the file is in the list.
// If not, create it and return its nonce using etag zero.
// If there are files in the list with a higher nonce, then we know there is a gap.
// If the write fails due to a conflict, continue iterating through the list.
// If we reach the end of the list, then write the new file with the new nonce and return its nonce.
// If the write fails due to a conflict, then we retry with a higher nonce.

// RemoveNonce:
//
// Remove the nonce file with the given nonce.

// Consuming code should attempt to submit the transaction with the nonce.
// If the transaction fails to submit due to "nonce too low", then call IncrementNonce to get the next nonce and retry.
// If the transactions fails for any other reason, then call RemoveNonce to remove the nonce file.