namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents an access list for EIP-2930 transactions.
/// </summary>
public class AccessListDto
{
    public AccessListEntryDto[] AccessList { get; set; }
}

/// <summary>
/// Represents an entry in an access list.
/// </summary>
public class AccessListEntryDto
{
    public string Address { get; set; }
    public string[] StorageKeys { get; set; }
}