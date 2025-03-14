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
    /// Estimates the transaction fee for calling a contract method.
    /// </summary>
    /// <param name="chain">The blockchain to interact with.</param>
    /// <param name="methodName">The name of the contract method to call.</param>
    /// <param name="senderAddress">The address that will send the transaction.</param>
    /// <param name="value">The amount of Ether to send with the transaction (in wei).</param>
    /// <param name="arguments">The arguments to pass to the method.</param>
    /// <returns>A detailed estimate of the transaction fees.</returns>
    public async Task<TransactionFeeEstimate> EstimateTransactionFeeAsync(
        Chain chain,
        string methodName,
        EthereumAddress senderAddress,
        BigInteger? value,
        IDictionary<string, object?> arguments)
    {
        // Step 1: Estimate the gas limit - the maximum amount of computational work
        // the transaction is allowed to use
        var gasLimit = await this.EstimateGasAsync(methodName, senderAddress, value, arguments);

        // Step 2: Get the current fee market conditions for EIP-1559 transactions
        // This includes suggested values for maxFeePerGas and maxPriorityFeePerGas
        var suggestion = await chain.SuggestEip1559FeesAsync();

        // Step 3: Get the current base fee from the network
        // This will throw if the network doesn't support EIP-1559 (pre-London fork)
        var baseFeeInWei = await chain.GetBaseFeeAsync();

        // Step 4: Calculate the estimated total transaction fee
        // Formula: (baseFee + priorityFee) * gasLimit
        // 
        // The total fee consists of two parts:
        // 1. Base fee: This is burned (removed from circulation) - baseFeeInWei * gasLimit
        // 2. Priority fee: This goes to miners/validators as an incentive - maxPriorityFeePerGasInWei * gasLimit
        //
        // Note: We use gasLimit as a worst-case estimate. The actual gas used may be less,
        // in which case the unused gas fee is refunded.
        BigInteger baseFeeComponent = baseFeeInWei * gasLimit;
        BigInteger priorityFeeComponent = suggestion.MaxPriorityFeePerGasInWei * gasLimit;
        BigInteger totalFeeInWei = baseFeeComponent + priorityFeeComponent;

        // Step 5: Calculate the equivalent legacy gas price
        // In pre-EIP-1559 transactions, there was a single gas price that combined
        // what is now separated into base fee and priority fee
        BigInteger legacyGasPrice = baseFeeInWei + suggestion.MaxPriorityFeePerGasInWei;

        // Step 6: Return the complete fee estimate with all components
        return new TransactionFeeEstimate
        {
            // The maximum amount of gas the transaction can consume
            GasLimit = gasLimit,

            // The maximum fee per gas unit the user is willing to pay (base fee + priority fee)
            // This is the absolute maximum that could be charged per gas unit
            MaxFeePerGas = suggestion.MaxFeePerGasInWei.ToWeiAmount(),

            // The priority fee (tip) per gas unit offered to validators
            // This is what incentivizes miners to include the transaction
            MaxPriorityFeePerGas = suggestion.MaxPriorityFeePerGasInWei.ToWeiAmount(),

            // The network's current base fee per gas unit
            // This is determined by network congestion and is burned when paid
            BaseFeePerGas = baseFeeInWei.ToWeiAmount(),

            // The estimated total transaction fee (worst case if all gas is used)
            // Formula: (baseFee + priorityFee) * gasLimit
            EstimatedFee = totalFeeInWei.ToWeiAmount(),

            // The equivalent legacy gas price (for compatibility with pre-EIP-1559 tools)
            // Formula: baseFee + priorityFee
            GasPrice = legacyGasPrice.ToWeiAmount()
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
