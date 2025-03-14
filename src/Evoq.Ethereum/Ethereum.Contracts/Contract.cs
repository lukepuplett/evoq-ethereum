using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;

namespace Evoq.Ethereum.Contracts;

/// <summary>
/// A class that represents a contract.
/// </summary>
public class Contract
{
    private readonly ContractAbi abi;
    private readonly ContractClient contractClient;

    //

    /// <summary>
    /// Initializes a new instance of the Contract class.   
    /// </summary>
    /// <param name="contractClient">The contract client.</param>
    /// <param name="abiDocument">The stream containing the ABI.</param>
    /// <param name="address">The address of the contract.</param>
    public Contract(ContractClient contractClient, Stream abiDocument, EthereumAddress address)
    {
        this.abi = ContractAbiReader.Read(abiDocument);
        this.contractClient = contractClient;
        this.Address = address;
    }

    /// <summary>
    /// Gets or sets the address of the contract.
    /// </summary>  
    public EthereumAddress Address { get; set; }

    //

    /// <summary>
    /// Gets the function signature for a method.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>The function signature.</returns>
    /// <exception cref="Exception">Thrown when the method is not found in the ABI.</exception>
    public FunctionSignature GetFunctionSignature(string methodName)
    {
        if (this.abi.TryGetFunction(methodName, out var function))
        {
            return function.GetFunctionSignature();
        }
        else
        {
            var functions = this.abi.GetFunctions().Select(f => f.Name);

            throw new Exception($"Function {methodName} not found in ABI. Available functions: {string.Join(", ", functions)}");
        }
    }

    /// <summary>
    /// Calls a method on a contract, off-chain, without creating a transaction.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="parameters">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>The result of the method call decoded into an object.</returns>
    public async Task<T> CallAsync<T>(
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> parameters)
    where T : new()
    {
        return await this.contractClient.CallAsync<T>(this, methodName, senderAddress, parameters);
    }

    /// <summary>
    /// Calls a method on a contract, off-chain, without creating a transaction.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="parameters">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>The result of the method call decoded into a dictionary.</returns>
    public async Task<IDictionary<string, object?>> CallAsync(
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> parameters)
    {
        return await this.contractClient.CallAsync(this, methodName, senderAddress, parameters);
    }

    /// <summary>
    /// Estimates the gas required to invoke a method on a contract.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="value">The value to send with the transaction.</param>
    /// <param name="parameters">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>The estimated gas required to invoke the method.</returns>
    public async Task<BigInteger> EstimateGasAsync(
        string methodName,
        EthereumAddress senderAddress,
        BigInteger? value,
        IDictionary<string, object?> parameters)
    {
        var hex = await this.contractClient.EstimateGasAsync(this, methodName, senderAddress, value, parameters);

        HexSignedness s = HexSignedness.Unsigned;
        HexEndianness e = HexEndianness.BigEndian;

        return hex.ToBigInteger(s, e);
    }

    /// <summary>
    /// Invokes a method on a contract, creating a transaction.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="parameters">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>The result of the method call decoded into an object.</returns>
    public async Task<Hex> InvokeAsync(
        string methodName,
        EthereumAddress senderAddress,
        ContractInvocationOptions options,
        IDictionary<string, object?> parameters)
    {
        return await this.contractClient.InvokeAsync(this, methodName, senderAddress, options, parameters);
    }
}
