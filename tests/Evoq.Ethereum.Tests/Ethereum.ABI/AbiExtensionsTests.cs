namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiExtensionsTests
{
    [TestMethod]
    public void GetFunctionSignature_SimpleFunction_ReturnsCorrectSignature()
    {
        var item = new AbiItem
        {
            Type = "function",
            Name = "transfer",
            Inputs = new List<Parameter>
            {
                new() { Type = "address" },
                new() { Type = "uint256" }
            }
        };

        var signature = item.GetFunctionSignature();
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void GetFunctionSelector_SimpleFunction_ReturnsCorrectSelector()
    {
        var item = new AbiItem
        {
            Type = "function",
            Name = "transfer",
            Inputs = new List<Parameter>
            {
                new() { Type = "address" },
                new() { Type = "uint256" }
            }
        };

        var selector = item.GetFunctionSelector();
        CollectionAssert.AreEqual(
            Convert.FromHexString("a9059cbb"),
            selector);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void GetFunctionSignature_NonFunction_ThrowsException()
    {
        var item = new AbiItem
        {
            Type = "event",
            Name = "Transfer"
        };

        _ = item.GetFunctionSignature();
    }
}