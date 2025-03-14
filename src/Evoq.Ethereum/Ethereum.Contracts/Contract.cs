using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.Chains;

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
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>The result of the method call decoded into an object.</returns>
    public async Task<T> CallAsync<T>(
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments)
    where T : new()
    {
        return await this.contractClient.CallAsync<T>(this, methodName, senderAddress, arguments);
    }

    /// <summary>
    /// Calls a method on a contract, off-chain, without creating a transaction.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>The result of the method call decoded into a dictionary.</returns>
    public async Task<IDictionary<string, object?>> CallAsync(
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments)
    {
        return await this.contractClient.CallAsync(this, methodName, senderAddress, arguments);
    }

    /// <summary>
    /// Estimates the gas required to invoke a method on a contract.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="value">The value to send with the transaction.</param>
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>The estimated gas required to invoke the method.</returns>
    public async Task<BigInteger> EstimateGasAsync(
        string methodName,
        EthereumAddress senderAddress,
        BigInteger? value,
        IDictionary<string, object?> arguments)
    {
        var hex = await this.contractClient.EstimateGasAsync(this, methodName, senderAddress, value, arguments);

        return hex.ToBigInteger();
    }

    /// <summary>
    /// Provides a complete transaction fee estimate for EIP-1559 transactions.
    /// </summary>
    /// <param name="chain">The chain.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="value">The amount of ETH to send (in wei).</param>
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>A complete fee estimate including gas limit and EIP-1559 fee parameters.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the network doesn't support EIP-1559 (pre-London fork).</exception>
    public async Task<TransactionFeeEstimate> EstimateTransactionFeeAsync(
        Chain chain,
        string methodName,
        EthereumAddress senderAddress,
        BigInteger? value,
        IDictionary<string, object?> arguments)
    {
        var gasLimit = await this.EstimateGasAsync(methodName, senderAddress, value, arguments);
        var (maxFeePerGas, maxPriorityFeePerGas) = await chain.SuggestEip1559FeesAsync();

        // This will throw if the network doesn't support EIP-1559
        var baseFee = await chain.GetBaseFeeAsync();

        return new TransactionFeeEstimate
        {
            GasLimit = gasLimit,
            MaxFeePerGas = maxFeePerGas,
            MaxPriorityFeePerGas = maxPriorityFeePerGas,
            BaseFeePerGas = baseFee,
            // The actual fee consists of:
            // 1. The base fee (burned) multiplied by the gas used
            // 2. The priority fee (tip to validators) multiplied by the gas used
            // Note: We use gasLimit as a worst-case estimate of gas used
            EstimatedFeeInWei = baseFee * gasLimit + (maxPriorityFeePerGas * gasLimit),
            // Legacy gas price equivalent (for informational purposes only)
            GasPrice = baseFee + maxPriorityFeePerGas
        };
    }

    /// <summary>
    /// Invokes a method on a contract, creating a transaction.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <returns>The result of the method call decoded into an object.</returns>
    public async Task<Hex> InvokeMethodAsync(
        string methodName,
        EthereumAddress senderAddress,
        ContractInvocationOptions options,
        IDictionary<string, object?> arguments)
    {
        return await this.contractClient.InvokeMethodAsync(this, methodName, senderAddress, options, arguments);
    }
}
