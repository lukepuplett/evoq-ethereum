namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents block data returned by eth_getBlockByNumber RPC method.
/// </summary>
public class BlockDataDto<T>
{
    public string Number { get; set; }
    public string Hash { get; set; }
    public string ParentHash { get; set; }
    public string Nonce { get; set; }
    public string Sha3Uncles { get; set; }
    public string LogsBloom { get; set; }
    public string TransactionsRoot { get; set; }
    public string StateRoot { get; set; }
    public string ReceiptsRoot { get; set; }
    public string Miner { get; set; }
    public string Difficulty { get; set; }
    public string TotalDifficulty { get; set; }
    public string ExtraData { get; set; }
    public string Size { get; set; }
    public string GasLimit { get; set; }
    public string GasUsed { get; set; }
    public string Timestamp { get; set; }
    public string[] Uncles { get; set; }
    public T[] Transactions { get; set; }

    // EIP-1559 fields (post-London fork)
    public string BaseFeePerGas { get; set; }

    // EIP-4895 fields (post-Shanghai fork)
    public WithdrawalDto[] Withdrawals { get; set; }
    public string WithdrawalsRoot { get; set; }

    // Beacon chain fields (post-Merge)
    public string ParentBeaconBlockRoot { get; set; }
}

/// <summary>
/// Represents a withdrawal in a post-Shanghai block.
/// </summary>
public class WithdrawalDto
{
    public string Index { get; set; }
    public string ValidatorIndex { get; set; }
    public string Address { get; set; }
    public string Amount { get; set; }
}