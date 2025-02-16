using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.EIP165;
using Microsoft.Extensions.Logging;
using Nethereum.ABI;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

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
    /// <param name="soliditySignature">The solidity signature of the function to call like "transfer(address,uint256)".</param>
    /// <param name="simpleParams">The parameters to call the function with, made up of simple types.</param>
    /// <returns>The result of the function call.</returns>
    public async Task<string> SimpleCall(EthereumAddress contractAddress, string soliditySignature, params object[] simpleParams)
    {
        this.logger.LogDebug(
            "Calling {FunctionSignature} on {ContractAddress} with {Parameters} parameters",
            soliditySignature,
            contractAddress,
            simpleParams.Length);

        foreach (var parameter in simpleParams)
        {
            if (parameter is byte[] bytes)
            {
                this.logger.LogDebug("Parameter: ({Type})[{Length}] {Parameter}", parameter.GetType(), bytes.Length, bytes.ToHexStruct());
            }
            else
            {
                this.logger.LogDebug("Parameter: ({Type}) {Parameter}", parameter.GetType(), parameter);
            }
        }
        var values = simpleParams.Select(p => GetSimpleABIValue(p)).ToArray();

        var abiEncoder = new ABIEncode();
        byte[] encodedParams = abiEncoder.GetABIEncoded(values);

        var sig = Help165.ComputeInterfaceID(soliditySignature);
        if (sig.Length != 4)
        {
            throw new Exception("Signature must be 4 bytes long");
        }
        var callData = sig.ToByteArray().Concat(encodedParams);
        var callInput = new CallInput(callData.ToHexStruct().ToString(), contractAddress.ToString());

        var web3 = new Web3(this.Endpoint.URL);
        var contractCall = new ContractCall(web3.Eth.Transactions.Call, null);

        //

        this.logger.LogInformation("Calling signature: {Signature}", sig);

        var result = await contractCall.CallAsync(callInput);

        this.logger.LogDebug("Result: {Result}...", result.Substring(0, 32));

        return result;
    }

    //

    private static ABIValue GetSimpleABIValue(object obj)
    {
        if (obj is EthereumAddress address)
        {
            return new ABIValue(ABIType.CreateABIType("address"), address);
        }

        if (obj is Hex hex)
        {
            return GetSimpleABIValue(hex.ToByteArray());
        }

        if (obj is byte[] bytes)
        {
            if (bytes.Length == 32)
            {
                return new ABIValue(ABIType.CreateABIType("bytes32"), bytes);
            }

            return new ABIValue(ABIType.CreateABIType("bytes"), bytes);
        }

        if (obj is string str)
        {
            return new ABIValue(ABIType.CreateABIType("string"), str);
        }

        if (obj is bool b)
        {
            return new ABIValue(ABIType.CreateABIType("bool"), b);
        }

        if (obj is UInt32 i)
        {
            return new ABIValue(ABIType.CreateABIType("uint32"), i);
        }

        if (obj is UInt64 l)
        {
            return new ABIValue(ABIType.CreateABIType("uint64"), l);
        }

        if (obj is Int32 h)
        {
            return new ABIValue(ABIType.CreateABIType("uint32"), h);
        }

        if (obj is Int64 j)
        {
            return new ABIValue(ABIType.CreateABIType("uint64"), j);
        }

        if (obj is BigInteger big)
        {
            return new ABIValue(ABIType.CreateABIType("uint256"), big);
        }

        throw new ArgumentException($"Unsupported type: {obj.GetType()}");
    }
}