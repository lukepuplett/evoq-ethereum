using System.Linq;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class ContractAbiTests
{
    [TestMethod]
    public void GetFunctions_ReturnsOnlyFunctions()
    {
        // Arrange
        var abi = new ContractAbi(new List<ContractAbiItem>
        {
            new() { Type = "function", Name = "transfer" },
            new() { Type = "event", Name = "Transfer" },
            new() { Type = "function", Name = "balanceOf" },
            new() { Type = "error", Name = "InsufficientBalance" }
        });

        // Act
        var functions = abi.GetFunctions().ToList();

        // Assert
        Assert.AreEqual(2, functions.Count);
        Assert.IsTrue(functions.All(f => f.Type == "function"));
        CollectionAssert.Contains(functions.Select(f => f.Name).ToList(), "transfer");
        CollectionAssert.Contains(functions.Select(f => f.Name).ToList(), "balanceOf");
    }

    [TestMethod]
    public void GetFunction_WithExistingName_ReturnsFunction()
    {
        // Arrange
        var abi = new ContractAbi(new List<ContractAbiItem>
        {
            new() { Type = "function", Name = "transfer" },
            new() { Type = "event", Name = "Transfer" }
        });

        // Act
        var found = abi.TryGetFunction("transfer", out var function);

        // Assert
        Assert.IsTrue(found);
        Assert.IsNotNull(function);
        Assert.AreEqual("function", function.Type);
        Assert.AreEqual("transfer", function.Name);
    }

    [TestMethod]
    public void GetFunction_WithNonExistentName_ReturnsNull()
    {
        // Arrange
        var abi = new ContractAbi(new List<ContractAbiItem>
        {
            new() { Type = "function", Name = "transfer" }
        });

        // Act
        var found = abi.TryGetFunction("nonexistent", out var function);

        // Assert
        Assert.IsFalse(found);
    }

    [TestMethod]
    public void GetFunctions_WithOverloadedName_ReturnsAllOverloads()
    {
        // Arrange
        var abi = new ContractAbi(new List<ContractAbiItem>
        {
            new() { Type = "function", Name = "transfer", Inputs = new List<ContractAbiParameter> { new() { Type = "address" } } },
            new() { Type = "function", Name = "transfer", Inputs = new List<ContractAbiParameter> { new() { Type = "address" }, new() { Type = "uint256" } } },
            new() { Type = "event", Name = "Transfer" }
        });

        // Act
        var overloads = abi.GetFunctions("transfer").ToList();

        // Assert
        Assert.AreEqual(2, overloads.Count);
        Assert.IsTrue(overloads.All(f => f.Type == "function" && f.Name == "transfer"));
        Assert.AreEqual(1, overloads[0].Inputs.Count);
        Assert.AreEqual(2, overloads[1].Inputs.Count);
    }
}