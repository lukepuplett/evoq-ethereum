namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a transaction data returned by eth_getBlockByNumber RPC method.
/// </summary>
public class TransactionDataDto
{
    public string BlockHash { get; set; }
    public string BlockNumber { get; set; }
    public string From { get; set; }
    public string Gas { get; set; }
    public string GasPrice { get; set; }
    public string Hash { get; set; }
    public string Input { get; set; }
    public string Nonce { get; set; }
    public string To { get; set; }
    public string TransactionIndex { get; set; }
    public string Value { get; set; }
    public string V { get; set; }
    public string R { get; set; }
    public string S { get; set; }

    // EIP-1559 fields (Type 2 transactions)
    public string Type { get; set; }
    public string MaxFeePerGas { get; set; }
    public string MaxPriorityFeePerGas { get; set; }
    public string ChainId { get; set; }

    // EIP-2930 fields (Type 1 transactions)
    public AccessListDto AccessList { get; set; }
}
