using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.RLP;
using Evoq.Ethereum.Transactions;
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
    private readonly IRlpTransactionEncoder rlpEncoder;

    //

    /// <summary>
    /// Initializes a new instance of the ContractCaller class.
    /// </summary>
    /// <param name="jsonRpc">The JSON-RPC client.</param>
    /// <param name="abiEncoder">The ABI encoder.</param>
    /// <param name="abiDecoder">The ABI decoder.</param>
    /// <param name="transactionSigner">The transaction signer.</param>
    /// <param name="rlpEncoder">The RLP encoder.</param>
    /// <param name="chainId">The chain ID.</param>
    public ContractClient(
        IEthereumJsonRpc jsonRpc,
        IAbiEncoder abiEncoder,
        IAbiDecoder abiDecoder,
        ITransactionSigner? transactionSigner,
        IRlpTransactionEncoder rlpEncoder,
        ulong chainId)
    {
        this.jsonRpc = jsonRpc;
        this.abiEncoder = abiEncoder;
        this.abiDecoder = abiDecoder;
        this.transactionSigner = transactionSigner;
        this.rlpEncoder = rlpEncoder;
    }

    //

    internal async Task<T> CallAsync<T>(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments)
    where T : new()
    {
        var (result, signature) = await this.ExecuteCallAsync(contract, methodName, senderAddress, arguments);

        var decoded = signature.AbiDecodeReturnValues(this.abiDecoder, result.ToByteArray());

        return decoded.Parameters.ToObject<T>();
    }

    internal async Task<IDictionary<string, object?>> CallAsync(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments)
    {
        var (result, signature) = await this.ExecuteCallAsync(contract, methodName, senderAddress, arguments);

        var decoded = signature.AbiDecodeReturnValues(this.abiDecoder, result.ToByteArray());

        return decoded.Parameters.ToDictionary(false);
    }

    internal async Task<Hex> InvokeMethodAsync(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        ContractInvocationOptions options,
        IDictionary<string, object?> arguments)
    {
        // TODO / research access list usage for the transaction

        var signature = contract.GetFunctionSignature(methodName);
        var encoded = signature.AbiEncodeCallValues(this.abiEncoder, arguments);

        byte[] rlpEncoded;

        if (options.Gas is LegacyGasOptions legacyGasOptions)
        {
            // TODO / construct a legacy transaction

            var transaction = new Transaction(
                nonce: options.Nonce,
                gasPrice: legacyGasOptions.Price.ToBigBouncy(),
                gasLimit: options.Gas.Limit,
                to: contract.Address.ToByteArray(),
                value: options.Value.ToBigBouncy(),
                data: encoded,
                signature: null);

            transaction = (Transaction)this.transactionSigner!.GetSignedTransaction(transaction);
            rlpEncoded = this.rlpEncoder.Encode(transaction);
        }
        else if (options.Gas is EIP1559GasOptions eip1559GasOptions)
        {
            // construct an EIP-1559 transaction

            var transaction = new TransactionEIP1559(
                chainId: 1,
                nonce: options.Nonce,
                maxPriorityFeePerGas: eip1559GasOptions.MaxPriorityFeePerGas.ToBigBouncy(),
                maxFeePerGas: eip1559GasOptions.MaxFeePerGas.ToBigBouncy(),
                gasLimit: options.Gas.Limit,
                to: contract.Address.ToByteArray(),
                value: options.Value.ToBigBouncy(),
                data: encoded,
                accessList: null,
                signature: null);

            transaction = (TransactionEIP1559)this.transactionSigner!.GetSignedTransaction(transaction);
            rlpEncoded = this.rlpEncoder.Encode(transaction);
        }
        else
        {
            throw new ArgumentException(
                "Cannot invoke method. The gas options specified are of an unsupported type.",
                nameof(options));
        }

        // TODO / send the RLP encoded transaction over JSON RPC by encoding it as a hex string

        var transactionHex = new Hex(rlpEncoded);

        var result = await this.jsonRpc.SendRawTransactionAsync(transactionHex, id: this.GetRandomId());

        return result;
    }

    internal async Task<Hex> EstimateGasAsync(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        BigInteger? value,
        IDictionary<string, object?> arguments)
    {
        if (value.HasValue && value.Value < 0)
        {
            throw new ArgumentException("Value must be greater than 0", nameof(value));
        }

        var signature = contract.GetFunctionSignature(methodName);
        var encoded = signature.AbiEncodeCallValues(this.abiEncoder, arguments);

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
        IDictionary<string, object?> arguments)
    {
        var signature = contract.GetFunctionSignature(methodName);
        var encoded = signature.AbiEncodeCallValues(this.abiEncoder, arguments);

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
    /// <param name="endpoint">The endpoint to use to call the contract.</param>
    /// <param name="sender">The sender; if null, the ContractCaller will be read-only; attempts to send transactions will throw.</param>
    /// <returns>The ContractCaller instance.</returns>
    public static ContractClient CreateDefault(
        Endpoint endpoint,
        Sender? sender)
    {
        var chainId = ulong.TryParse(ChainNames.GetChainId(endpoint.NetworkName), out var id)
            ? id
            : throw new InvalidOperationException($"Cannot create a {nameof(ContractClient)} for an unsupported network.");

        var jsonRpc = new JsonRpcClient(new Uri(endpoint.URL), endpoint.LoggerFactory);
        var abiEncoder = new AbiEncoder();
        var abiDecoder = new AbiDecoder();
        var rlpEncoder = new RlpEncoder();

        if (sender.HasValue)
        {
            var transactionSigner = TransactionSigner.CreateDefault(sender.Value.PrivateKey.ToByteArray());

            return new ContractClient(jsonRpc, abiEncoder, abiDecoder, transactionSigner, rlpEncoder, chainId);
        }
        else
        {
            return new ContractClient(jsonRpc, abiEncoder, abiDecoder, null, rlpEncoder, chainId);
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
    public ContractInvocationOptions(ulong nonce, GasOptions gas, EtherAmount value)
    {
        this.Nonce = nonce;
        this.Gas = gas;
        this.Value = value;
    }

    /// <summary>
    /// Transaction sequence number to prevent replay attacks (null = auto-detect)
    /// </summary>
    public ulong Nonce { get; }

    /// <summary>
    /// Gas pricing and limit configuration for the transaction
    /// </summary>
    public GasOptions Gas { get; }

    /// <summary>
    /// Amount of ETH (in wei) to send with the transaction
    /// </summary>
    public EtherAmount Value { get; }
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