using System.Threading.Tasks;
using Evoq.Blockchain;

namespace Evoq.Ethereum.EIP165;

/// <summary>
/// Represents an entity that supports the EIP-165 standard
/// </summary>
public interface IEIP165
{
    /// <summary>
    /// Checks if the entity supports a given interface
    /// </summary>
    /// <param name="contractAddress">The address of the contract to check</param>
    /// <param name="interfaceId">The interface ID</param>
    /// <returns>True if the entity supports the interface, false otherwise</returns>
    Task<bool> SupportsInterface(EthereumAddress contractAddress, Hex interfaceId);
}