using System;
using System.Linq;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Transactions.Tests;

[TestClass]
public class TransactionReceiptTests
{
    [TestMethod]
    public void FromDto_WhenDtoIsNull_ReturnsNull()
    {
        var result = TransactionReceipt.FromDto(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromDto_WhenDtoIsValid_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var dto = new TransactionReceiptDto
        {
            TransactionHashHex = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            TransactionIndexHex = "0x1",
            BlockHashHex = "0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            BlockNumberHex = "0x100",
            FromAddressHex = "0x742d35Cc6634C0532925a3b844Bc454e4438f44e",
            ToAddressHex = "0x742d35Cc6634C0532925a3b844Bc454e4438f44f",
            CumulativeGasUsedHex = "0x5208",
            EffectiveGasPriceHex = "0x4a817c800",
            GasUsedHex = "0x5208",
            ContractAddressHex = null,
            LogsBloomHex = "0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
            TransactionTypeHex = "0x2",
            StateRootHex = null,
            StatusHex = "0x1"
        };

        // Act
        var result = TransactionReceipt.FromDto(dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", result.TransactionHash.ToString());
        Assert.AreEqual(1UL, result.TransactionIndex);
        Assert.AreEqual("0x742d35Cc6634C0532925a3b844Bc454e4438f44e", result.From.ToString());
        Assert.AreEqual("0x742D35CC6634C0532925a3b844Bc454e4438F44f", result.To.ToString());
        Assert.AreEqual(TransactionType.DynamicFee, result.Type);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public void FromDto_WhenContractCreation_SetsToAddressToEmpty()
    {
        // Arrange
        var dto = new TransactionReceiptDto
        {
            TransactionHashHex = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            FromAddressHex = "0x742d35Cc6634C0532925a3b844Bc454e4438f44e",
            ToAddressHex = null,
            ContractAddressHex = "0x742D35CC6634C0532925a3b844Bc454e4438F44f",
            StatusHex = "0x1"
        };

        // Act
        var result = TransactionReceipt.FromDto(dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(EthereumAddress.Empty, result.To);
        Assert.AreEqual("0x742D35CC6634C0532925a3b844Bc454e4438F44f", result.ContractAddress.ToString());
    }

    [TestMethod]
    public void FromDto_WhenFailedTransaction_SetSuccessToFalse()
    {
        // Arrange
        var dto = new TransactionReceiptDto
        {
            TransactionHashHex = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            StatusHex = "0x0"
        };

        // Act
        var result = TransactionReceipt.FromDto(dto);

        // Assert
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void FromDto_WithLogs_ParsesLogsCorrectly()
    {
        // Arrange
        var dto = new TransactionReceiptDto
        {
            TransactionHashHex = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            Logs = new[]
            {
                new LogDto
                {
                    AddressHex = "0x742d35Cc6634C0532925a3b844Bc454e4438f44e",
                    TopicsHex = new[]
                    {
                        "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"
                    },
                    DataHex = "0x0000000000000000000000000000000000000000000000000000000000000001",
                    LogIndexHex = "0x0",
                    Removed = false
                }
            }
        };

        // Act
        var result = TransactionReceipt.FromDto(dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Logs.Length);
        Assert.AreEqual("0x742d35Cc6634C0532925a3b844Bc454e4438f44e", result.Logs[0].Address.ToString());
        Assert.AreEqual(1, result.Logs[0].Topics.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void FromDto_WithInvalidHexString_ThrowsFormatException()
    {
        // Arrange
        var dto = new TransactionReceiptDto
        {
            TransactionHashHex = "not a hex string"
        };

        // Act
        TransactionReceipt.FromDto(dto);
    }

    [TestMethod]
    public void FromDto_WithPreByzantiumReceipt_ParsesStateRootCorrectly()
    {
        // Arrange
        var dto = new TransactionReceiptDto
        {
            TransactionHashHex = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            StateRootHex = "0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            StatusHex = null
        };

        // Act
        var result = TransactionReceipt.FromDto(dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.StateRoot);
        Assert.AreEqual("0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890", result.StateRoot.ToString());
    }
}