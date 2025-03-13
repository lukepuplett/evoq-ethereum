using System.Numerics;

namespace Evoq.Ethereum.ABI.Conversion;

[TestClass]
public class ArrayObjectConverterTests
{
    private ArrayObjectConverter converter = new();

    [TestMethod]
    public void ArrayToObject_PositionalMapping_Success()
    {
        // Arrange
        var values = new object[] { "John", "Doe", BigInteger.Parse("35") };

        // Act
        var user = this.converter.ArrayToObject<PositionalUser>(values);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("John", user.FirstName);
        Assert.AreEqual("Doe", user.LastName);
        Assert.AreEqual(BigInteger.Parse("35"), user.Age);
    }
}