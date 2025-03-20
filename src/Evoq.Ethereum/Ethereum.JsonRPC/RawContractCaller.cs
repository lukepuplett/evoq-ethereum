using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// A contract caller that can be used to call functions on a contract without using the ABI.
/// </summary>
public class RawContractCaller
{
    private readonly ILogger logger;

    //

    /// <summary>
    /// A contract caller that can be used to call functions on a contract without using the ABI.
    /// </summary>
    /// <param name="endpoint">The endpoint to use to call the contract.</param>
    public RawContractCaller(Endpoint endpoint)
    {
        this.Endpoint = endpoint;
        this.logger = endpoint.LoggerFactory.CreateLogger(typeof(RawContractCaller));
    }

    //

    /// <summary>
    /// The endpoint to use to call the contract.
    /// </summary>
    public Endpoint Endpoint { get; }

    //

    /// <summary>
    /// Calls a function on a contract without using the ABI.
    /// </summary>
    /// <param name="contractAddress">The address of the contract to call.</param>
    /// <param name="from">The address of the account to use to call the function.</param>
    /// <param name="abiSignature">The ABI signature of the function to call like "transfer(address,uint256)".</param>
    /// <param name="value">The value to send with the call.</param>
    /// <param name="simpleParams">The parameters to call the function with, made up of simple types.</param>
    /// <returns>The result of the function call.</returns>
    public async Task<Hex> SimpleCall(
        EthereumAddress contractAddress, EthereumAddress from, string abiSignature, BigInteger value, params (string name, object? value)[] simpleParams)
    {
        this.logger.LogDebug(
            "Calling {FunctionSignature} on {ContractAddress} with {Parameters} parameters",
            abiSignature,
            contractAddress,
            simpleParams.Length);

        var encoder = new AbiEncoder();
        var sig = AbiSignature.Parse(AbiItemType.Function, abiSignature);
        var encodedBytes = sig.AbiEncodeCallValues(encoder, simpleParams.ToDictionary(p => p.name, p => p.value));

        var jsonRpcClient = new JsonRpcClient(new Uri(this.Endpoint.URL), this.Endpoint.LoggerFactory);

        var dto = new EthCallParamObjectDto
        {
            From = from.ToString(),
            To = contractAddress.ToString(),
            Input = encodedBytes.ToHex(),
            Value = value.ToHexString(trimLeadingZeroDigits: true),
        };

        this.logger.LogInformation("Calling signature: {Signature}", sig);

        var result = await jsonRpcClient.CallAsync(dto);

        this.logger.LogDebug("Result: {Result}...", result.ToString().Substring(0, 66));

        return result;
    }

    //

}