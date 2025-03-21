using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.EIP165;

/// <summary>
/// An implementation of the EIP-165 standard using our own native Ethereum client.
/// </summary>
public class EIP165Native : IEIP165
{
    private const string ERC165_ID = "0x01ffc9a7";
    private const string INVALID_ID = "0xffffffff";
    private const string SUPPORTS_INTERFACE_ABI_SIGNATURE = "supportsInterface(bytes4)";
    private readonly Dictionary<string, bool> supportsInterfaceCache = new();

    private readonly ILogger logger;
    private readonly RawContractCaller caller;

    //

    /// <summary>
    /// Constructs a new EIP165Native instance.
    /// </summary>
    /// <param name="endpoint">The endpoint to use for the EIP-165 contract.</param>
    public EIP165Native(Endpoint endpoint)
    {
        this.logger = endpoint.LoggerFactory.CreateLogger<EIP165Native>();
        this.caller = new RawContractCaller(this.Endpoint);

        this.Endpoint = endpoint;
    }

    //

    /// <summary>
    /// The endpoint to use for the EIP-165 contract.
    /// </summary>
    public Endpoint Endpoint { get; }

    //

    /// <summary>
    /// Checks if the contract supports a given interface.
    /// </summary>
    /// <param name="contractAddress">The address of the contract to check.</param>
    /// <param name="interfaceId">The interface ID to check.</param>
    /// <returns>True if the contract supports the interface, false otherwise.</returns>
    public async Task<bool> SupportsInterface(EthereumAddress contractAddress, Hex interfaceId)
    {
        string key = $"{contractAddress.ToString()}:{interfaceId.ToString()}";
        if (this.supportsInterfaceCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // Step 1: Check if contract supports ERC-165 itself by checking
        // that it returns true for the ERC-165 interface ID.

        var supportsERC165 = await this.SupportsERC165(contractAddress, ERC165_ID);
        if (!supportsERC165) // not!
        {
            this.supportsInterfaceCache[key] = false;
            return false;
        }

        // Step 2: Validate that supportsInterface is implemented correctly by
        // checking that it returns false for an invalid interface ID.

        var supportsNonsense = await this.SupportsERC165(contractAddress, INVALID_ID);
        if (supportsNonsense)
        {
            this.logger.LogInformation("Contract supports nonsense so it doesn't support ERC-165");

            this.supportsInterfaceCache[key] = false;
            return false;
        }

        // Step 3: Check the actual interface support by calling the function
        // with the interface ID.

        var supportsInterface = await this.SupportsERC165(contractAddress, interfaceId);
        this.supportsInterfaceCache[key] = supportsInterface;

        this.logger.LogInformation("Contract supports interface {InterfaceId}: {SupportsInterface}", interfaceId, supportsInterface);

        return supportsInterface;
    }

    //

    private async Task<bool> SupportsERC165(EthereumAddress contractAddress, Hex interfaceId)
    {
        try
        {
            this.logger.LogDebug("Checking if contract supports {InterfaceId}", interfaceId);

            var hex = await this.caller.CallAsync(contractAddress, SUPPORTS_INTERFACE_ABI_SIGNATURE, ("interfaceId", interfaceId));

            return !hex.IsZeroValue();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error checking if contract supports {InterfaceId}", interfaceId);

            return false;
        }
    }
}