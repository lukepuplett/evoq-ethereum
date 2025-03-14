using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Tests;

[TestClass]
public class EthereumAmountsTests
{
    [TestMethod]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var weiValue = new BigInteger(123);
        var unit = EthereumUnit.Ether;

        // Act
        var amount = new EthereumAmount(weiValue, unit);

        // Assert
        Assert.AreEqual(weiValue, amount.WeiValue);
        Assert.AreEqual(unit, amount.DisplayUnit);
    }

    [TestMethod]
    public void FromWei_CreatesCorrectAmount()
    {
        // Arrange
        var wei = new BigInteger(1000000000);

        // Act
        var amount = EthereumAmount.FromWei(wei);

        // Assert
        Assert.AreEqual(wei, amount.WeiValue);
        Assert.AreEqual(EthereumUnit.Wei, amount.DisplayUnit);
    }

    [TestMethod]
    public void FromEther_CreatesCorrectAmountPreservingDecimalPrecision()
    {
        // Arrange
        decimal ether = 1.23m;

        // Act
        var amount = EthereumAmount.FromEther(ether);

        // Assert
        Assert.AreEqual(EthereumUnit.Ether, amount.DisplayUnit);
        Assert.AreEqual(ether, amount.ToEther(), "Decimal precision should be preserved");
    }

    [TestMethod]
    public void ToWei_ConvertsCorrectly()
    {
        // Arrange
        var weiAmount = EthereumAmount.FromWei(1000000000000000000);
        var etherAmount = EthereumAmount.FromEther(1);

        // Act & Assert
        Assert.AreEqual(new BigInteger(1000000000000000000), weiAmount.ToWei(), "Wei amount should convert to correct Wei value");
        Assert.AreEqual(new BigInteger(1000000000000000000), etherAmount.ToWei(), "Ether amount should convert to correct Wei value");
    }

    [TestMethod]
    public void ToEther_ConvertsCorrectly()
    {
        // Arrange
        var weiAmount = EthereumAmount.FromWei(1000000000000000000);
        var etherAmount = EthereumAmount.FromEther(1);

        // Act & Assert
        Assert.AreEqual(1m, weiAmount.ToEther(), "Wei amount should convert to correct Ether value");
        Assert.AreEqual(1m, etherAmount.ToEther(), "Ether amount should convert to correct Ether value");
    }

    [TestMethod]
    public void ConvertTo_ChangesUnitCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);

        // Act
        var inWei = oneEther.ConvertTo(EthereumUnit.Wei);
        var inEther = oneEther.ConvertTo(EthereumUnit.Ether);

        // Assert
        Assert.AreEqual(EthereumUnit.Wei, inWei.DisplayUnit, "Unit should be changed to Wei");
        Assert.AreEqual(EthereumUnit.Ether, inEther.DisplayUnit, "Unit should remain Ether");

        Assert.AreEqual(oneEther.ToWei(), inWei.ToWei(), "Value should be preserved when converting to Wei");
        Assert.AreEqual(oneEther.ToEther(), inEther.ToEther(), "Value should be preserved when converting to Ether");
    }

    [TestMethod]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        var weiAmount = EthereumAmount.FromWei(123);
        var etherAmount = EthereumAmount.FromEther(7.89m);

        // Act
        var weiString = weiAmount.ToString();
        var etherString = etherAmount.ToString();

        // Assert
        Assert.AreEqual("123 Wei", weiString, "Wei amount should be formatted correctly");
        Assert.IsTrue(etherString.StartsWith("7.89"), "Ether amount should start with the correct value");
        Assert.IsTrue(etherString.EndsWith("ETH"), "Ether amount should end with ETH");
    }

    [TestMethod]
    public void RoundTrip_PreservesValue()
    {
        // Arrange
        decimal originalEther = 1.23456789m;

        // Act
        var amount = EthereumAmount.FromEther(originalEther);
        decimal roundTrippedEther = amount.ToEther();

        // Assert
        Assert.AreEqual(originalEther, roundTrippedEther, "Value should be preserved in round trip conversion");
    }

    [TestMethod]
    public void Equals_ComparesValuesCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var oneEtherAgain = EthereumAmount.FromEther(1);
        var oneEtherInWei = EthereumAmount.FromWei(1000000000000000000);
        var twoEther = EthereumAmount.FromEther(2);

        // Act & Assert
        Assert.IsTrue(oneEther.Equals(oneEtherAgain), "Same Ether amounts should be equal");
        Assert.IsTrue(oneEther.Equals(oneEtherInWei), "Equivalent amounts in different units should be equal");
        Assert.IsFalse(oneEther.Equals(twoEther), "Different amounts should not be equal");
        Assert.IsFalse(oneEther.Equals(null), "Amount should not equal null");
        Assert.IsFalse(oneEther.Equals("not an EthereumAmount"), "Amount should not equal different type");
    }

    [TestMethod]
    public void GetHashCode_ReturnsSameValueForEqualAmounts()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var oneEtherInWei = EthereumAmount.FromWei(1000000000000000000);

        // Act & Assert
        Assert.AreEqual(oneEther.GetHashCode(), oneEtherInWei.GetHashCode(),
            "Equal amounts should have the same hash code");
    }

    [TestMethod]
    public void CompareTo_OrdersAmountsCorrectly()
    {
        // Arrange
        var smallAmount = EthereumAmount.FromWei(1000000000000000); // 0.001 Ether
        var mediumAmount = EthereumAmount.FromWei(2000000000000000); // 0.002 Ether
        var largeAmount = EthereumAmount.FromEther(1);

        // Act & Assert
        Assert.IsTrue(smallAmount.CompareTo(mediumAmount) < 0, "Smaller amount should compare less than larger amount");
        Assert.IsTrue(mediumAmount.CompareTo(smallAmount) > 0, "Larger amount should compare greater than smaller amount");
        Assert.AreEqual(0, mediumAmount.CompareTo(mediumAmount), "Equal amounts should compare as equal");
        Assert.IsTrue(largeAmount.CompareTo(smallAmount) > 0, "Much larger amount should compare greater than smaller amount");
    }

    [TestMethod]
    public void OperatorEquals_ComparesCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var oneEtherInWei = EthereumAmount.FromWei(1000000000000000000);
        var twoEther = EthereumAmount.FromEther(2);

        // Act & Assert
        Assert.IsTrue(oneEther == oneEtherInWei, "Equal amounts should be equal with == operator");
        Assert.IsFalse(oneEther == twoEther, "Different amounts should not be equal with == operator");
    }

    [TestMethod]
    public void OperatorNotEquals_ComparesCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var oneEtherInWei = EthereumAmount.FromWei(1000000000000000000);
        var twoEther = EthereumAmount.FromEther(2);

        // Act & Assert
        Assert.IsFalse(oneEther != oneEtherInWei, "Equal amounts should not be unequal with != operator");
        Assert.IsTrue(oneEther != twoEther, "Different amounts should be unequal with != operator");
    }

    [TestMethod]
    public void ComparisonOperators_CompareCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var twoEther = EthereumAmount.FromEther(2);
        var oneEtherAgain = EthereumAmount.FromEther(1);

        // Act & Assert
        Assert.IsTrue(oneEther < twoEther, "1 ETH should be less than 2 ETH");
        Assert.IsFalse(twoEther < oneEther, "2 ETH should not be less than 1 ETH");

        Assert.IsTrue(twoEther > oneEther, "2 ETH should be greater than 1 ETH");
        Assert.IsFalse(oneEther > twoEther, "1 ETH should not be greater than 2 ETH");

        Assert.IsTrue(oneEther <= twoEther, "1 ETH should be less than or equal to 2 ETH");
        Assert.IsTrue(oneEther <= oneEtherAgain, "1 ETH should be less than or equal to 1 ETH");
        Assert.IsFalse(twoEther <= oneEther, "2 ETH should not be less than or equal to 1 ETH");

        Assert.IsTrue(twoEther >= oneEther, "2 ETH should be greater than or equal to 1 ETH");
        Assert.IsTrue(oneEther >= oneEtherAgain, "1 ETH should be greater than or equal to 1 ETH");
        Assert.IsFalse(oneEther >= twoEther, "1 ETH should not be greater than or equal to 2 ETH");
    }

    [TestMethod]
    public void Addition_CombinesAmountsCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var twoEther = EthereumAmount.FromEther(2);
        var threeEther = EthereumAmount.FromEther(3);
        var oneGwei = EthereumAmount.FromWei(1000000000); // 1 Gwei = 10^9 Wei

        // Act
        var sum1 = oneEther + twoEther;
        var sum2 = oneEther + oneGwei;

        // Assert
        Assert.AreEqual(threeEther, sum1, "1 ETH + 2 ETH should equal 3 ETH");
        Assert.AreEqual(1.000000001m, sum2.ToEther(), "1 ETH + 1 Gwei should equal 1.000000001 ETH");
        Assert.AreEqual(oneEther.DisplayUnit, sum1.DisplayUnit, "Addition should preserve the left operand's display unit");
    }

    [TestMethod]
    public void Subtraction_ReducesAmountsCorrectly()
    {
        // Arrange
        var threeEther = EthereumAmount.FromEther(3);
        var twoEther = EthereumAmount.FromEther(2);
        var oneEther = EthereumAmount.FromEther(1);
        var oneGwei = EthereumAmount.FromWei(1000000000); // 1 Gwei = 10^9 Wei

        // Act
        var diff1 = threeEther - twoEther;
        var diff2 = oneEther - oneGwei;

        // Assert
        Assert.AreEqual(oneEther, diff1, "3 ETH - 2 ETH should equal 1 ETH");
        Assert.AreEqual(0.999999999m, diff2.ToEther(), "1 ETH - 1 Gwei should equal 0.999999999 ETH");
        Assert.AreEqual(threeEther.DisplayUnit, diff1.DisplayUnit, "Subtraction should preserve the left operand's display unit");
    }

    [TestMethod]
    public void Addition_WithDifferentUnits_WorksCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var oneWei = EthereumAmount.FromWei(1);
        var expectedWei = new BigInteger(1000000000000000001);

        // Act
        var sumInEther = oneEther + oneWei;
        var sumInWei = oneWei + oneEther;

        // Assert
        Assert.AreEqual(expectedWei, sumInEther.ToWei(), "1 ETH + 1 Wei should equal 1000000000000000001 Wei");
        Assert.AreEqual(expectedWei, sumInWei.ToWei(), "1 Wei + 1 ETH should equal 1000000000000000001 Wei");
        Assert.AreEqual(EthereumUnit.Ether, sumInEther.DisplayUnit, "Sum should have Ether as display unit");
        Assert.AreEqual(EthereumUnit.Wei, sumInWei.DisplayUnit, "Sum should have Wei as display unit");
    }

    [TestMethod]
    public void Subtraction_WithDifferentUnits_WorksCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var oneWei = EthereumAmount.FromWei(1);
        var expectedWei = new BigInteger(999999999999999999);

        // Act
        var diffInEther = oneEther - oneWei;

        // Assert
        Assert.AreEqual(expectedWei, diffInEther.ToWei(), "1 ETH - 1 Wei should equal 999999999999999999 Wei");
        Assert.AreEqual(EthereumUnit.Ether, diffInEther.DisplayUnit, "Difference should have Ether as display unit");
    }

    [TestMethod]
    public void Subtraction_ResultingInNegativeAmount_ThrowsException()
    {
        // Arrange
        var smallAmount = EthereumAmount.FromWei(1);
        var largeAmount = EthereumAmount.FromEther(1);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => smallAmount - largeAmount,
            "Subtracting a larger amount from a smaller one should throw an exception");
    }

    [TestMethod]
    public void MultiplyByDecimal_ScalesAmountCorrectly()
    {
        // Arrange
        var twoEther = EthereumAmount.FromEther(2);
        decimal factor = 1.5m;

        // Act
        var result = twoEther * factor;

        // Assert
        Assert.AreEqual(3m, result.ToEther(), "2 ETH * 1.5 should equal 3 ETH");
        Assert.AreEqual(twoEther.DisplayUnit, result.DisplayUnit, "Multiplication should preserve the display unit");
    }

    [TestMethod]
    public void DivideByDecimal_ScalesAmountCorrectly()
    {
        // Arrange
        var twoEther = EthereumAmount.FromEther(2);
        decimal divisor = 4m;

        // Act
        var result = twoEther / divisor;

        // Assert
        Assert.AreEqual(0.5m, result.ToEther(), "2 ETH / 4 should equal 0.5 ETH");
        Assert.AreEqual(twoEther.DisplayUnit, result.DisplayUnit, "Division should preserve the display unit");
    }

    [TestMethod]
    public void DivideByZero_ThrowsException()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        decimal divisor = 0m;

        // Act & Assert
        Assert.ThrowsException<DivideByZeroException>(() => oneEther / divisor,
            "Dividing by zero should throw an exception");
    }

    [TestMethod]
    public void ChainedOperations_CalculateCorrectly()
    {
        // Arrange
        var oneEther = EthereumAmount.FromEther(1);
        var twoEther = EthereumAmount.FromEther(2);
        var halfEther = EthereumAmount.FromEther(0.5m);

        // Act
        var result = oneEther + twoEther - halfEther;

        // Assert
        Assert.AreEqual(2.5m, result.ToEther(), "1 ETH + 2 ETH - 0.5 ETH should equal 2.5 ETH");
    }
}