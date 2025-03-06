using System;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Contracts;

// A hypothetical Contract class would be able to produce function
// signatures for the methods in the ABI, and hold the address of
// the contract.
//
// contract.CallAsync("GetSchema", schemaId);
//
// contractCaller.CallAsync(contract, "GetSchema", schemaId);
//
// ContractCaller is a class that can be used to call methods on a
// contract. Is is configured with a IEthereumJsonRpc, IAbiEncoder,
// IAbiDecode, and a ITransactionSigner, and a INonceStore.
//
// !! We need something to compute the gas price.
//

/// <summary>
/// A class that can be used to call methods on a contract.
/// </summary>
public class ContractCaller
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
    public ContractCaller(
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

    /// <summary>
    /// Calls a method on a contract, off-chain, without creating a transaction.
    /// </summary>
    /// <param name="contract">The contract.</param>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="senderAddress">The address of the sender.</param>
    /// <param name="parameters">The parameters to pass to the method.</param>
    /// <returns>The result of the method call.</returns>
    public async Task<Hex> CallAsync(
        Contract contract,
        string methodName,
        EthereumAddress senderAddress,
        params object[] parameters)
    {
        var signature = contract.GetFunctionSignature(methodName);
        var encoded = signature.EncodeFullSignature(this.abiEncoder, parameters);

        var ethCallParams = new EthCallParamObjectDto
        {
            To = contract.Address.ToString(),
            From = senderAddress.ToString(),
            Input = new Hex(encoded).ToString(),
        };

        return await this.jsonRpc.CallAsync(ethCallParams, id: this.GetRandomId());
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
    public static ContractCaller CreateDefault(
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

            return new ContractCaller(jsonRpc, abiEncoder, abiDecoder, transactionSigner, sender.Value.NonceStore);
        }
        else
        {
            return new ContractCaller(jsonRpc, abiEncoder, abiDecoder, null, null);
        }
    }
}