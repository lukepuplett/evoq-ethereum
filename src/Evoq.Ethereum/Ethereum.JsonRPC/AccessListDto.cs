using System;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents an access list for EIP-2930 transactions.
/// Access lists specify the storage slots and addresses a transaction will access,
/// allowing for gas cost reductions on supported networks.
/// </summary>
public class AccessListDto
{
    /// <summary>
    /// Array of access list entries, each specifying an address and its storage slots.
    /// </summary>
    public AccessListEntryDto[] AccessList { get; set; } = Array.Empty<AccessListEntryDto>();
}

/// <summary>
/// Represents an entry in an access list.
/// Each entry specifies a contract address and the storage slots that will be accessed.
/// </summary>
public class AccessListEntryDto
{
    /// <summary>
    /// The 20-byte address of the contract whose storage is being accessed.
    /// Hex-encoded with '0x' prefix.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Array of 32-byte storage slot keys that will be accessed in the contract.
    /// Each key is hex-encoded with '0x' prefix.
    /// </summary>
    public string[] StorageKeys { get; set; } = Array.Empty<string>();
}