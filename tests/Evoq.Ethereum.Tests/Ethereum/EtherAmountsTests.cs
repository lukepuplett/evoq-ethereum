using System;
using System.Numerics;
using Evoq.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Tests;

[TestClass]
public class EtherAmountsTests
{
    [TestMethod]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var weiValue = new BigInteger(123);
        var unit = EthereumUnit.Ether;

        // Act
        var amount = new EtherAmount(weiValue, unit);

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
        var amount = EtherAmount.FromWei(wei);

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
        var amount = EtherAmount.FromEther(ether);

        // Assert
        Assert.AreEqual(EthereumUnit.Ether, amount.DisplayUnit);
        Assert.AreEqual(ether, amount.ToEther(), "Decimal precision should be preserved");
    }

    [TestMethod]
    public void ToWei_ConvertsCorrectly()
    {
        // Arrange
        var weiAmount = EtherAmount.FromWei(1000000000000000000);
        var etherAmount = EtherAmount.FromEther(1);

        // Act & Assert
        Assert.AreEqual(new BigInteger(1000000000000000000), weiAmount.ToWei(), "Wei amount should convert to correct Wei value");
        Assert.AreEqual(new BigInteger(1000000000000000000), etherAmount.ToWei(), "Ether amount should convert to correct Wei value");
    }

    [TestMethod]
    public void ToEther_ConvertsCorrectly()
    {
        // Arrange
        var weiAmount = EtherAmount.FromWei(1000000000000000000);
        var etherAmount = EtherAmount.FromEther(1);

        // Act & Assert
        Assert.AreEqual(1m, weiAmount.ToEther(), "Wei amount should convert to correct Ether value");
        Assert.AreEqual(1m, etherAmount.ToEther(), "Ether amount should convert to correct Ether value");
    }

    [TestMethod]
    public void ConvertTo_ChangesUnitCorrectly()
    {
        // Arrange
        var oneEther = EtherAmount.FromEther(1);

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
        var weiAmount = EtherAmount.FromWei(123);
        var etherAmount = EtherAmount.FromEther(7.89m);
        var largeWeiAmount = EtherAmount.FromWei(1234567890);

        // Act
        var weiString = weiAmount.ToString();
        var etherString = etherAmount.ToString();
        var largeWeiString = largeWeiAmount.ToString();

        // Assert
        Assert.AreEqual("123 Wei", weiString, "Wei amount should be formatted correctly");
        Assert.IsTrue(etherString.StartsWith("7.89"), "Ether amount should start with the correct value");
        Assert.IsTrue(etherString.EndsWith("ETH"), "Ether amount should end with ETH");
        Assert.AreEqual("1,234,567,890 Wei", largeWeiString, "Large Wei amount should include thousands separators");
    }

    [TestMethod]
    public void RoundTrip_PreservesValue()
    {
        // Arrange
        decimal originalEther = 1.23456789m;

        // Act
        var amount = EtherAmount.FromEther(originalEther);
        decimal roundTrippedEther = amount.ToEther();

        // Assert
        Assert.AreEqual(originalEther, roundTrippedEther, "Value should be preserved in round trip conversion");
    }

    [TestMethod]
    public void Equals_ComparesValuesCorrectly()
    {
        // Arrange
        var oneEther = EtherAmount.FromEther(1);
        var oneEtherAgain = EtherAmount.FromEther(1);
        var oneEtherInWei = EtherAmount.FromWei(1000000000000000000);
        var twoEther = EtherAmount.FromEther(2);

        // Act & Assert
        Assert.IsTrue(oneEther.Equals(oneEtherAgain), "Same Ether amounts should be equal");
        Assert.IsTrue(oneEther.Equals(oneEtherInWei), "Equivalent amounts in different units should be equal");
        Assert.IsFalse(oneEther.Equals(twoEther), "Different amounts should not be equal");
        Assert.IsFalse(oneEther.Equals(null!), "Amount should not equal null");
        Assert.IsFalse(oneEther.Equals("not an EthereumAmount"), "Amount should not equal different type");
    }

    [TestMethod]
    public void GetHashCode_ReturnsSameValueForEqualAmounts()
    {
        // Arrange
        var oneEther = EtherAmount.FromEther(1);
        var oneEtherInWei = EtherAmount.FromWei(1000000000000000000);

        // Act & Assert
        Assert.AreEqual(oneEther.GetHashCode(), oneEtherInWei.GetHashCode(),
            "Equal amounts should have the same hash code");
    }

    [TestMethod]
    public void CompareTo_OrdersAmountsCorrectly()
    {
        // Arrange
        var smallAmount = EtherAmount.FromWei(1000000000000000); // 0.001 Ether
        var mediumAmount = EtherAmount.FromWei(2000000000000000); // 0.002 Ether
        var largeAmount = EtherAmount.FromEther(1);

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
        var oneEther = EtherAmount.FromEther(1);
        var oneEtherInWei = EtherAmount.FromWei(1000000000000000000);
        var twoEther = EtherAmount.FromEther(2);

        // Act & Assert
        Assert.IsTrue(oneEther == oneEtherInWei, "Equal amounts should be equal with == operator");
        Assert.IsFalse(oneEther == twoEther, "Different amounts should not be equal with == operator");
    }

    [TestMethod]
    public void OperatorNotEquals_ComparesCorrectly()
    {
        // Arrange
        var oneEther = EtherAmount.FromEther(1);
        var oneEtherInWei = EtherAmount.FromWei(1000000000000000000);
        var twoEther = EtherAmount.FromEther(2);

        // Act & Assert
        Assert.IsFalse(oneEther != oneEtherInWei, "Equal amounts should not be unequal with != operator");
        Assert.IsTrue(oneEther != twoEther, "Different amounts should be unequal with != operator");
    }

    [TestMethod]
    public void ComparisonOperators_CompareCorrectly()
    {
        // Arrange
        var oneEther = EtherAmount.FromEther(1);
        var twoEther = EtherAmount.FromEther(2);
        var oneEtherAgain = EtherAmount.FromEther(1);

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
        var oneEther = EtherAmount.FromEther(1);
        var twoEther = EtherAmount.FromEther(2);
        var threeEther = EtherAmount.FromEther(3);
        var oneGwei = EtherAmount.FromWei(1000000000); // 1 Gwei = 10^9 Wei

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
        var threeEther = EtherAmount.FromEther(3);
        var twoEther = EtherAmount.FromEther(2);
        var oneEther = EtherAmount.FromEther(1);
        var oneGwei = EtherAmount.FromWei(1000000000); // 1 Gwei = 10^9 Wei

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
        var oneEther = EtherAmount.FromEther(1);
        var oneWei = EtherAmount.FromWei(1);
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
        var oneEther = EtherAmount.FromEther(1);
        var oneWei = EtherAmount.FromWei(1);
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
        var smallAmount = EtherAmount.FromWei(1);
        var largeAmount = EtherAmount.FromEther(1);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => smallAmount - largeAmount,
            "Subtracting a larger amount from a smaller one should throw an exception");
    }

    [TestMethod]
    public void MultiplyByDecimal_ScalesAmountCorrectly()
    {
        // Arrange
        var twoEther = EtherAmount.FromEther(2);
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
        var twoEther = EtherAmount.FromEther(2);
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
        var oneEther = EtherAmount.FromEther(1);
        decimal divisor = 0m;

        // Act & Assert
        Assert.ThrowsException<DivideByZeroException>(() => oneEther / divisor,
            "Dividing by zero should throw an exception");
    }

    [TestMethod]
    public void ChainedOperations_CalculateCorrectly()
    {
        // Arrange
        var oneEther = EtherAmount.FromEther(1);
        var twoEther = EtherAmount.FromEther(2);
        var halfEther = EtherAmount.FromEther(0.5m);

        // Act
        var result = oneEther + twoEther - halfEther;

        // Assert
        Assert.AreEqual(2.5m, result.ToEther(), "1 ETH + 2 ETH - 0.5 ETH should equal 2.5 ETH");
    }

    [TestMethod]
    public void VerySmallAmount_HandledCorrectly()
    {
        // Arrange
        var oneWei = EtherAmount.FromWei(1);

        // Act
        var etherValue = oneWei.ToEther();
        var backToWei = EtherAmount.FromEther(etherValue);

        // Assert
        Assert.AreEqual(1, oneWei.ToWei(), "One Wei should be preserved");
        Assert.AreEqual(0.000000000000000001m, etherValue, "One Wei should convert to 10^-18 Ether");
        Assert.AreEqual(oneWei.ToWei(), backToWei.ToWei(), "Converting to Ether and back should preserve value");
    }

    [TestMethod]
    public void VeryLargeAmount_HandledCorrectly()
    {
        // Arrange - 100 million Ether (more than currently exists)
        var largeEtherAmount = EtherAmount.FromEther(100_000_000m);
        var expectedWei = new BigInteger(100_000_000) * new BigInteger(1000000000000000000);

        // Act
        var weiValue = largeEtherAmount.ToWei();
        var backToEther = EtherAmount.FromWei(weiValue).ToEther();

        // Assert
        Assert.AreEqual(expectedWei, weiValue, "Large Ether amount should convert to correct Wei value");
        Assert.AreEqual(100_000_000m, backToEther, "Converting to Wei and back should preserve large values");
    }

    [TestMethod]
    public void ZeroAmount_HandledCorrectly()
    {
        // Arrange
        var zeroEther = EtherAmount.FromEther(0);
        var zeroWei = EtherAmount.FromWei(0);
        var oneEther = EtherAmount.FromEther(1);

        // Act
        var additionResult = zeroEther + oneEther;
        var subtractionResult = oneEther - zeroEther;
        var weiToEtherResult = zeroWei.ToEther();

        // Assert
        Assert.AreEqual(0, zeroEther.ToWei(), "Zero Ether should be zero Wei");
        Assert.AreEqual(0, zeroWei.ToWei(), "Zero Wei should be zero Wei");
        Assert.AreEqual("0 ETH", zeroEther.ToString(), "Zero Ether should format correctly");
        Assert.AreEqual("0 Wei", zeroWei.ToString(), "Zero Wei should format correctly");
        Assert.AreEqual(oneEther, additionResult, "Adding zero should not change value");
        Assert.AreEqual(oneEther, subtractionResult, "Subtracting zero should not change value");
        Assert.AreEqual(0m, weiToEtherResult, "Zero Wei should convert to zero Ether");
    }

    [TestMethod]
    public void MaximumPrecisionAmount_HandledCorrectly()
    {
        // Arrange - number with 18 decimal places (maximum Ether precision)
        decimal maxPrecisionEther = 1.123456789123456789m;
        var amount = EtherAmount.FromEther(maxPrecisionEther);

        // Act
        var weiValue = amount.ToWei();
        var backToEther = EtherAmount.FromWei(weiValue).ToEther();

        // Assert
        // Note: C# decimal has maximum 28-29 significant digits, so we'll get some precision loss
        // We'll check that we're within a single Wei of precision
        var expectedWei = new BigInteger(1123456789123456789);
        Assert.IsTrue(BigInteger.Abs(expectedWei - weiValue) <= 1,
            "Maximum precision Ether should convert to Wei within 1 Wei of precision");

        // For Ether representation, we expect to maintain the precision supported by decimal
        Assert.AreEqual(
            Math.Round(maxPrecisionEther, 18),
            Math.Round(backToEther, 18),
            "Should maintain precision within decimal limitations");
    }

    // Grok

    [TestMethod]
    public void HighPrecisionInput_RoundsCorrectly()
    {
        // Arrange - number with more than 18 decimal places
        decimal highPrecisionEther = 1.123456789123456789123456789m;

        // Act
        var amount = EtherAmount.FromEther(highPrecisionEther);

        // Assert
        // Should round to 18 decimal places
        Assert.AreEqual(1.123456789123456789m, amount.ToEther(),
            "Should round to 18 decimal places");
    }

    [TestMethod]
    public void MultiplicationRounding_PreservesPrecision()
    {
        // Arrange
        var oneEther = EtherAmount.FromEther(1m);
        var oneThirdEther = oneEther / 3m;

        // Act
        var backToOne = oneThirdEther * 3m;

        // Assert
        // We expect exactly 0.999999999999999999 due to the nature of decimal division
        Assert.AreEqual(0.999999999999999999m, backToOne.ToEther(),
            "Should get exactly 0.999999999999999999 when multiplying one third by 3");
    }

    [TestMethod]
    public void NegativeEtherInput_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            EtherAmount.FromEther(-1m),
            "Should reject negative Ether input");
    }

    [TestMethod]
    public void NegativeWeiInput_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            EtherAmount.FromWei(new BigInteger(-1)),
            "Should reject negative Wei input");
    }

    [TestMethod]
    public void VeryLargeWeiAmount_HandlesCorrectly()
    {
        // Arrange - approximately 1 billion ETH (far more than will ever exist)
        var billion = new BigInteger(1000000000);
        var weiInOneEther = new BigInteger(1000000000000000000);
        var largeWeiAmount = billion * weiInOneEther;

        // Act
        var amount = EtherAmount.FromWei(largeWeiAmount);

        // Assert
        Assert.AreEqual(1000000000m, amount.ToEther(),
            "Should handle conversion of very large Wei amounts");
        Assert.AreEqual(largeWeiAmount, amount.ToWei(),
            "Should preserve very large Wei amounts exactly");
    }

    [TestMethod]
    public void InvalidEthereumUnit_ThrowsException()
    {
        // Arrange
        var invalidUnit = (EthereumUnit)999;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new EtherAmount(BigInteger.One, invalidUnit),
            "Should reject invalid EthereumUnit values");
    }

    [TestMethod]
    public void ConvertToInvalidUnit_ThrowsException()
    {
        // Arrange
        var amount = EtherAmount.FromEther(1);
        var invalidUnit = (EthereumUnit)999;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            amount.ConvertTo(invalidUnit),
            "Should reject conversion to invalid unit");
    }

    [TestMethod]
    public void MultiplicationWithSmallValues_PreservesPrecision()
    {
        // Arrange
        var oneWei = EtherAmount.FromWei(1);

        // Act
        var result = oneWei * 0.5m;

        // Assert
        Assert.AreEqual(BigInteger.Zero, result.ToWei(),
            "Multiplying 1 Wei by 0.5 should round to zero Wei");
    }

    [TestMethod]
    public void DivisionOfSmallValues_HandlesCorrectly()
    {
        // Arrange
        var twoWei = EtherAmount.FromWei(2);

        // Act
        var result = twoWei / 2m;

        // Assert
        Assert.AreEqual(BigInteger.One, result.ToWei(),
            "Dividing 2 Wei by 2 should yield 1 Wei");
    }

    [TestMethod]
    public void ToString_WithCustomPrecision()
    {
        // Arrange
        var amount = EtherAmount.FromEther(1.23456789m);
        var largeAmount = EtherAmount.FromEther(1234567.89m);

        // Act & Assert
        Assert.AreEqual("1.23 ETH", amount.ToString(2),
            "Should format with specified decimal places");
        Assert.AreEqual("1.234568 ETH", amount.ToString(6),
            "Should round to specified decimal places");
        Assert.AreEqual("1,234,567.89 ETH", largeAmount.ToString(2),
            "Should format large numbers with thousands separators");
    }

    [TestMethod]
    public void FromHex_ValidHexString_CreatesCorrectAmount()
    {
        // Arrange
        var hex = Hex.Parse("0x0000000000000000000000000000000000000000000000000DE0B6B3A7640000"); // 1 Ether in Wei (1e18)

        // Act
        var amount = EtherAmount.FromHex(hex);

        // Assert
        Assert.AreEqual(1m, amount.ToEther(), "1 Ether in hex should convert to 1 Ether");
        Assert.AreEqual(EthereumUnit.Wei, amount.DisplayUnit, "Should default to Wei display unit");
    }

    [TestMethod]
    public void FromHex_SmallHexValue_CreatesCorrectAmount()
    {
        // Arrange
        var hex = Hex.Parse("0x0a"); // 10 Wei

        // Act
        var amount = EtherAmount.FromHex(hex);

        // Assert
        Assert.AreEqual(new BigInteger(10), amount.ToWei(), "10 in hex should convert to 10 Wei");
    }

    [TestMethod]
    public void FromHex_InvalidNegativeValue_ThrowsException()
    {
        // Arrange
        var hex = Hex.Parse("0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"); // -1 in two's complement

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => EtherAmount.FromHex(hex),
            "Should reject hex values that would result in negative amounts");
    }

    [TestMethod]
    public void ToLocalCurrency_OneEther_ReturnsCorrectUsdCents()
    {
        // Arrange
        var oneEther = EtherAmount.FromEther(1);
        var etherPriceInCents = new BigInteger(193045); // $1,930.45

        // Act
        var localCurrencyCents = oneEther.ToLocalCurrency(etherPriceInCents);

        // Assert
        Assert.AreEqual(etherPriceInCents, localCurrencyCents,
            "1 ETH should convert to 193,045 cents ($1,930.45)");
    }

    [TestMethod]
    public void ToLocalCurrency_SmallAmount_HandlesRoundingCorrectly()
    {
        // Arrange
        var smallAmount = EtherAmount.FromEther(0.0001m); // About 19 cents
        var etherPriceInCents = new BigInteger(193045);

        // Act
        var localCurrencyCents = smallAmount.ToLocalCurrency(etherPriceInCents);

        // Assert
        Assert.AreEqual(new BigInteger(19), localCurrencyCents,
            "0.0001 ETH should convert to approximately 19 cents");
    }

    [TestMethod]
    public void FromLocalCurrency_OneDollar_CreatesCorrectAmount()
    {
        // Arrange
        var oneDollarInCents = new BigInteger(100);
        var etherPriceInCents = new BigInteger(193045); // $1,930.45

        // Act
        var amount = EtherAmount.FromLocalCurrency(oneDollarInCents, etherPriceInCents);

        // Assert
        Assert.AreEqual(0.000518m, Math.Round(amount.ToEther(), 6),
            "1 USD should convert to approximately 0.000518 ETH");
    }

    [TestMethod]
    public void LocalCurrency_RoundTrip_PreservesValue()
    {
        // Arrange
        var originalCents = new BigInteger(100000); // $1,000.00
        var etherPriceInCents = new BigInteger(193045);

        // Act
        var amount = EtherAmount.FromLocalCurrency(originalCents, etherPriceInCents);
        var roundTrippedCents = amount.ToLocalCurrency(etherPriceInCents);

        // Assert
        // Due to integer division, we might lose a few cents, so we check if we're within 1 cent
        var difference = BigInteger.Abs(originalCents - roundTrippedCents);
        Assert.IsTrue(difference <= 1,
            $"Round trip conversion should preserve value within 1 cent. Difference: {difference} cents");
    }
}