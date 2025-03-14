using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Contracts;

/// <summary>
/// A class that can be used to call methods on a contract.
/// </summary>
public class ContractClient
{
    private readonly Random rng = new Random();

    private readonly IEthereumJsonRpc jsonRpc;
    private readonly IAbiEncoder abiEncoder;
    private readonly IAbiDecoder abiDecoder;
    private readonly ITransactionSigner? transactionSigner;
    private readonly INonceStore? nonceStore;

    //

    /// <summary>
    /// Initializes a new instance of the ContractCaller class.
    /// </summary>
    /// <param name="jsonRpc">The JSON-RPC client.</param>
    /// <param name="abiEncoder">The ABI encoder.</param>
    /// <param name="abiDecoder">The ABI decoder.</param>
    /// <param name="transactionSigner">The transaction signer.</param>
    /// <param name="nonceStore">The nonce store.</param>
    public ContractClient(
        IEthereumJsonRpc jsonRpc,
        IAbiEncoder abiEncoder,
        IAbiDecoder abiDecoder,
        ITransactionSigner? transactionSigner,
        INonceStore? nonceStore)
    {
        this.jsonRpc = jsonRpc;
        this.abiEncoder = abiEncoder;
        this.abiDecoder = abiDecoder;
        this.transactionSigner = transactionSigner;
        this.nonceStore = nonceStore;
    }

    //

    internal async Task<T> CallAsync<T>(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> parameters)
    where T : new()
    {
        var (result, signature) = await this.ExecuteCallAsync(contract, methodName, senderAddress, parameters);

        var decoded = signature.AbiDecodeReturnValues(this.abiDecoder, result.ToByteArray());

        return decoded.Parameters.ToObject<T>();
    }

    internal async Task<IDictionary<string, object?>> CallAsync(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> parameters)
    {
        var (result, signature) = await this.ExecuteCallAsync(contract, methodName, senderAddress, parameters);

        var decoded = signature.AbiDecodeReturnValues(this.abiDecoder, result.ToByteArray());

        return decoded.Parameters.ToDictionary(false);
    }

    internal async Task<Hex> InvokeMethodAsync(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        ContractInvocationOptions options,
        IDictionary<string, object?> parameters)
    {
        // TODO / research access list usage for the transaction

        throw new NotImplementedException();
    }

    internal async Task<Hex> EstimateGasAsync(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        BigInteger? value,
        IDictionary<string, object?> parameters)
    {
        if (value.HasValue && value.Value < 0)
        {
            throw new ArgumentException("Value must be greater than 0", nameof(value));
        }

        var signature = contract.GetFunctionSignature(methodName);
        var encoded = signature.AbiEncodeCallValues(this.abiEncoder, parameters);

        var transactionParams = new TransactionParamsDto
        {
            From = senderAddress.ToString(),
            To = contract.Address.ToString(),
            Data = new Hex(encoded).ToString(),
            Value = value.ToHexStringForJsonRpc()
        };

        return await this.jsonRpc.EstimateGasAsync(transactionParams, id: this.GetRandomId());
    }

    //

    private async Task<(Hex Results, FunctionSignature Function)> ExecuteCallAsync(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> parameters)
    {
        var signature = contract.GetFunctionSignature(methodName);
        var encoded = signature.AbiEncodeCallValues(this.abiEncoder, parameters);

        var ethCallParams = new EthCallParamObjectDto
        {
            To = contract.Address.HasValue ? contract.Address.ToString() : throw new InvalidOperationException("Contract address is not set"),
            From = senderAddress.HasValue ? senderAddress.ToString() : throw new InvalidOperationException("Sender address is not set"),
            Input = new Hex(encoded).ToString(),
        };

        var result = await this.jsonRpc.CallAsync(ethCallParams, id: this.GetRandomId());

        return (result, signature);
    }

    //

    private int GetRandomId()
    {
        return this.rng.Next();
    }

    //

