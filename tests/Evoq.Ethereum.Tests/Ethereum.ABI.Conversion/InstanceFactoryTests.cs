using System;
using System.Numerics;
using Evoq.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.ABI.Conversion;

[TestClass]
public class InstanceFactoryTests
{
    private readonly InstanceFactory factory = new InstanceFactory();

    // Regular class with parameterless constructor
    public class SimpleClass
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    // Regular struct
    public struct SimpleStruct
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    // Record struct (no parameterless constructor)
    public record struct TokenRecord(
        EthereumAddress ContractAddress,
        string Symbol,
        byte Decimals,
        BigInteger TotalSupply);

    // Record class (has compiler-generated parameterless constructor)
    public record TokenRecordClass(
        EthereumAddress ContractAddress,
        string Symbol,
        byte Decimals,
        BigInteger TotalSupply);

    [TestMethod]
    public void CreateInstance_SimpleClass_CreatesInstance()
    {
        // Act
        var instance = factory.CreateInstance(typeof(SimpleClass));

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsInstanceOfType(instance, typeof(SimpleClass));

        var simpleClass = (SimpleClass)instance;
        Assert.IsNull(simpleClass.Name);
        Assert.AreEqual(0, simpleClass.Value);
    }

    [TestMethod]
    public void CreateInstance_SimpleStruct_CreatesInstance()
    {
        // Act
        var instance = factory.CreateInstance(typeof(SimpleStruct));

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsInstanceOfType(instance, typeof(SimpleStruct));

        var simpleStruct = (SimpleStruct)instance;
        Assert.AreEqual(null, simpleStruct.Name);
        Assert.AreEqual(0, simpleStruct.Value);
    }

    [TestMethod]
    public void CreateInstance_RecordStruct_CreatesInstance()
    {
        // Act
        var instance = factory.CreateInstance(typeof(TokenRecord));

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsInstanceOfType(instance, typeof(TokenRecord));

        var tokenRecord = (TokenRecord)instance;
        Assert.AreEqual(default(EthereumAddress), tokenRecord.ContractAddress);
        Assert.AreEqual(null, tokenRecord.Symbol);
        Assert.AreEqual(0, tokenRecord.Decimals);
        Assert.AreEqual(BigInteger.Zero, tokenRecord.TotalSupply);
    }

    [TestMethod]
    public void CreateInstance_RecordClass_CreatesInstance()
    {
        // Act
        var instance = factory.CreateInstance(typeof(TokenRecordClass));

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsInstanceOfType(instance, typeof(TokenRecordClass));

        var tokenRecord = (TokenRecordClass)instance;
        Assert.AreEqual(default(EthereumAddress), tokenRecord.ContractAddress);
        Assert.AreEqual(null, tokenRecord.Symbol);
        Assert.AreEqual(0, tokenRecord.Decimals);
        Assert.AreEqual(BigInteger.Zero, tokenRecord.TotalSupply);
    }

    [TestMethod]
    public void CreateInstance_GenericMethod_CreatesInstance()
    {
        // Act & Assert - Test with generic method for convenience
        var simpleClass = factory.CreateInstance<SimpleClass>();
        Assert.IsNotNull(simpleClass);
        Assert.IsNull(simpleClass.Name);

        var simpleStruct = factory.CreateInstance<SimpleStruct>();
        Assert.AreEqual(null, simpleStruct.Name);

        var tokenRecord = factory.CreateInstance<TokenRecord>();
        Assert.AreEqual(default(EthereumAddress), tokenRecord.ContractAddress);

        var tokenRecordClass = factory.CreateInstance<TokenRecordClass>();
        Assert.AreEqual(default(EthereumAddress), tokenRecordClass.ContractAddress);
    }
}