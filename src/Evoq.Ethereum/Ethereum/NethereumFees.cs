using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace Evoq.Ethereum;

/// <summary>
/// A class for calculating transaction fees using Nethereum.
/// </summary>
public class FeesNethereum
{
    private readonly Web3 web3;

    //

    /// <summary>
    /// Create a new instance of the FeesNethereum class.
    /// </summary>
    /// <param name="web3">The Web3 instance to use for fee calculation.</param>
    public FeesNethereum(Web3 web3)
    {
        this.web3 = web3;
    }

    //

    /// <summary>
    /// Get the fees for a transaction.
    /// </summary>
    /// <param name="value">The value to send with the transaction.</param>
    /// <param name="gasLimit">The gas limit for the transaction.</param>
    /// <param name="getBribe">Takes a suggested bribe and should return the final bribe, which may just the suggested bribe.</param>
    /// <returns>The fees for the transaction.</returns>
    /// <exception cref="FeeCalculationException">Thrown if the fees cannot be calculated.</exception>
    public async Task<TransactionFees> CalculateFeesAsync(
        BigInteger value, BigInteger gasLimit, Func<BigInteger, BigInteger>? getBribe = null)
    {
        var blockCount = new HexBigInteger(1); // Requesting history for 1 block
        var highestBlock = BlockParameter.CreateLatest();
        var rewardPercentiles = new decimal[] { 10.0m }; // Using 10th percentile priority fee

        HexBigInteger baseFee;
        HexBigInteger maxPriorityFeePerGas;
        try
        {
            var feeHistory = await web3.Eth.FeeHistory.SendRequestAsync(blockCount, highestBlock, rewardPercentiles);

            baseFee = feeHistory.BaseFeePerGas[0];
            maxPriorityFeePerGas = feeHistory.Reward[0][0]; // first percentile priority fee in first block

            if (getBribe != null)
            {
                maxPriorityFeePerGas = new HexBigInteger(getBribe(maxPriorityFeePerGas.Value));
            }
        }
        catch (Exception ex)
        {
            var message = $"Unable to prepare transaction. Error calculating gas price. '{ex.Message}'";

            throw new FeeCalculationException(message, ex);
        }

        var maxFeePerGas = baseFee.Value + maxPriorityFeePerGas.Value;

        var fees = new TransactionFees(
            Value: value,
            GasLimit: gasLimit,
            MaxFeePerGas: maxFeePerGas,
            MaxPriorityFeePerGas: maxPriorityFeePerGas.Value);

        return fees;
    }
}
