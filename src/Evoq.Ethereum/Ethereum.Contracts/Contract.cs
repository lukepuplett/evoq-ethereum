using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.ABI.Conversion;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Contracts;

/// <summary>
/// A class that represents a contract at a specific address on a chain.
/// </summary>
public class Contract
{
    private readonly ContractAbi abi;
    private readonly ContractClient contractClient;
    private readonly ILoggerFactory? loggerFactory;

    //

    /// <summary>
    /// Initializes a new instance of the Contract class.   
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="chainClient">The chain client.</param>
    /// <param name="contractClient">The contract client.</param>
    /// <param name="abiDocument">The stream containing the ABI.</param>
    /// <param name="address">The address of the contract.</param>
    /// <param name="loggerFactory">The logger factory to use.</param>
    internal Contract(
        ulong chainId,
        ChainClient chainClient,
        ContractClient contractClient,
        Stream abiDocument,
        EthereumAddress address,
        ILoggerFactory? loggerFactory = null)
    {
        this.abi = AbiJsonReader.Read(abiDocument);
        this.contractClient = contractClient;
        this.loggerFactory = loggerFactory;

        this.Address = address;
        this.Chain = new Chain(chainId, chainClient);
    }

    /// <summary>
    /// Gets or sets the address of the contract.
    /// </summary>  
    public EthereumAddress Address { get; }

    /// <summary>
    /// Gets the chain.
    /// </summary>
    public Chain Chain { get; }

    //

    /// <summary>
    /// Gets the function signature for a method.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>The function signature.</returns>
    /// <exception cref="Exception">Thrown when the method is not found in the ABI.</exception>
    internal AbiSignature GetFunctionSignature(string methodName)
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
    /// Gets the event signature for an event.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <returns>The event signature.</returns>
    /// <exception cref="Exception">Thrown when the event is not found in the ABI.</exception>
    internal AbiSignature GetEventSignature(string eventName)
    {
        if (this.abi.TryGetEvent(eventName, out var @event))
        {
            return @event.GetEventSignature();
        }
        else
        {
            var events = this.abi.GetEvents().Select(e => e.Name);

            throw new Exception($"Event {eventName} not found in ABI. Available events: {string.Join(", ", events)}");
        }
    }

    //

    /// <summary>
    /// Calls a method on a contract, off-chain, without creating a transaction.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the method call decoded into an object.</returns>
    public async Task<T> CallAsync<T>(
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    where T : new()
    {
        return await this.contractClient.CallAsync<T>(
            this, methodName, senderAddress, arguments, cancellationToken);
    }

    /// <summary>
    /// Calls a method on a contract, off-chain, without creating a transaction.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the method call decoded into a dictionary.</returns>
    public async Task<Dictionary<string, object?>> CallAsync(
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        return await this.contractClient.CallAsync(
            this, methodName, senderAddress, arguments, cancellationToken);
    }

    /// <summary>
    /// Estimates the gas required to invoke a method on a contract.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="value">The value to send with the transaction.</param>
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The estimated gas required to invoke the method.</returns>
    public async Task<BigInteger> EstimateGasAsync(
        string methodName,
        EthereumAddress senderAddress,
        BigInteger? value,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var hex = await this.contractClient.EstimateGasAsync(
            this, methodName, senderAddress, value, arguments, cancellationToken);

        return hex.ToBigInteger();
    }

    /// <summary>
    /// Estimates the transaction fee for calling a contract method.
    /// </summary>
    /// <param name="methodName">The name of the contract method to call.</param>
    /// <param name="senderAddress">The address that will send the transaction.</param>
    /// <param name="value">The amount of Ether to send with the transaction (in wei).</param>
    /// <param name="arguments">The arguments to pass to the method.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A detailed estimate of the transaction fees.</returns>
    public async Task<ITransactionFeeEstimate> EstimateTransactionFeeAsync(
        string methodName,
        EthereumAddress senderAddress,
        BigInteger? value,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Estimate the gas limit - the maximum amount of computational work
        // the transaction is allowed to use
        var gasLimit = await this.EstimateGasAsync(
            methodName, senderAddress, value, arguments, cancellationToken);

        // Step 2: Get the current fee market conditions for EIP-1559 transactions
        // This includes suggested values for maxFeePerGas and maxPriorityFeePerGas
        var suggestion = await this.Chain.SuggestEip1559FeesAsync();

        // Step 3: Get the current base fee from the network
        // This will throw if the network doesn't support EIP-1559 (pre-London fork)
        var baseFeeInWei = await this.Chain.GetBaseFeeAsync();

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
            EstimatedGasLimit = gasLimit,

            // The maximum fee per gas unit the user is willing to pay (base fee + priority fee)
            // This is the absolute maximum that could be charged per gas unit
            SuggestedMaxFeePerGas = suggestion.MaxFeePerGasInWei.ToWeiAmount(),

            // The priority fee (tip) per gas unit offered to validators
            // This is what incentivizes miners to include the transaction
            SuggestedMaxPriorityFeePerGas = suggestion.MaxPriorityFeePerGasInWei.ToWeiAmount(),

            // The network's current base fee per gas unit
            // This is determined by network congestion and is burned when paid
            CurrentBaseFeePerGas = baseFeeInWei.ToWeiAmount(),

            // The estimated total transaction fee (worst case if all gas is used)
            // Formula: (baseFee + priorityFee) * gasLimit
            EstimatedTotalFee = totalFeeInWei.ToWeiAmount(),

            // The equivalent legacy gas price (for compatibility with pre-EIP-1559 tools)
            // Formula: baseFee + priorityFee
            LegacyGasPrice = legacyGasPrice.ToWeiAmount()
        };
    }

