using System.Numerics;

namespace Evoq.Ethereum.ABI.Conversion;

[TestClass]
public class DefaultValueCheckerTests
{
    private DefaultValueChecker checker = new DefaultValueChecker();

    [TestMethod]
    public void NullValue_IsDefault()
    {
        // Arrange & Act
        bool result = checker.HasNonDefaultValue(null, typeof(string));

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValueTypes_DefaultValues_AreDefault()
    {
        // Arrange & Act
        bool intResult = checker.HasNonDefaultValue(0, typeof(int));
        bool boolResult = checker.HasNonDefaultValue(false, typeof(bool));
        bool doubleResult = checker.HasNonDefaultValue(0.0, typeof(double));
        bool bigIntResult = checker.HasNonDefaultValue(BigInteger.Zero, typeof(BigInteger));

        // Assert
        Assert.IsFalse(intResult);
        Assert.IsFalse(boolResult);
        Assert.IsFalse(doubleResult);
        Assert.IsFalse(bigIntResult);
    }

    [TestMethod]
    public void ValueTypes_NonDefaultValues_AreNonDefault()
    {
        // Arrange & Act
        bool intResult = checker.HasNonDefaultValue(42, typeof(int));
        bool boolResult = checker.HasNonDefaultValue(true, typeof(bool));
        bool doubleResult = checker.HasNonDefaultValue(3.14, typeof(double));
        bool bigIntResult = checker.HasNonDefaultValue(BigInteger.Parse("123"), typeof(BigInteger));

        // Assert
        Assert.IsTrue(intResult);
        Assert.IsTrue(boolResult);
        Assert.IsTrue(doubleResult);
        Assert.IsTrue(bigIntResult);
    }

    [TestMethod]
    public void NullableValueTypes_Null_IsDefault()
    {
        // Arrange
        int? nullInt = null;
        bool? nullBool = null;

        // Act
        bool intResult = checker.HasNonDefaultValue(nullInt, typeof(int?));
        bool boolResult = checker.HasNonDefaultValue(nullBool, typeof(bool?));

        // Assert
        Assert.IsFalse(intResult);
        Assert.IsFalse(boolResult);
    }

    [TestMethod]
    public void NullableValueTypes_NonNull_IsNonDefault()
    {
        // Arrange
        int? nonNullInt = 0; // Even though it's the default int value, it's not null
        bool? nonNullBool = false;

        // Act
        bool intResult = checker.HasNonDefaultValue(nonNullInt, typeof(int?));
        bool boolResult = checker.HasNonDefaultValue(nonNullBool, typeof(bool?));

        // Assert
        Assert.IsTrue(intResult);
        Assert.IsTrue(boolResult);
    }

    [TestMethod]
    public void String_Null_IsDefault()
    {
        // Arrange & Act
        bool result = checker.HasNonDefaultValue(null, typeof(string));

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void String_Empty_IsDefault()
    {
        // Arrange & Act
        bool result = checker.HasNonDefaultValue("", typeof(string));

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void String_NonEmpty_IsNonDefault()
    {
        // Arrange & Act
        bool result = checker.HasNonDefaultValue("Hello", typeof(string));

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Array_Empty_IsDefault()
    {
        // Arrange
        int[] emptyArray = new int[0];

        // Act
        bool result = checker.HasNonDefaultValue(emptyArray, typeof(int[]));

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Array_NonEmpty_IsNonDefault()
    {
        // Arrange
        int[] nonEmptyArray = new int[] { 1, 2, 3 };

        // Act
        bool result = checker.HasNonDefaultValue(nonEmptyArray, typeof(int[]));

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void List_Empty_IsDefault()
    {
        // Arrange
        var emptyList = new List<string>();

        // Act
        bool result = checker.HasNonDefaultValue(emptyList, typeof(List<string>));

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void List_NonEmpty_IsNonDefault()
    {
        // Arrange
        var nonEmptyList = new List<string> { "item1", "item2" };

        // Act
        bool result = checker.HasNonDefaultValue(nonEmptyList, typeof(List<string>));

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SimpleObject_AllDefaultProperties_IsDefault()
    {
        // Arrange
        var obj = new SimpleTestClass();

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void SimpleObject_OneNonDefaultProperty_IsNonDefault()
    {
        // Arrange
        var obj = new SimpleTestClass { Name = "Test" };

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void NestedObject_AllDefaultProperties_IsDefault()
    {
        // Arrange
        var obj = new NestedTestClass
        {
            Name = "",
            Inner = new SimpleTestClass()
        };

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void NestedObject_OuterPropertyNonDefault_IsNonDefault()
    {
        // Arrange
        var obj = new NestedTestClass
        {
            Name = "Test",
            Inner = new SimpleTestClass()
        };

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void NestedObject_InnerPropertyNonDefault_IsNonDefault()
    {
        // Arrange
        var obj = new NestedTestClass
        {
            Name = "",
            Inner = new SimpleTestClass { Age = 25 }
        };

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ObjectWithCollection_EmptyCollection_IsDefault()
    {
        // Arrange
        var obj = new CollectionTestClass
        {
            Name = "",
            Items = new List<string>()
        };

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ObjectWithCollection_NonEmptyCollection_IsNonDefault()
    {
        // Arrange
        var obj = new CollectionTestClass
        {
            Name = "",
            Items = new List<string> { "item1" }
        };

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ObjectWithNestedCollection_AllDefault_IsDefault()
    {
        // Arrange
        var obj = new NestedCollectionTestClass
        {
            Name = "",
            Children = new List<SimpleTestClass>
            {
                new SimpleTestClass(),
                new SimpleTestClass()
            }
        };

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ObjectWithNestedCollection_OneChildNonDefault_IsNonDefault()
    {
        // Arrange
        var obj = new NestedCollectionTestClass
        {
            Name = "",
            Children = new List<SimpleTestClass>
            {
                new SimpleTestClass(),
                new SimpleTestClass { Age = 10 }
            }
        };

        // Act
        bool result = checker.HasNonDefaultComplexValue(obj);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EthereumAddress_Default_IsDefault()
    {
        // Arrange
        var address = default(EthereumAddress);

        // Act
        bool result = checker.HasNonDefaultValue(address, typeof(EthereumAddress));

        // Assert
        Assert.IsFalse(result, "Default(EthereumAddress) should be default");
    }

    [TestMethod]
    public void EthereumAddress_Zero_IsNotDefault()
    {
        // Arrange
        var address = EthereumAddress.Zero;

        // Act
        bool result = checker.HasNonDefaultValue(address, typeof(EthereumAddress));

        // Assert
        Assert.IsTrue(result, "EthereumAddress.Zero should be non-default");
    }

    [TestMethod]
    public void EthereumAddress_NonDefault_IsNonDefault()
    {
        // Arrange
        var address = EthereumAddress.Parse("0x1234567890123456789012345678901234567890");

        // Act
        bool result = checker.HasNonDefaultValue(address, typeof(EthereumAddress));

        // Assert
        Assert.IsTrue(result);
    }

    // Test classes
    private class SimpleTestClass
    {
        public string Name { get; set; } = "";
        public int Age { get; set; } = 0;
        public bool IsActive { get; set; } = false;
    }

    private class NestedTestClass
    {
        public string Name { get; set; } = "";
        public SimpleTestClass Inner { get; set; } = new SimpleTestClass();
    }

    private class CollectionTestClass
    {
        public string Name { get; set; } = "";
        public List<string> Items { get; set; } = new List<string>();
    }

    private class NestedCollectionTestClass
    {
        public string Name { get; set; } = "";
        public List<SimpleTestClass> Children { get; set; } = new List<SimpleTestClass>();
    }
}