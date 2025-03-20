using System.Numerics;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI.Conversion;

[TestClass]
public class TupleObjectConverterTests
{
    private TupleObjectConverter converter = new();

    [TestMethod]
    public void TupleToObject_PositionalMapping_Success()
    {
        // Arrange
        var tuple = CreateTuple("John", "Doe", BigInteger.Parse("35"));

        // Act
        var user = this.converter.TupleToObject<PositionalUser>(tuple);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("John", user.FirstName);
        Assert.AreEqual("Doe", user.LastName);
        Assert.AreEqual(BigInteger.Parse("35"), user.Age);
    }

    [TestMethod]
    public void TupleToObject_WithNullValue_Success()
    {
        // Arrange
        var tuple = CreateTuple("Jane", null, BigInteger.Parse("28"));

        // Act
        var user = this.converter.TupleToObject<NullablePositionalUser>(tuple);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Jane", user.FirstName);
        Assert.IsNull(user.LastName);
        Assert.AreEqual(BigInteger.Parse("28"), user.Age);
    }

    [TestMethod]
    public void TupleToObject_WithTypeConversion_Success()
    {
        // Arrange - using string for age which should be converted to BigInteger
        var tuple = CreateTuple("Alice", "Smith", "42");

        // Act
        var user = this.converter.TupleToObject<PositionalUser>(tuple);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice", user.FirstName);
        Assert.AreEqual("Smith", user.LastName);
        Assert.AreEqual(BigInteger.Parse("42"), user.Age);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void TupleToObject_WithIncompatibleType_ThrowsException()
    {
        // Arrange - using a value that can't be converted to BigInteger
        var tuple = CreateTuple("Bob", "Johnson", "not-a-number");

        // Act - should throw
        this.converter.TupleToObject<PositionalUser>(tuple);
    }

    [TestMethod]
    public void TupleToObject_WithFewerProperties_Success()
    {
        // Arrange - tuple with more elements than target has properties
        var tuple = CreateTuple("Charlie", "Brown", BigInteger.Parse("8"), "extra value");

        // Act
        var user = this.converter.TupleToObject<PositionalUser>(tuple);

        // Assert - extra tuple element should be ignored
        Assert.IsNotNull(user);
        Assert.AreEqual("Charlie", user.FirstName);
        Assert.AreEqual("Brown", user.LastName);
        Assert.AreEqual(BigInteger.Parse("8"), user.Age);
    }

    [TestMethod]
    public void TupleToObject_WithMoreProperties_Success()
    {
        // Arrange - tuple with fewer elements than target has properties
        var tuple = CreateTuple("David", "Miller");

        // Act
        var user = this.converter.TupleToObject<PositionalUser>(tuple);

        // Assert - unmapped property should have default value
        Assert.IsNotNull(user);
        Assert.AreEqual("David", user.FirstName);
        Assert.AreEqual("Miller", user.LastName);
        Assert.AreEqual(BigInteger.Zero, user.Age); // Default value for BigInteger
    }

    // Helper method to create tuples with variable number of elements
    private static ITuple CreateTuple(params object?[] values)
    {
        switch (values.Length)
        {
            case 1:
                return Tuple.Create(values[0]);
            case 2:
                return Tuple.Create(values[0], values[1]);
            case 3:
                return Tuple.Create(values[0], values[1], values[2]);
            case 4:
                return Tuple.Create(values[0], values[1], values[2], values[3]);
            case 5:
                return Tuple.Create(values[0], values[1], values[2], values[3], values[4]);
            case 6:
                return Tuple.Create(values[0], values[1], values[2], values[3], values[4], values[5]);
            case 7:
                return Tuple.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
            case 8:
                return Tuple.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            default:
                throw new ArgumentException($"Cannot create tuple with {values.Length} elements", nameof(values));
        }
    }
}

public class NullablePositionalUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public BigInteger Age { get; set; }
}