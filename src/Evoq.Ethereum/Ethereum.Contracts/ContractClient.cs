using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.RLP;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Contracts;

/// <summary>
/// Performs stateless contract operations.
/// </summary>
internal class ContractClient
{
    private readonly Random rng = new Random();

    private readonly IEthereumJsonRpc jsonRpc;
    private readonly IAbiEncoder abiEncoder;
    private readonly IAbiDecoder abiDecoder;
    private readonly ITransactionSigner? transactionSigner;
    private readonly IRlpTransactionEncoder rlpEncoder;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;

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
    /// <param name="loggerFactory">The logger factory.</param>
    internal ContractClient(
        IEthereumJsonRpc jsonRpc,
        IAbiEncoder abiEncoder,
        IAbiDecoder abiDecoder,
        ITransactionSigner? transactionSigner,
        IRlpTransactionEncoder rlpEncoder,
        ulong chainId,
        ILoggerFactory loggerFactory)
    {
        this.jsonRpc = jsonRpc;
        this.abiEncoder = abiEncoder;
        this.abiDecoder = abiDecoder;
        this.transactionSigner = transactionSigner;
        this.rlpEncoder = rlpEncoder;
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory?.CreateLogger<ContractClient>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        this.ChainId = chainId;
    }

    //

    /// <summary>
    /// The chain ID.
    /// </summary>
    public ulong ChainId { get; }

    //

    internal async Task<T> CallAsync<T>(
        IJsonRpcContext context,
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments)
    where T : new()
    {
        var (result, signature) = await this.ExecuteCallAsync(
            context,
            contract,
            methodName,
            senderAddress,
            arguments);

        var decoded = signature.AbiDecodeOutputs(this.abiDecoder, result.ToByteArray());

        return decoded.Parameters.ToObject<T>(this.loggerFactory);
    }

    internal async Task<Dictionary<string, object?>> CallAsync(
        IJsonRpcContext context,
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments)
    {
        var (result, signature) = await this.ExecuteCallAsync(
            context,
            contract,
            methodName,
            senderAddress,
            arguments);

        var decoded = signature.AbiDecodeOutputs(this.abiDecoder, result.ToByteArray());

        return decoded.Parameters.ToDictionary(false);
    }

    internal async Task<Hex> InvokeMethodAsync(
        IJsonRpcContext context,
        Contract contract,
        string methodName,
        ulong nonce,
        ContractInvocationOptions options,
        IDictionary<string, object?> arguments)
    {
        // note that access lists are not supported, and no type 1 transactions

        var signature = contract.GetFunctionSignature(methodName);
        var encoded = signature.AbiEncodeCallValues(this.abiEncoder, arguments);

        byte[] rlpEncoded;

        if (options.Gas is LegacyGasOptions legacyGasOptions)
        {
            // construct a legacy transaction
            var transaction = new TransactionType0(
                nonce: nonce,
                gasPrice: legacyGasOptions.Price.ToBigBouncy(),
                gasLimit: options.Gas.GasLimit,
                to: contract.Address.ToByteArray(),
                value: options.Value.ToBigBouncy(),
                data: encoded,
                signature: null);

            transaction = (TransactionType0)this.transactionSigner!.GetSignedTransaction(transaction);
            rlpEncoded = this.rlpEncoder.Encode(transaction);
        }
        else if (options.Gas is EIP1559GasOptions eip1559GasOptions)
        {
            // construct an EIP-1559 transaction
            var transaction = new TransactionType2(
                chainId: this.ChainId,
                nonce: nonce,
                maxPriorityFeePerGas: eip1559GasOptions.MaxPriorityFeePerGas.ToBigBouncy(),
                maxFeePerGas: eip1559GasOptions.MaxFeePerGas.ToBigBouncy(),
                gasLimit: options.Gas.GasLimit,
                to: contract.Address.ToByteArray(),
                value: options.Value.ToBigBouncy(),
                data: encoded,
                accessList: null,
                signature: null);

            transaction = this.transactionSigner!.GetSignedTransaction(transaction);
            rlpEncoded = this.rlpEncoder.Encode(transaction);
        }
        else
        {
            throw new ArgumentException(
                "Cannot invoke method. The gas options specified are of an unsupported type.",
                nameof(options));
        }

        var rlpHex = new Hex(rlpEncoded);
        var id = this.GetRandomId();

        this.logger.LogInformation("Sending transaction: RLP: {Rlp}..., ID: {Id}", rlpHex.ToString()[..18], id);

        var transactionHash = await this.jsonRpc.SendRawTransactionAsync(context, rlpHex);

        this.logger.LogInformation(
            "Transaction sent to {BaseAddress}: ID: {Id}, TransactionHash: {Hash}",
            this.jsonRpc.BaseAddress.GetLeftPart(UriPartial.Authority), // mitigates logging of the API key
            id,
            transactionHash);

        return transactionHash;
    }

    internal bool TryRead(
        TransactionReceipt receipt,
        AbiSignature eventSignature,
        out IReadOnlyDictionary<string, object?>? indexed,
        out IReadOnlyDictionary<string, object?>? data)
    {
        var reader = new EventLogReader(this.abiDecoder);

        return reader.TryRead(receipt, eventSignature, out indexed, out data);
    }

    internal async Task<Hex> EstimateGasAsync(
        IJsonRpcContext context,
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

        return await this.jsonRpc.EstimateGasAsync(context, transactionParams);
    }

    //

    private async Task<(Hex Results, AbiSignature Function)> ExecuteCallAsync(
        IJsonRpcContext context,
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        IDictionary<string, object?> arguments)
    {
        var signature = contract.GetFunctionSignature(methodName);
        var encodedBytes = signature.AbiEncodeCallValues(this.abiEncoder, arguments);

        var ethCallParams = new EthCallParamObjectDto
        {
            To = contract.Address.HasValue ? contract.Address.ToString() : throw new InvalidOperationException("Contract address is not set"),
            From = senderAddress.HasValue ? senderAddress.ToString() : throw new InvalidOperationException("Sender address is not set"),
            Input = new Hex(encodedBytes).ToString(),
        };

        var result = await this.jsonRpc.CallAsync(context, ethCallParams);

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
    internal static ContractClient CreateDefault(
        Endpoint endpoint,
        Sender? sender)
    {
        var chainId = ulong.TryParse(ChainNames.GetChainId(endpoint.NetworkName), out var id)
            ? id
            : throw new InvalidOperationException($"Cannot create a {nameof(ContractClient)} for an unsupported network.");

        var jsonRpc = new JsonRpcClient(new Uri(endpoint.URL), endpoint.LoggerFactory);
        var abiEncoder = new AbiEncoder(endpoint.LoggerFactory);
        var abiDecoder = new AbiDecoder(endpoint.LoggerFactory);
        var rlpEncoder = new RlpEncoder();

        TransactionSigner? transactionSigner = null;
        if (sender.HasValue)
        {
            transactionSigner = TransactionSigner.CreateDefault(sender.Value.SenderAccount.PrivateKey.ToByteArray());
        }

        return new ContractClient(
            jsonRpc,
            abiEncoder,
            abiDecoder,
            transactionSigner,
            rlpEncoder,
            chainId,
            endpoint.LoggerFactory);
    }
}