    /// <summary>
    /// Creates a new ContractCaller instance using the default secp256k1 curve, if the sender is provided.
    /// </summary>
    /// <param name="providerBaseUrl">The base URL of the JSON-RPC provider.</param>
    /// <param name="sender">The sender; if null, the ContractCaller will be read-only; attempts to send transactions will throw.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The ContractCaller instance.</returns>
    public static ContractClient CreateDefault(
        Uri providerBaseUrl,
        Sender? sender,
        ILoggerFactory loggerFactory)
    {
        var jsonRpc = new JsonRpcClient(providerBaseUrl, loggerFactory);
        var abiEncoder = new AbiEncoder();
        var abiDecoder = new AbiDecoder();

        if (sender.HasValue)
        {
            var transactionSigner = TransactionSigner.CreateDefault(sender.Value.PrivateKey.ToByteArray());

            return new ContractClient(jsonRpc, abiEncoder, abiDecoder, transactionSigner, sender.Value.NonceStore);
        }
        else
        {
            return new ContractClient(jsonRpc, abiEncoder, abiDecoder, null, null);
        }
    }
}

/// <summary>
/// Options for invoking a contract method.
/// </summary>
public class ContractInvocationOptions
{
    /// <summary>
    /// Initializes a new instance of the ContractInvocationOptions class.
    /// </summary>
    /// <param name="nonce">Transaction sequence number to prevent replay attacks (null = auto-detect)</param>
    /// <param name="gas">Gas pricing and limit configuration for the transaction</param>
    /// <param name="value">Amount of ETH (in wei) to send with the transaction</param>
    public ContractInvocationOptions(ulong? nonce, GasOptions? gas, BigInteger? value)
    {
        Nonce = nonce;
        Gas = gas;
        Value = value;
    }

    /// <summary>
    /// Transaction sequence number to prevent replay attacks (null = auto-detect)
    /// </summary>
    public ulong? Nonce { get; }

    /// <summary>
    /// Gas pricing and limit configuration for the transaction
    /// </summary>
    public GasOptions? Gas { get; }

    /// <summary>
    /// Amount of ETH (in wei) to send with the transaction
    /// </summary>
    public BigInteger? Value { get; }
}

/// <summary>
/// Options for the gas price of a transaction.
/// </summary>
public abstract class GasOptions
{
    /// <summary>
    /// Initializes a new instance of the GasOptions class. 
    /// </summary>
    /// <param name="limit">Maximum gas units the transaction can consume</param>
    protected GasOptions(ulong limit)
    {
        Limit = limit;
    }

    /// <summary>
    /// Maximum gas units the transaction can consume
    /// </summary>
    public ulong Limit { get; }
}

/// <summary>
/// Options for the gas price of a legacy transaction.
/// </summary>
public class LegacyGasOptions : GasOptions
{
    /// <summary>
    /// Initializes a new instance of the LegacyGasOptions class.
    /// </summary>
    /// <param name="limit">Maximum gas units the transaction can consume</param>
    /// <param name="price">Price per gas unit in wei (higher = faster processing)</param>
    public LegacyGasOptions(ulong limit, BigInteger price) : base(limit)
    {
        Price = price;
    }

    /// <summary>
    /// Price per gas unit in wei (higher = faster processing)
    /// </summary>
    public BigInteger Price { get; }
}

/// <summary>
/// Options for the gas price of an EIP-1559 transaction.
/// </summary>
public class EIP1559GasOptions : GasOptions
{
    /// <summary>
    /// Initializes a new instance of the EIP1559GasOptions class.
    /// </summary>
    /// <param name="limit">Maximum gas units the transaction can consume</param>
    /// <param name="maxFeePerGas">Maximum total fee per gas unit in wei (base fee + priority fee)</param>
    /// <param name="maxPriorityFeePerGas">Maximum tip to miners per gas unit in wei</param>
    public EIP1559GasOptions(ulong limit, BigInteger maxFeePerGas, BigInteger maxPriorityFeePerGas) : base(limit)
    {
        MaxFeePerGas = maxFeePerGas;
        MaxPriorityFeePerGas = maxPriorityFeePerGas;
    }

    /// <summary>
    /// Maximum total fee per gas unit in wei (base fee + priority fee)
    /// </summary>
    public BigInteger MaxFeePerGas { get; }

    /// <summary>
    /// Maximum tip to miners per gas unit in wei
    /// </summary>
    public BigInteger MaxPriorityFeePerGas { get; }
}