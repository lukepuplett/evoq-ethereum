using System.Numerics;

namespace Evoq.Ethereum.Tests;

[TestClass]
public class WeiAmountsTests
{
    // Current Ether price in USD (as a decimal: 1926.42)
    // We need to convert this to a BigInteger in a way that works with the methods
    private static readonly BigInteger EtherPriceInUsd = BigInteger.Parse("1926");  // $1,926 (rounded to nearest dollar)

    [TestMethod]
    public void LocalCurrencyInWei_OneDollar_ReturnsCorrectWeiAmount()
    {
        // Act
        // Calculate how much wei equals $1
        BigInteger weiForOneDollar = WeiAmounts.LocalCurrencyInWei(EtherPriceInUsd);

        // Assert
        // $1 should be approximately 1/1926 of an Ether, which is about 5.19 * 10^14 wei
        // But since we're using integer division, it will be less precise
        BigInteger expectedWeiApprox = BigInteger.Parse("519000000000000");

        // Check if it's within an order of magnitude (since we're using integer math)
        Assert.IsTrue(IsWithinOrderOfMagnitude(weiForOneDollar, expectedWeiApprox),
            $"Expected approximately {expectedWeiApprox} wei, but got {weiForOneDollar}");
    }

    [TestMethod]
    public void WeiInLocalCurrency_OneEther_ReturnsEtherPrice()
    {
        // Arrange
        BigInteger oneEther = WeiAmounts.Ether;  // 10^18 wei

        // Act
        BigInteger usdValue = WeiAmounts.WeiInLocalCurrency(EtherPriceInUsd, oneEther);

        // Assert
        // 1 Ether should be worth approximately the Ether price
        Assert.AreEqual(EtherPriceInUsd, usdValue,
            $"1 Ether should be worth approximately {EtherPriceInUsd} USD, but got {usdValue}");
    }

    [TestMethod]
    public void WeiInLocalCurrency_TenEther_ReturnsTenTimesEtherPrice()
    {
        // Arrange
        BigInteger tenEther = WeiAmounts.Ether * 10;  // 10 * 10^18 wei

        // Act
        BigInteger usdValue = WeiAmounts.WeiInLocalCurrency(EtherPriceInUsd, tenEther);

        // Assert
        // 10 Ether should be worth 10 times the Ether price
        Assert.AreEqual(EtherPriceInUsd * 10, usdValue,
            $"10 Ether should be worth {EtherPriceInUsd * 10} USD, but got {usdValue}");
    }

    [TestMethod]
    public void WeiInLocalCurrency_TransactionFee_ReturnsReasonableAmount()
    {
        // Arrange
        // Typical transaction fee: 21,000 gas * 20 Gwei
        BigInteger gasLimit = BigInteger.Parse("21000");  // Standard ETH transfer
        BigInteger gasPrice = BigInteger.Parse("20000000000");  // 20 Gwei
        BigInteger transactionFeeInWei = gasLimit * gasPrice;

        // Act
        BigInteger usdValue = WeiAmounts.WeiInLocalCurrency(EtherPriceInUsd, transactionFeeInWei);

        // Assert
        // A typical transaction fee should be in a reasonable range (less than $2)
        Assert.IsTrue(usdValue < 2,
            $"Transaction fee should be less than $2, but got ${usdValue}");
    }

    [TestMethod]
    public void RoundTrip_ConversionIsConsistent()
    {
        // Arrange
        BigInteger originalUsd = BigInteger.Parse("100");  // $100

        // Act
        BigInteger weiAmount = WeiAmounts.LocalCurrencyInWei(EtherPriceInUsd) * originalUsd;
        BigInteger roundTripUsd = WeiAmounts.WeiInLocalCurrency(EtherPriceInUsd, weiAmount);

        // Assert
        // Due to integer division, we might lose some precision, but it should be close
        Assert.IsTrue(IsWithinTenPercent(originalUsd, roundTripUsd),
            $"Round trip conversion should be close to original ${originalUsd}, but got ${roundTripUsd}");
    }

    // Helper method to check if two values are within the same order of magnitude
    private bool IsWithinOrderOfMagnitude(BigInteger value1, BigInteger value2)
    {
        if (value1 == 0 || value2 == 0)
        {
            return value1 == value2;
        }

        // Get the number of digits in each value
        int digits1 = value1.ToString().Length;
        int digits2 = value2.ToString().Length;

        // Check if they're within one order of magnitude
        return Math.Abs(digits1 - digits2) <= 1;
    }

    // Helper method to check if two values are within 10% of each other
    private bool IsWithinTenPercent(BigInteger value1, BigInteger value2)
    {
        if (value1 == 0 || value2 == 0)
        {
            return value1 == value2;
        }

        BigInteger difference = BigInteger.Abs(value1 - value2);
        BigInteger tenPercent = BigInteger.Max(value1, value2) / 10;

        return difference <= tenPercent;
    }
}
