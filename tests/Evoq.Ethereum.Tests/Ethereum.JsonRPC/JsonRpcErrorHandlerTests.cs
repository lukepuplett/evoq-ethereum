using System;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Tests.JsonRPC;

[TestClass]
public class JsonRpcErrorHandlerTests
{
    [TestMethod]
    public void IsExpectedException_WithOutOfGasError_ReturnsOutOfGasException()
    {
        // Arrange
        var error = new JsonRpcError { Code = 3, Message = "out of gas" };
        var exception = new JsonRpcProvidedErrorException(error);

        // Act
        var result = JsonRpcErrorHandler.IsExpectedException(exception, out var specificException);

        // Assert
        Assert.IsTrue(result);
        Assert.IsInstanceOfType(specificException, typeof(OutOfGasException));
    }

    [TestMethod]
    public void IsExpectedException_WithNonceTooLowError_ReturnsInvalidNonceException()
    {
        // Arrange
        var error = new JsonRpcError { Code = 3, Message = "nonce too low" };
        var exception = new JsonRpcProvidedErrorException(error);

        // Act
        var result = JsonRpcErrorHandler.IsExpectedException(exception, out var specificException);

        // Assert
        Assert.IsTrue(result);
        Assert.IsInstanceOfType(specificException, typeof(InvalidNonceException));
    }

    [TestMethod]
    public void IsExpectedException_WithInsufficientFundsError_ReturnsInsufficientFundsException()
    {
        // Arrange
        var error = new JsonRpcError { Code = 3, Message = "insufficient funds" };
        var exception = new JsonRpcProvidedErrorException(error);

        // Act
        var result = JsonRpcErrorHandler.IsExpectedException(exception, out var specificException);

        // Assert
        Assert.IsTrue(result);
        Assert.IsInstanceOfType(specificException, typeof(InsufficientFundsException));
    }

    [TestMethod]
    public void IsExpectedException_WithLimitExceededError_ReturnsLimitExceededException()
    {
        // Arrange
        var error = new JsonRpcError { Code = -32005, Message = "limit exceeded" };
        var exception = new JsonRpcProvidedErrorException(error);

        // Act
        var result = JsonRpcErrorHandler.IsExpectedException(exception, out var specificException);

        // Assert
        Assert.IsTrue(result);
        Assert.IsInstanceOfType(specificException, typeof(LimitExceededException));
    }

    [TestMethod]
    public void IsExpectedException_WithNestedError_FindsExpectedException()
    {
        // Arrange
        var innerError = new JsonRpcError { Code = 3, Message = "out of gas" };
        var innerException = new JsonRpcProvidedErrorException(innerError);
        var exception = new Exception("Outer exception", innerException);

        // Act
        var result = JsonRpcErrorHandler.IsExpectedException(exception, out var specificException);

        // Assert
        Assert.IsTrue(result);
        Assert.IsInstanceOfType(specificException, typeof(OutOfGasException));
    }

    [TestMethod]
    public void IsExpectedException_WithUnknownError_ReturnsFalse()
    {
        // Arrange
        var error = new JsonRpcError { Code = -1, Message = "unknown error" };
        var exception = new JsonRpcProvidedErrorException(error);

        // Act
        var result = JsonRpcErrorHandler.IsExpectedException(exception, out var specificException);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(specificException);
    }

    [TestMethod]
    public void IsExpectedException_WithRequestFailedException_ChecksMessagePattern()
    {
        // Arrange
        var exception = new JsonRpcRequestFailedException("Transaction failed: gas price too low");

        // Act
        var result = JsonRpcErrorHandler.IsExpectedException(exception, out var specificException);

        // Assert
        Assert.IsTrue(result);
        Assert.IsInstanceOfType(specificException, typeof(GasPriceTooLowException));
    }
}