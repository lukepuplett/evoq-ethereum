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
    /// Calls a function on a contract without using the ABI, using a signature with unnamed parameters.
    /// </summary>
    /// <param name="contractAddress">The address of the contract to call.</param>
    /// <param name="functionSignature">The ABI signature of the function to call like "transfer(address,uint256)".</param>
    /// <param name="simpleParams">The parameters to call the function with, made up of simple types.</param>
    /// <returns>The result of the function call.</returns>
    public async Task<Hex> CallAsync(
        EthereumAddress contractAddress,
        string functionSignature,
        params object?[] simpleParams)
    {
        return await this.CallAsync(contractAddress, EthereumAddress.Zero, functionSignature, BigInteger.Zero, simpleParams);
    }

    /// <summary>
    /// Calls a function on a contract without using the ABI, using a signature with named parameters.
    /// </summary>
    /// <param name="contractAddress">The address of the contract to call.</param>
    /// <param name="functionSignature">The ABI signature of the function to call like "transfer(address to,uint256 value)".</param>
    /// <param name="simpleParams">The parameters to call the function with, made up of simple types.</param>
    /// <returns>The result of the function call.</returns>
    public async Task<Hex> CallAsync(
        EthereumAddress contractAddress,
        string functionSignature,
        params (string name, object? value)[] simpleParams)
    {
        return await this.CallAsync(contractAddress, EthereumAddress.Zero, functionSignature, BigInteger.Zero, simpleParams);
    }

    /// <summary>
    /// Calls a function on a contract without using the ABI, using a signature with unnamed parameters.
    /// </summary>
    /// <param name="contractAddress">The address of the contract to call.</param>
    /// <param name="from">The address of the account to use to call the function.</param>
    /// <param name="functionSignature">The ABI signature of the function to call like "transfer(address,uint256)".</param>
    /// <param name="value">The value to send with the call.</param>
    /// <param name="simpleParams">The parameters to call the function with, made up of simple types.</param>
    /// <returns>The result of the function call.</returns>
    public async Task<Hex> CallAsync(
        EthereumAddress contractAddress,
        EthereumAddress from,
        string functionSignature,
        BigInteger value,
        params object?[] simpleParams)
    {
        var indexNamedParams = simpleParams.Select((p, i) => (i.ToString(), p)).ToArray();

        return await this.CallAsync(contractAddress, from, functionSignature, value, indexNamedParams);
    }

    /// <summary>
    /// Calls a function on a contract without using the ABI, using a signature with named parameters.
    /// </summary>
    /// <param name="contractAddress">The address of the contract to call.</param>
    /// <param name="from">The address of the account to use to call the function.</param>
    /// <param name="functionSignature">The ABI signature of the function to call like "transfer(address to,uint256 value)".</param>
    /// <param name="value">The value to send with the call.</param>
    /// <param name="simpleParams">The parameters to call the function with, made up of simple types.</param>
    /// <returns>The result of the function call.</returns>
    public async Task<Hex> CallAsync(
        EthereumAddress contractAddress,
        EthereumAddress from,
        string functionSignature,
        BigInteger value,
        params (string name, object? value)[] simpleParams)
    {
        this.logger.LogDebug(
            "Calling {FunctionSignature} on {ContractAddress} with {Parameters} parameters",
            functionSignature,
            contractAddress,
            simpleParams.Length);

        AbiSignature sig;
        try
        {
            sig = AbiSignature.Parse(AbiItemType.Function, functionSignature);
        }
        catch (ArgumentException badSig)
        {
            throw new ArgumentException(
                "Unable to call function. Could not parse the function signature. The format should be like myFunc(type1,type2).",
                badSig);
        }

        var encoder = new AbiEncoder(this.Endpoint.LoggerFactory);
        var encodedBytes = sig.AbiEncodeCallValues(encoder, simpleParams.ToDictionary(p => p.name, p => p.value));

        var jsonRpcClient = new JsonRpcClient(new Uri(this.Endpoint.URL), this.Endpoint.LoggerFactory);

        var dto = new EthCallParamObjectDto
        {
            From = from.IsEmpty ? null : from.ToString(),
            To = contractAddress.ToString(),
            Input = new Hex(encodedBytes).ToString(),
            Value = value.ToHexString(trimLeadingZeroDigits: true),
        };

        this.logger.LogInformation("Calling signature: {Signature}", sig);

        var result = await jsonRpcClient.CallAsync(dto);

        this.logger.LogDebug("Result: {Result}...", result.ToString().Substring(0, 66));

        return result;
    }

    /// <summary>
    /// Decodes a set of parameters from a hex string.
    /// </summary>
    /// <param name="returnSignature">The return signature of the function to decode.</param>
    /// <param name="result">The result to decode.</param>
    public AbiDecodingResult DecodeParameters(string returnSignature, Hex result)
    {
        var decoder = new AbiDecoder(this.Endpoint.LoggerFactory);
        var parameters = AbiParameters.Parse(returnSignature);

        return decoder.DecodeParameters(parameters, result);
    }

}