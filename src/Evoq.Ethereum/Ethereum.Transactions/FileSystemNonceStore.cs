using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// A nonce store that persists nonces to the file system. This implementation is intended for local testing
/// and development purposes only, not for production use. It uses simple file operations to track nonces,
/// where each nonce is represented by a file in the specified directory.
/// </summary>
public class FileSystemNonceStore : INonceStore
{
    private readonly string path;
    private readonly ILogger<FileSystemNonceStore> logger;
    private readonly object nonceStoreLock = new();
    private readonly Func<Task<BigInteger>>? getTransactionCount;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemNonceStore"/> class.
    /// </summary>
    /// <param name="path">The path to the nonce store.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="getTransactionCount">Optional function to get the current transaction count from an external source (e.g., blockchain).</param>
    public FileSystemNonceStore(
        string path,
        ILoggerFactory loggerFactory,
        Func<Task<BigInteger>>? getTransactionCount = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));

        // Get the full path and validate its length
        var fullPath = Path.GetFullPath(path);

        // Check if the path length would exceed system limits
        // Add some buffer for the nonce files (e.g., "/999999.nonce")
        const int nonceFileBufferLength = 12;
        if (fullPath.Length + nonceFileBufferLength >= 260) // Common max path length
        {
            throw new ArgumentException(
                $"Path is too long. The path length ({fullPath.Length}) plus space needed for nonce files would exceed system limits.",
                nameof(path));
        }

        this.path = fullPath;
        this.logger = loggerFactory?.CreateLogger<FileSystemNonceStore>()
            ?? throw new ArgumentNullException(nameof(loggerFactory));
        this.getTransactionCount = getTransactionCount;

        EnsureDirectoryExists();
    }

    //

    /// <summary>
    /// Gets the next nonce to use.
    /// </summary>
    /// <returns>The next nonce to use.</returns>
    public Task<uint> BeforeSubmissionAsync()
    {
        EnsureDirectoryExists();

        try
        {
            lock (nonceStoreLock)
            {
                uint startingNonce = 0;

                // If getNonce is provided, try to get the current nonce from external source
                if (getTransactionCount != null)
                {
                    try
                    {
                        var currentNonce = getTransactionCount().Result;
                        startingNonce = (uint)currentNonce;
                        logger.LogDebug("Got starting nonce {Nonce} from external source", startingNonce);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to get nonce from external source, falling back to file system");
                    }
                }

                while (true)
                {
                    var noncePath = Path.Combine(path, $"{startingNonce}.nonce");

                    try
                    {
                        // Use FileMode.CreateNew to ensure atomic creation
                        using (FileStream fs = File.Open(noncePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            // File created successfully
                            logger.LogDebug("Reserved nonce {Nonce} at {Path}", startingNonce, noncePath);
                            return Task.FromResult(startingNonce);
                        }
                    }
                    catch (IOException) when (File.Exists(noncePath))
                    {
                        // More specific exception handling
                        logger.LogDebug("Nonce {Nonce} already exists, trying next", startingNonce);
                        startingNonce++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reserve nonce in directory {Path}", path);
            throw;
        }
    }

    /// <summary>
    /// Gets the next nonce to use after a too-low nonce.
    /// </summary>
    /// <param name="nonce">The nonce to check.</param>
    /// <returns>The next nonce to use.</returns>
    public async Task<uint> AfterNonceTooLowAsync(uint nonce)
    {
        try
        {
            uint nextNonce;
            do
            {
                nextNonce = await BeforeSubmissionAsync();
            } while (nextNonce <= nonce);

            logger.LogDebug("Found next available nonce {NextNonce} after too-low nonce {Nonce}", nextNonce, nonce);
            return nextNonce;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get next nonce after too-low nonce {Nonce}", nonce);
            throw;
        }
    }

    /// <summary>
    /// Handles a failed transaction submission.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <returns>The response to the caller.</returns>
    public Task<NonceRollbackResponse> AfterSubmissionFailureAsync(uint nonce)
    {
        try
        {
            lock (nonceStoreLock)
            {
                var failureFilePath = Path.Combine(path, $"{nonce}.failed");
                var nonceFilePath = Path.Combine(path, $"{nonce}.nonce");

                if (!File.Exists(nonceFilePath))
                {
                    logger.LogWarning("Nonce file {Nonce} not found during failure handling", nonce);
                    return Task.FromResult(NonceRollbackResponse.NonceNotFound);
                }

                if (File.Exists(failureFilePath))
                {
                    var failureTimeText = File.ReadAllText(failureFilePath);
                    var failureTime = DateTimeOffset.Parse(failureTimeText);
                    if (DateTimeOffset.UtcNow.Subtract(failureTime).TotalSeconds < 10)
                    {
                        logger.LogInformation(
                            "Nonce {Nonce} failed recently. Caller should keep trying for now.", nonce);
                        return Task.FromResult(NonceRollbackResponse.NotRemovedShouldRetry);
                    }

                    return RemoveNonceUnderLockAsync(nonce);
                }

                // First-time failure - store UTC timestamp
                File.WriteAllText(failureFilePath, DateTimeOffset.UtcNow.ToString("O"));
                logger.LogDebug("First-time failure for nonce {Nonce}, recording for retry", nonce);
                return Task.FromResult(NonceRollbackResponse.NotRemovedShouldRetry);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling submission failure for nonce {Nonce}", nonce);
            return Task.FromResult(NonceRollbackResponse.NotRemovedDueToError);
        }
    }

    /// <summary>
    /// Handles a successful transaction submission.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <returns>The response to the caller.</returns>
    public Task AfterSubmissionSuccessAsync(uint nonce)
    {
        try
        {
            lock (nonceStoreLock)
            {
                // Remove any failure marker file if it exists
                var failureFilePath = Path.Combine(path, $"{nonce}.failed");
                if (File.Exists(failureFilePath))
                {
                    File.Delete(failureFilePath);
                    logger.LogDebug("Removed failure marker for successful nonce {Nonce}", nonce);
                }

                // Note: We keep the .nonce file as it represents a used nonce
                logger.LogDebug("Transaction with nonce {Nonce} succeeded", nonce);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling successful submission for nonce {Nonce}", nonce);
            throw;
        }
    }

    /// <summary>
    /// Handles a transaction that ran out of gas.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <returns>The response to the caller.</returns>
    public Task<NonceRollbackResponse> AfterTransactionOutOfGas(uint nonce)
    {
        try
        {
            lock (nonceStoreLock)
            {
                var nonceFilePath = Path.Combine(path, $"{nonce}.nonce");

                if (!File.Exists(nonceFilePath))
                {
                    logger.LogWarning("Nonce file {Nonce} not found during out-of-gas handling", nonce);
                    return Task.FromResult(NonceRollbackResponse.NonceNotFound);
                }

                logger.LogError("Transaction with nonce {Nonce} out of gas", nonce);
                return Task.FromResult(NonceRollbackResponse.NotRemovedGasSpent);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling out-of-gas for nonce {Nonce}", nonce);
            return Task.FromResult(NonceRollbackResponse.NotRemovedDueToError);
        }
    }

    /// <summary>
    /// Handles a transaction that was reverted.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <returns>The response to the caller.</returns>
    public Task<NonceRollbackResponse> AfterTransactionRevertedAsync(uint nonce)
    {
        try
        {
            lock (nonceStoreLock)
            {
                var nonceFilePath = Path.Combine(path, $"{nonce}.nonce");

                if (!File.Exists(nonceFilePath))
                {
                    logger.LogWarning("Nonce file {Nonce} not found during revert handling", nonce);
                    return Task.FromResult(NonceRollbackResponse.NonceNotFound);
                }

                logger.LogError("Transaction with nonce {Nonce} reverted", nonce);
                return Task.FromResult(NonceRollbackResponse.NotRemovedGasSpent);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling transaction revert for nonce {Nonce}", nonce);
            return Task.FromResult(NonceRollbackResponse.NotRemovedDueToError);
        }
    }

    //

    private Task<NonceRollbackResponse> RemoveNonceUnderLockAsync(uint nonce)
    {
        // Already under lock from caller
        var nonceFilePath = Path.Combine(path, $"{nonce}.nonce");
        var failureFilePath = Path.Combine(path, $"{nonce}.failed");

        try
        {
            if (!File.Exists(nonceFilePath))
            {
                return Task.FromResult(NonceRollbackResponse.NonceNotFound);
            }

            // Delete files safely
            try
            {
                if (File.Exists(failureFilePath))
                    File.Delete(failureFilePath);
                File.Delete(nonceFilePath);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Failed to delete nonce files for {Nonce}", nonce);
                return Task.FromResult(NonceRollbackResponse.NotRemovedDueToError);
            }

            // Check for gaps
            var hasHigherNonce = Directory.GetFiles(path, "*.nonce")
                .Any(f =>
                {
                    var filename = Path.GetFileNameWithoutExtension(f);
                    return uint.TryParse(filename, out var existingNonce) && existingNonce > nonce;
                });

            if (hasHigherNonce)
            {
                logger.LogWarning("Removed nonce {Nonce}, created gap as higher nonces exist", nonce);
                return Task.FromResult(NonceRollbackResponse.RemovedGapDetected);
            }

            logger.LogDebug("Removed nonce {Nonce}, no gap created", nonce);
            return Task.FromResult(NonceRollbackResponse.RemovedOkay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing nonce {Nonce}", nonce);
            return Task.FromResult(NonceRollbackResponse.NotRemovedDueToError);
        }
    }

    // Helper methods

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            logger.LogInformation("Created nonce store directory at {Path}", path);
        }
    }
}
