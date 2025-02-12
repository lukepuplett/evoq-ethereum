using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;

namespace Evoq.Ethereum.EIP165;

/// <summary>
/// An implementation of the EIP-165 standard using Nethereum.
/// </summary>
public class EIP165Nethereum : IEIP165
{
    private const string ERC165_ID = "0x01ffc9a7";
    private const string INVALID_ID = "0xffffffff";

    private readonly Endpoint endpoint;
    private readonly ILogger logger;
    private readonly Dictionary<Hex, bool> supportsInterfaceCache = new();

    //

    /// <summary>
    /// Constructs a new EIP165Nethereum instance.
    /// </summary>
    /// <param name="endpoint">The endpoint to use for the EIP-165 contract.</param>
    /// <param name="contractAddress">The address of the EIP-165 contract.</param>
    public EIP165Nethereum(Endpoint endpoint, EthereumAddress contractAddress)
    {
        this.endpoint = endpoint;
        this.logger = endpoint.LoggerFactory.CreateLogger<EIP165Nethereum>();

        this.ContractAddress = contractAddress;
    }

    //

    public EthereumAddress ContractAddress { get; }

    //

    /// <summary>
    /// Checks if the contract supports a given interface.
    /// </summary>
    /// <param name="interfaceId">The interface ID to check.</param>
    /// <returns>True if the contract supports the interface, false otherwise.</returns>
    public async Task<bool> SupportsInterface(Hex interfaceId)
    {
        if (this.supportsInterfaceCache.TryGetValue(interfaceId, out var cached))
        {
            return cached;
        }

        var web3 = new Web3(this.endpoint.URL);
        var contract = web3.Eth.GetContract(Help165.ABI, this.ContractAddress.ToString());
        var function = contract.GetFunction("supportsInterface");

        try
        {
            // Step 1: Check if contract supports ERC-165 itself by checking
            // that it returns true for the ERC-165 interface ID.

            var supportsERC165 = await function.CallAsync<bool>(ERC165_ID.HexToByteArray());
            if (!supportsERC165)
            {
                this.supportsInterfaceCache[interfaceId] = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            // WATCHOUT / Expecting this to throw perhaps on revert if the contract
            // does not support ERC-165 itself, has no supportsInterface function.

            this.logger.LogError(ex, "Error checking if contract supports ERC-165");

            this.supportsInterfaceCache[interfaceId] = false;
            return false;
        }

        try
        {
            // Step 2: Validate that supportsInterface is implemented correctly by
            // checking that it returns false for an invalid interface ID.

            var returnsInvalidTrue = await function.CallAsync<bool>(INVALID_ID.HexToByteArray());
            if (returnsInvalidTrue)
            {
                this.supportsInterfaceCache[interfaceId] = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error checking if contract supportsInterface is implemented correctly");

            this.supportsInterfaceCache[interfaceId] = false;
            return false;
        }

        try
        {
            // Step 3: Check the actual interface support by calling the function
            // with the interface ID.

            var supported = await function.CallAsync<bool>(interfaceId.ToByteArray());

            this.supportsInterfaceCache[interfaceId] = supported;
            return supported;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error checking if contract supports interface");

            this.supportsInterfaceCache[interfaceId] = false;
            return false;
        }
    }
}