    /// <summary>
    /// Invokes a method on a contract, creating a transaction.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="nonce">The nonce to use for the transaction.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="arguments">The parameters to pass to the method; tuples can be passed as .NET tuples.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the method call decoded into an object.</returns>
    public async Task<Hex> InvokeMethodAsync(
        string methodName,
        ulong nonce,
        ContractInvocationOptions options,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        return await this.contractClient.InvokeMethodAsync(
            this, methodName, nonce, options, arguments, cancellationToken);
    }

    /// <summary>
    /// Tries to read a logged event from a transaction receipt.
    /// </summary>
    /// <param name="receipt">The transaction receipt.</param>
    /// <param name="eventName">The name of the event to read.</param>
    /// <param name="indexed">The indexed parameters of the event.</param>
    /// <param name="data">The data parameters of the event.</param>
    public bool TryReadEventLogsFromReceipt(
        TransactionReceipt receipt,
        string eventName,
        out IReadOnlyDictionary<string, object?>? indexed,
        out IReadOnlyDictionary<string, object?>? data)
    {
        var eventSignature = this.GetEventSignature(eventName);

        return this.contractClient.TryRead(receipt, eventSignature, out indexed, out data);
    }

    /// <summary>
    /// Tries to read a logged event from a transaction receipt.
    /// </summary>
    /// <typeparam name="TIndexed">The type of the indexed parameters.</typeparam>
    /// <typeparam name="TData">The type of the data parameters.</typeparam>
    /// <param name="receipt">The transaction receipt.</param>
    /// <param name="eventName">The name of the event to read.</param>
    /// <param name="indexed">The indexed parameters of the event.</param>
    /// <param name="data">The data parameters of the event.</param>
    /// <returns>True if the event was read successfully, false otherwise.</returns>
    public bool TryReadEventLogsFromReceipt<TIndexed, TData>(
        TransactionReceipt receipt,
        string eventName,
        out TIndexed? indexed,
        out TData? data)
    {
        var eventSignature = this.GetEventSignature(eventName);

        if (this.contractClient.TryRead(receipt, eventSignature, out var indexedDictionary, out var dataDictionary))
        {
            var converter = new AbiConverter(this.loggerFactory);

            indexed = converter.DictionaryToObject<TIndexed>(indexedDictionary!);
            data = converter.DictionaryToObject<TData>(dataDictionary!);

            return true;
        }

        indexed = default;
        data = default;

        return false;
    }

}
