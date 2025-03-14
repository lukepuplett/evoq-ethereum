using System;
using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Represents fee history data returned by eth_feeHistory RPC method.  
/// </summary>
public class FeeHistory
{
    /// <summary>
    /// The oldest block number in the returned range.
    /// </summary>
    public BigInteger OldestBlock { get; set; }

    /// <summary>
    /// Base fee per gas for each block in the returned range.
    /// </summary>
    public BigInteger[] BaseFeePerGas { get; set; } = Array.Empty<BigInteger>();

    /// <summary>
    /// Gas used ratio for each block in the returned range.
    /// </summary>
    public double[] GasUsedRatio { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Priority fee at the requested percentiles for each block in the returned range.
    /// </summary>
    public BigInteger[][] Reward { get; set; } = Array.Empty<BigInteger[]>();

    /// <summary>
    /// Creates a FeeHistory instance from a FeeHistoryDto.
    /// </summary>
    /// <param name="dto">The DTO to convert from.</param>
    /// <returns>A new FeeHistory instance.</returns>
    internal static FeeHistory FromDto(FeeHistoryDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var result = new FeeHistory
        {
            OldestBlock = Hex.Parse(dto.OldestBlock).ToBigInteger(),
            GasUsedRatio = dto.GasUsedRatio ?? Array.Empty<double>()
        };

        // Convert BaseFeePerGas from hex strings to BigIntegers
        if (dto.BaseFeePerGas != null)
        {
            result.BaseFeePerGas = new BigInteger[dto.BaseFeePerGas.Length];
            for (int i = 0; i < dto.BaseFeePerGas.Length; i++)
            {
                result.BaseFeePerGas[i] = Hex.Parse(dto.BaseFeePerGas[i]).ToBigInteger();
            }
        }

        // Convert Reward from hex strings to BigIntegers
        if (dto.Reward != null)
        {
            result.Reward = new BigInteger[dto.Reward.Length][];
            for (int i = 0; i < dto.Reward.Length; i++)
            {
                if (dto.Reward[i] != null)
                {
                    result.Reward[i] = new BigInteger[dto.Reward[i].Length];
                    for (int j = 0; j < dto.Reward[i].Length; j++)
                    {
                        result.Reward[i][j] = Hex.Parse(dto.Reward[i][j]).ToBigInteger();
                    }
                }
                else
                {
                    result.Reward[i] = Array.Empty<BigInteger>();
                }
            }
        }

        return result;
    }
}
