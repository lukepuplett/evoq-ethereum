using System.Threading.Tasks;
using Evoq.Blockchain;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// The JSON-RPC interface for the `eth_getCode` method.
/// </summary>
public interface IGetCode
{
    /// <summary>
    /// Gets the code of a contract.
    /// </summary>
    /// <param name="address">The address of the contract to get the code for.</param>
    /// <returns>The code of the contract.</returns>
    Task<Hex> GetCodeAsync(EthereumAddress address);
}
