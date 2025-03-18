using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Transactions;

[TestClass]
public class FileSystemNonceStoreTests
{
    private string testDirectory = string.Empty;
    private ILoggerFactory loggerFactory = null!;

    [TestInitialize]
    public void Setup()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), "FileSystemNonceStoreTests", Guid.NewGuid().ToString());
        loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void Constructor_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new FileSystemNonceStore(null!, loggerFactory));
    }

    [TestMethod]
    public void Constructor_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new FileSystemNonceStore(string.Empty, loggerFactory));
    }

    [TestMethod]
    public void Constructor_WithWhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new FileSystemNonceStore("   ", loggerFactory));
    }

    [TestMethod]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new FileSystemNonceStore(testDirectory, null!));
    }

    [TestMethod]
    public void Constructor_WithValidParameters_CreatesDirectory()
    {
        // Act
        _ = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Assert
        Assert.IsTrue(Directory.Exists(testDirectory));
    }

    [TestMethod]
    public void Constructor_WithExistingDirectory_DoesNotThrow()
    {
        // Arrange
        Directory.CreateDirectory(testDirectory);

        // Act & Assert
        try
        {
            _ = new FileSystemNonceStore(testDirectory, loggerFactory);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected no exception, but got: {ex}");
        }
    }

    [TestMethod]
    public void Constructor_NormalizesPath()
    {
        // Arrange
        var nonNormalizedPath = Path.Combine(testDirectory, "..", Path.GetFileName(testDirectory));

        // Act
        var store = new FileSystemNonceStore(nonNormalizedPath, loggerFactory);

        // Assert
        // Use reflection to check the normalized path
        var pathField = typeof(FileSystemNonceStore).GetField("path",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var actualPath = pathField!.GetValue(store) as string;
        Assert.AreEqual(Path.GetFullPath(testDirectory), actualPath);
    }

    [TestMethod]
    public async Task BeforeSubmissionAsync_FirstCall_ReturnsZero()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act
        var nonce = await store.BeforeSubmissionAsync();

        // Assert
        Assert.AreEqual(0u, nonce);
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "0.nonce")));
    }

    [TestMethod]
    public async Task BeforeSubmissionAsync_SequentialCalls_ReturnsIncrementingNonces()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act
        var nonce1 = await store.BeforeSubmissionAsync();
        var nonce2 = await store.BeforeSubmissionAsync();
        var nonce3 = await store.BeforeSubmissionAsync();

        // Assert
        Assert.AreEqual(0u, nonce1);
        Assert.AreEqual(1u, nonce2);
        Assert.AreEqual(2u, nonce3);

        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "0.nonce")));
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "1.nonce")));
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "2.nonce")));
    }

    [TestMethod]
    public async Task BeforeSubmissionAsync_ConcurrentCalls_ReturnsUniqueNonces()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var tasks = new List<Task<uint>>();
        const int concurrentCalls = 10;

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(store.BeforeSubmissionAsync());
        }
        var nonces = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(concurrentCalls, nonces.Distinct().Count(), "All nonces should be unique");
        Assert.AreEqual(0u, nonces.Min(), "Should start from 0");
        Assert.AreEqual((uint)(concurrentCalls - 1), nonces.Max(), "Should be consecutive");

        // Verify files exist
        for (uint i = 0; i < concurrentCalls; i++)
        {
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, $"{i}.nonce")));
        }
    }

    [TestMethod]
    public async Task BeforeSubmissionAsync_DirectoryDeleted_RecreatesDirectory()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        await store.BeforeSubmissionAsync(); // Create first nonce
        Directory.Delete(testDirectory, recursive: true);

        // Act
        var nonce = await store.BeforeSubmissionAsync();

        // Assert
        Assert.IsTrue(Directory.Exists(testDirectory));
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, $"{nonce}.nonce")));
    }

    [TestMethod]
    public async Task BeforeSubmissionAsync_WithExistingFiles_SkipsUsedNonces()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        File.WriteAllText(Path.Combine(testDirectory, "0.nonce"), string.Empty);
        File.WriteAllText(Path.Combine(testDirectory, "1.nonce"), string.Empty);

        // Act
        var nonce = await store.BeforeSubmissionAsync();

        // Assert
        Assert.AreEqual(2u, nonce);
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "2.nonce")));
    }

    [TestMethod]
    public async Task BeforeSubmissionAsync_WithInvalidPermissions_ThrowsException()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        Directory.CreateDirectory(testDirectory);

        // Make directory read-only
        File.SetAttributes(testDirectory, FileAttributes.ReadOnly);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
            async () => await store.BeforeSubmissionAsync());
    }

    [TestMethod]
    public async Task AfterNonceTooLowAsync_ReturnsHigherNonce()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var lowNonce = 5u;

        // Act
        var nextNonce = await store.AfterNonceTooLowAsync(lowNonce);

        // Assert
        Assert.IsTrue(nextNonce > lowNonce);
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, $"{nextNonce}.nonce")));
    }

    [TestMethod]
    public async Task AfterNonceTooLowAsync_WithExistingNonces_SkipsUsedNonces()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        File.WriteAllText(Path.Combine(testDirectory, "6.nonce"), string.Empty);
        File.WriteAllText(Path.Combine(testDirectory, "7.nonce"), string.Empty);
        var lowNonce = 5u;

        // Act
        var nextNonce = await store.AfterNonceTooLowAsync(lowNonce);

        // Assert
        Assert.AreEqual(8u, nextNonce);
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "8.nonce")));
    }

    [TestMethod]
    public async Task AfterNonceTooLowAsync_ConcurrentCalls_ReturnsUniqueNonces()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var tasks = new List<Task<uint>>();
        const int concurrentCalls = 5;
        const uint lowNonce = 10u;

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(store.AfterNonceTooLowAsync(lowNonce));
        }
        var nonces = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(concurrentCalls, nonces.Distinct().Count(), "All nonces should be unique");
        Assert.IsTrue(nonces.All(n => n > lowNonce), "All nonces should be higher than low nonce");

        // Verify files exist
        foreach (var nonce in nonces)
        {
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, $"{nonce}.nonce")));
        }
    }

    [TestMethod]
    public async Task AfterNonceTooLowAsync_DirectoryDeleted_RecreatesDirectory()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        Directory.Delete(testDirectory, recursive: true);

        // Act
        var nextNonce = await store.AfterNonceTooLowAsync(5u);

        // Assert
        Assert.IsTrue(Directory.Exists(testDirectory));
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, $"{nextNonce}.nonce")));
        Assert.IsTrue(nextNonce > 5u);
    }

    [TestMethod]
    public async Task AfterSubmissionSuccessAsync_RemovesFailureMarker()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);
        File.WriteAllText(failurePath, DateTimeOffset.UtcNow.ToString("O"));

        // Act
        await store.AfterSubmissionSuccessAsync(nonce);

        // Assert
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
        Assert.IsFalse(File.Exists(failurePath), "Failure marker should be removed");
    }

    [TestMethod]
    public async Task AfterSubmissionSuccessAsync_WithoutFailureMarker_DoesNotThrow()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        // Act & Assert
        try
        {
            await store.AfterSubmissionSuccessAsync(nonce);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected no exception, but got: {ex}");
        }
    }

    [TestMethod]
    public async Task AfterSubmissionSuccessAsync_WithoutNonceFile_DoesNotThrow()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act & Assert
        try
        {
            await store.AfterSubmissionSuccessAsync(5u);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected no exception, but got: {ex}");
        }
    }

    [TestMethod]
    public async Task AfterSubmissionSuccessAsync_ConcurrentCalls_HandledSafely()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);
        File.WriteAllText(failurePath, DateTimeOffset.UtcNow.ToString("O"));

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => store.AfterSubmissionSuccessAsync(nonce))
            .ToList();
        await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
        Assert.IsFalse(File.Exists(failurePath), "Failure marker should be removed");
    }

    [TestMethod]
    public async Task AfterSubmissionFailureAsync_FirstFailure_CreatesFailureMarker()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        // Act
        var result = await store.AfterSubmissionFailureAsync(nonce);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NotRemovedShouldRetry, result);
        Assert.IsTrue(File.Exists(failurePath), "Failure marker should be created");
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
    }

    [TestMethod]
    public async Task AfterSubmissionFailureAsync_WithinRetryWindow_ReturnsRetry()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);
        File.WriteAllText(failurePath, DateTimeOffset.UtcNow.ToString("O"));

        // Act
        var result = await store.AfterSubmissionFailureAsync(nonce);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NotRemovedShouldRetry, result);
        Assert.IsTrue(File.Exists(failurePath), "Failure marker should remain");
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
    }

    [TestMethod]
    public async Task AfterSubmissionFailureAsync_AfterRetryWindow_RemovesNonce()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);
        File.WriteAllText(failurePath, DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"));

        // Act
        var result = await store.AfterSubmissionFailureAsync(nonce);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.RemovedOkay, result);
        Assert.IsFalse(File.Exists(failurePath), "Failure marker should be removed");
        Assert.IsFalse(File.Exists(noncePath), "Nonce file should be removed");
    }

    [TestMethod]
    public async Task AfterSubmissionFailureAsync_WithHigherNonce_DetectsGap()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);
        File.WriteAllText(failurePath, DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"));
        File.WriteAllText(Path.Combine(testDirectory, "6.nonce"), string.Empty);

        // Act
        var result = await store.AfterSubmissionFailureAsync(nonce);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.RemovedGapDetected, result);
        Assert.IsFalse(File.Exists(failurePath), "Failure marker should be removed");
        Assert.IsFalse(File.Exists(noncePath), "Nonce file should be removed");
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "6.nonce")), "Higher nonce file should remain");
    }

    [TestMethod]
    public async Task AfterSubmissionFailureAsync_NonExistentNonce_ReturnsNonceNotFound()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        Directory.CreateDirectory(testDirectory);

        // Act
        var result = await store.AfterSubmissionFailureAsync(5u);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NonceNotFound, result);
    }

    [TestMethod]
    public async Task AfterSubmissionFailureAsync_ConcurrentCalls_HandledSafely()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => store.AfterSubmissionFailureAsync(nonce))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(r => r == NonceRollbackResponse.NotRemovedShouldRetry));
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
    }

    [TestMethod]
    public async Task AfterTransactionRevertedAsync_ReturnsNotRemovedGasSpent()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        // Act
        var result = await store.AfterTransactionRevertedAsync(nonce);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NotRemovedGasSpent, result);
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
    }

    [TestMethod]
    public async Task AfterTransactionRevertedAsync_WithNonExistentNonce_ReturnsNonceNotFound()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        Directory.CreateDirectory(testDirectory);

        // Act
        var result = await store.AfterTransactionRevertedAsync(5u);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NonceNotFound, result);
    }

    [TestMethod]
    public async Task AfterTransactionRevertedAsync_ConcurrentCalls_HandledSafely()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => store.AfterTransactionRevertedAsync(nonce))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(r => r == NonceRollbackResponse.NotRemovedGasSpent));
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
    }

    [TestMethod]
    public async Task AfterTransactionRevertedAsync_WithFailureMarker_PreservesFailureMarker()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);
        File.WriteAllText(failurePath, DateTimeOffset.UtcNow.ToString("O"));

        // Act
        var result = await store.AfterTransactionRevertedAsync(nonce);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NotRemovedGasSpent, result);
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
        Assert.IsTrue(File.Exists(failurePath), "Failure marker should remain");
    }

    [TestMethod]
    public async Task AfterTransactionOutOfGas_ReturnsNotRemovedGasSpent()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        // Act
        var result = await store.AfterTransactionOutOfGas(nonce);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NotRemovedGasSpent, result);
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
    }

    [TestMethod]
    public async Task AfterTransactionOutOfGas_WithNonExistentNonce_ReturnsNonceNotFound()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        Directory.CreateDirectory(testDirectory);

        // Act
        var result = await store.AfterTransactionOutOfGas(5u);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NonceNotFound, result);
    }

    [TestMethod]
    public async Task AfterTransactionOutOfGas_ConcurrentCalls_HandledSafely()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => store.AfterTransactionOutOfGas(nonce))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(r => r == NonceRollbackResponse.NotRemovedGasSpent));
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
    }

    [TestMethod]
    public async Task AfterTransactionOutOfGas_WithFailureMarker_PreservesFailureMarker()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);
        File.WriteAllText(failurePath, DateTimeOffset.UtcNow.ToString("O"));

        // Act
        var result = await store.AfterTransactionOutOfGas(nonce);

        // Assert
        Assert.AreEqual(NonceRollbackResponse.NotRemovedGasSpent, result);
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain");
        Assert.IsTrue(File.Exists(failurePath), "Failure marker should remain");
    }

    [TestMethod]
    public async Task Integration_SuccessfulTransactionLifecycle()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act & Assert
        // 1. Reserve nonce
        var nonce = await store.BeforeSubmissionAsync();
        Assert.AreEqual(0u, nonce);
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "0.nonce")));

        // 2. Mark as success
        await store.AfterSubmissionSuccessAsync(nonce);
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "0.nonce")), "Nonce file should remain after success");
    }

    [TestMethod]
    public async Task Integration_FailureRetrySuccessLifecycle()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act & Assert
        // 1. Reserve nonce
        var nonce = await store.BeforeSubmissionAsync();
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        // 2. First failure
        var result1 = await store.AfterSubmissionFailureAsync(nonce);
        Assert.AreEqual(NonceRollbackResponse.NotRemovedShouldRetry, result1);
        Assert.IsTrue(File.Exists(noncePath));
        Assert.IsTrue(File.Exists(failurePath));

        // 3. Retry within window
        var result2 = await store.AfterSubmissionFailureAsync(nonce);
        Assert.AreEqual(NonceRollbackResponse.NotRemovedShouldRetry, result2);
        Assert.IsTrue(File.Exists(noncePath));
        Assert.IsTrue(File.Exists(failurePath));

        // 4. Finally succeed
        await store.AfterSubmissionSuccessAsync(nonce);
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain after success");
        Assert.IsFalse(File.Exists(failurePath), "Failure marker should be removed after success");
    }

    [TestMethod]
    public async Task Integration_RevertedTransactionLifecycle()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act & Assert
        // 1. Reserve nonce
        var nonce = await store.BeforeSubmissionAsync();
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");

        // 2. Transaction reverts
        var result = await store.AfterTransactionRevertedAsync(nonce);
        Assert.AreEqual(NonceRollbackResponse.NotRemovedGasSpent, result);
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain after revert");

        // 3. Next nonce should be higher
        var nextNonce = await store.BeforeSubmissionAsync();
        Assert.AreEqual(1u, nextNonce);
    }

    [TestMethod]
    public async Task Integration_OutOfGasLifecycle()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act & Assert
        // 1. Reserve nonce
        var nonce = await store.BeforeSubmissionAsync();
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");

        // 2. Transaction runs out of gas
        var result = await store.AfterTransactionOutOfGas(nonce);
        Assert.AreEqual(NonceRollbackResponse.NotRemovedGasSpent, result);
        Assert.IsTrue(File.Exists(noncePath), "Nonce file should remain after out of gas");

        // 3. Next nonce should be higher
        var nextNonce = await store.BeforeSubmissionAsync();
        Assert.AreEqual(1u, nextNonce);
    }

    [TestMethod]
    public async Task Integration_ConcurrentTransactions()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        const int transactionCount = 5;

        // Act
        // 1. Reserve multiple nonces concurrently
        var reserveTasks = Enumerable.Range(0, transactionCount)
            .Select(_ => store.BeforeSubmissionAsync())
            .ToList();
        var nonces = await Task.WhenAll(reserveTasks);

        // 2. Mix of success and failure responses
        var tasks = new List<Task>();
        for (int i = 0; i < nonces.Length; i++)
        {
            var nonce = nonces[i];
            if (i % 2 == 0)
            {
                tasks.Add(store.AfterSubmissionSuccessAsync(nonce));
            }
            else
            {
                tasks.Add(store.AfterSubmissionFailureAsync(nonce));
            }
        }
        await Task.WhenAll(tasks);

        // Assert
        // Check all even nonces have their files but no failure markers
        // Check all odd nonces have both nonce files and failure markers
        for (int i = 0; i < nonces.Length; i++)
        {
            var noncePath = Path.Combine(testDirectory, $"{i}.nonce");
            var failurePath = Path.Combine(testDirectory, $"{i}.failed");

            Assert.IsTrue(File.Exists(noncePath), $"Nonce file {i} should exist");
            Assert.AreEqual(i % 2 == 1, File.Exists(failurePath),
                $"Failure marker for {i} should {(i % 2 == 1 ? "exist" : "not exist")}");
        }
    }

    [TestMethod]
    public async Task Integration_ApplicationRestartPersistence()
    {
        // Arrange
        var store1 = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act & Assert
        // 1. Create some nonces with first instance
        var nonce1 = await store1.BeforeSubmissionAsync();
        var nonce2 = await store1.BeforeSubmissionAsync();
        await store1.AfterSubmissionSuccessAsync(nonce1);
        await store1.AfterSubmissionFailureAsync(nonce2);

        // 2. Create new instance (simulating app restart)
        var store2 = new FileSystemNonceStore(testDirectory, loggerFactory);

        // 3. Verify state persisted
        var nonce3 = await store2.BeforeSubmissionAsync();
        Assert.AreEqual(2u, nonce3, "New instance should continue from last used nonce");
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "0.nonce")));
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "1.nonce")));
        Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "1.failed")));
    }

    [TestMethod]
    public void EdgeCase_VeryLongPath()
    {
        // Arrange
        var longPath = Path.Combine(testDirectory, new string('x', 1000));

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new FileSystemNonceStore(longPath, loggerFactory));
    }

    [TestMethod]
    public async Task EdgeCase_RapidSuccessiveOperations()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        const int operationCount = 100;

        // Act & Assert
        // Rapidly alternate between reserving nonces and marking them as success/failure
        for (int i = 0; i < operationCount; i++)
        {
            var nonce = await store.BeforeSubmissionAsync();
            if (i % 2 == 0)
            {
                await store.AfterSubmissionSuccessAsync(nonce);
            }
            else
            {
                await store.AfterSubmissionFailureAsync(nonce);
            }
        }

        // Verify all files exist and are in correct state
        for (int i = 0; i < operationCount; i++)
        {
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, $"{i}.nonce")));
            if (i % 2 == 1)
            {
                Assert.IsTrue(File.Exists(Path.Combine(testDirectory, $"{i}.failed")));
            }
        }
    }

    [TestMethod]
    public async Task EdgeCase_DiskFull()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        try
        {
            // Act & Assert
            // Mock disk full by making directory read-only
            File.SetAttributes(testDirectory, FileAttributes.ReadOnly);

            // Should handle disk full gracefully
            var result = await store.AfterSubmissionFailureAsync(nonce);
            Assert.AreEqual(NonceRollbackResponse.NotRemovedDueToError, result);
        }
        finally
        {
            // Restore attributes for cleanup
            File.SetAttributes(testDirectory, FileAttributes.Normal);
        }
    }

    [TestMethod]
    public async Task EdgeCase_CorruptedFailureTimestamp()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);
        File.WriteAllText(failurePath, "not a valid timestamp");

        // Act & Assert
        // Should handle corrupted timestamp gracefully
        var result = await store.AfterSubmissionFailureAsync(nonce);
        Assert.AreEqual(NonceRollbackResponse.NotRemovedDueToError, result);
    }

    [TestMethod]
    public async Task EdgeCase_ConcurrentDirectoryDeletion()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);

        // Act & Assert
        // Start multiple operations
        var tasks = new List<Task>();

        // Add BeforeSubmissionAsync tasks
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await store.BeforeSubmissionAsync();
                }
                catch (DirectoryNotFoundException)
                {
                    // Expected for some tasks
                }
            }));
        }

        // Delete directory while operations are in progress
        await Task.Delay(10); // Give some tasks a chance to start
        Directory.Delete(testDirectory, recursive: true);

        // All tasks should complete without unhandled exceptions
        await Task.WhenAll(tasks);
    }

    [TestMethod]
    public async Task EdgeCase_PartiallyWrittenFiles()
    {
        // Arrange
        var store = new FileSystemNonceStore(testDirectory, loggerFactory);
        var nonce = 5u;
        var noncePath = Path.Combine(testDirectory, $"{nonce}.nonce");
        var failurePath = Path.Combine(testDirectory, $"{nonce}.failed");

        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(noncePath, string.Empty);

        // Create a 0-byte failure marker
        using (File.Create(failurePath)) { }

        // Act
        var result = await store.AfterSubmissionFailureAsync(nonce);

        // Assert
        // Should handle empty failure marker gracefully
        Assert.AreEqual(NonceRollbackResponse.NotRemovedDueToError, result);
    }

    [TestMethod]
    public async Task EdgeCase_MaximumPathLength()
    {
        // Skip on platforms with high/unlimited path length
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            Assert.Inconclusive("Path length test only applicable on Windows");
            return;
        }

        // Arrange
        var longPath = Path.Combine(testDirectory, new string('x', 240)); // Close to max path length
        var store = new FileSystemNonceStore(longPath, loggerFactory);

        // Act & Assert
        try
        {
            await store.BeforeSubmissionAsync();
            Assert.Fail("Should throw PathTooLongException");
        }
        catch (PathTooLongException)
        {
            // Expected
        }
    }
}
