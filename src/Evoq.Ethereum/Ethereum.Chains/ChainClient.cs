using System;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.RLP;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Performs stateless blockchain operations.
/// </summary>
internal class ChainClient
{
    private readonly Random rng = new Random();
    private readonly ulong chainId;
    private readonly IEthereumJsonRpc jsonRpc;
    private readonly IRlpTransactionEncoder? rlpEncoder;
    private readonly ITransactionSigner? transactionSigner;
    private readonly ILogger logger;
    //

    /// <summary>
    /// Initializes a new instance of the ChainClient class.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="jsonRpc">The JSON-RPC client.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="rlpEncoder">The RLP encoder.</param>
    /// <param name="transactionSigner">The transaction signer.</param>
    internal ChainClient(
        ulong chainId,
        IEthereumJsonRpc jsonRpc,
        ILoggerFactory loggerFactory,
        IRlpTransactionEncoder? rlpEncoder,
        ITransactionSigner? transactionSigner)
    {
        this.chainId = chainId;
        this.jsonRpc = jsonRpc;
        this.logger = loggerFactory.CreateLogger<ChainClient>();
        this.rlpEncoder = rlpEncoder;
        this.transactionSigner = transactionSigner;
    }

    //

    /// <summary>
    /// Gets the current gas price.
    /// </summary>
    /// <returns>The current gas price.</returns>
    internal async Task<Hex> GasPriceAsync(IJsonRpcContext context)
    {
        return await this.jsonRpc.GasPriceAsync(context);
    }

    /// <summary>
    /// Gets the chain ID.
    /// </summary>
    /// <returns>The chain ID as a hexadecimal string.</returns>
    internal async Task<Hex> ChainIdAsync(IJsonRpcContext context)
    {
        return await this.jsonRpc.ChainIdAsync(context);
    }

    /// <summary>
    /// Gets the current block number.
    /// </summary>
    /// <returns>The current block number as a hexadecimal string.</returns>
    internal async Task<Hex> GetBlockNumberAsync(IJsonRpcContext context)
    {
        return await this.jsonRpc.BlockNumberAsync(context);
    }

    /// <summary>
    /// Gets a block by number with transaction hashes.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <returns>The block data with transaction hashes.</returns>
    internal async Task<BlockData<string>?> TryGetBlockByNumberWithTxHashesAsync(IJsonRpcContext context, string blockNumberOrTag)
    {
        var dto = await this.jsonRpc.GetBlockByNumberWithTxHashesAsync(context, blockNumberOrTag);

        if (dto == null)
        {
            return null;
        }

        return BlockData<string>.FromDto(dto, hash => hash);
    }

    /// <summary>
    /// Gets a block by number with transaction objects.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <returns>The block data with transaction objects.</returns>
    internal async Task<BlockData<TransactionData>?> TryGetBlockByNumberWithTxObjectsAsync(IJsonRpcContext context, string blockNumberOrTag)
    {
        var dto = await this.jsonRpc.GetBlockByNumberWithTxObjectsAsync(context, blockNumberOrTag);

        if (dto == null)
        {
            return null;
        }

        return BlockData<TransactionData>.FromDto(dto, t => TransactionData.FromDto(t)!);
    }

    /// <summary>
    /// Gets fee history data for recent blocks.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="blockCount">Number of blocks to analyze.</param>
    /// <param name="newestBlock">The newest block to consider ("latest" or block number).</param>
    /// <param name="rewardPercentiles">Percentiles to sample for priority fees.</param>
    /// <returns>Fee history data including base fees and priority fee percentiles.</returns>
    internal async Task<FeeHistory> GetFeeHistoryAsync(IJsonRpcContext context, ulong blockCount, string newestBlock, double[] rewardPercentiles)
    {
        var dto = await this.jsonRpc.FeeHistoryAsync(
            context,
            blockCount.NumberToHexStruct(),
            newestBlock,
            rewardPercentiles);

        return new FeeHistory();
    }

    /// <summary>
    /// Gets the transaction count (nonce) for an address.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="address">The address to get the transaction count for.</param>
    /// <param name="blockParameter">The block parameter (defaults to "latest").</param>
    /// <returns>The transaction count as a hexadecimal value.</returns>
    internal async Task<Hex> GetTransactionCountAsync(IJsonRpcContext context, EthereumAddress address, string blockParameter = "latest")
    {
        return await this.jsonRpc.GetTransactionCountAsync(context, address, blockParameter);
    }

    /// <summary>
    /// Gets the transaction receipt for a given transaction hash.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="transactionHash">The hash of the transaction.</param>
    /// <returns>The transaction receipt, or null if the transaction is not found or pending.</returns>
    internal async Task<TransactionReceipt?> TryGetTransactionReceiptAsync(IJsonRpcContext context, Hex transactionHash)
    {
        var dto = await this.jsonRpc.GetTransactionReceiptAsync(context, transactionHash);

        return TransactionReceipt.FromDto(dto);
    }

    /// <summary>
    /// Waits for a transaction receipt until a deadline is reached.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="transactionHash">The transaction hash to wait for.</param>
    /// <param name="pollingInterval">The polling interval.</param>
    /// <param name="deadline">The absolute deadline after which polling will stop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The transaction receipt, or null if not found before the deadline.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    internal async Task<(TransactionReceipt? Receipt, bool DeadlineReached)> TryWaitForTransactionReceiptAsync(
        IJsonRpcContext context,
        Hex transactionHash,
        ChainPollingStrategy.PollingInterval pollingInterval,
        DateTime deadline,
        CancellationToken cancellationToken = default)
    {
        var currentInterval = pollingInterval.Initial;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var receipt = await TryGetTransactionReceiptAsync(context, transactionHash);
            if (receipt != null)
            {
                return (receipt, false);
            }

            var delayTime = DateTime.UtcNow + currentInterval < deadline
                ? currentInterval
                : deadline - DateTime.UtcNow;

            if (delayTime > TimeSpan.Zero)
            {
                await Task.Delay(delayTime, cancellationToken);
            }

            // Implement exponential backoff with chain-specific maximum
            currentInterval = TimeSpan.FromTicks(Math.Min(
                currentInterval.Ticks * 2,
                pollingInterval.Maximum.Ticks));
        }

        return (null, true);
    }

    /// <summary>
    /// Gets the balance of an Ethereum address at a specific block.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="address">The address to get the balance for.</param>
    /// <param name="blockParameter">The block parameter (defaults to "latest").</param>
    /// <returns>The balance as an EtherAmount in Wei.</returns>
    internal async Task<EtherAmount> GetBalanceAsync(IJsonRpcContext context, EthereumAddress address, string blockParameter = "latest")
    {
        return await this.jsonRpc.GetBalanceAsync(context, address, blockParameter);
    }

    /// <summary>
    /// Gets the code of a contract at a specific Ethereum address.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="address">The Ethereum address.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <returns>The code of the contract.</returns>
    internal async Task<Hex> GetCodeAsync(IJsonRpcContext context, EthereumAddress address, string blockParameter = "latest")
    {
        return await this.jsonRpc.GetCodeAsync(context, address, blockParameter);
    }

    internal async Task<Hex> TransferAsync(
        IJsonRpcContext context,
        ulong nonce,
        TransferInvocationOptions options)
    {
        if (this.rlpEncoder == null || this.transactionSigner == null)
        {
            throw new InvalidOperationException(
                "Transaction sending is disabled. This client was not configured with an RLP encoder or" +
                " transaction signer. Create a new client with a sender to enable transaction sending.");
        }

        byte[] rlpEncoded;

        if (options.Gas is LegacyGasOptions legacyGasOptions)
        {
            // construct a legacy transaction
            var transaction = new TransactionType0(
                nonce: nonce,
                gasPrice: legacyGasOptions.Price.ToBigBouncy(),
                gasLimit: options.Gas.GasLimit,
                to: options.Recipient.ToByteArray(),
                value: options.Value.ToBigBouncy(),
                data: new byte[0],
                signature: null);

            transaction = (TransactionType0)this.transactionSigner!.GetSignedTransaction(transaction);
            rlpEncoded = this.rlpEncoder.Encode(transaction);
        }
        else if (options.Gas is EIP1559GasOptions eip1559GasOptions)
        {
            // construct an EIP-1559 transaction
            var transaction = new TransactionType2(
                chainId: this.chainId,
                nonce: nonce,
                maxPriorityFeePerGas: eip1559GasOptions.MaxPriorityFeePerGas.ToBigBouncy(),
                maxFeePerGas: eip1559GasOptions.MaxFeePerGas.ToBigBouncy(),
                gasLimit: options.Gas.GasLimit,
                to: options.Recipient.ToByteArray(),
                value: options.Value.ToBigBouncy(),
                data: new byte[0],
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
        var id = this.rng.Next();

        this.logger.LogInformation("Sending transaction: RLP: {Rlp}..., ID: {Id}", rlpHex.ToString()[..18], id);

        var transactionHash = await this.jsonRpc.SendRawTransactionAsync(context, rlpHex);

        this.logger.LogInformation(
            "Transaction sent to {BaseAddress}: ID: {Id}, TransactionHash: {Hash}",
            this.jsonRpc.BaseAddress.GetLeftPart(UriPartial.Authority), // mitigates logging of the API key
            id,
            transactionHash);

        return transactionHash;
    }

    //

    /// <summary>
    /// Creates a default chain client.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="uri">The URI of the chain.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The chain client.</returns>
    internal static ChainClient CreateDefault(ulong chainId, Uri uri, ILoggerFactory loggerFactory)
    {
        var jsonRpc = new JsonRpcClient(uri, loggerFactory);

        return new ChainClient(chainId, jsonRpc, loggerFactory, null, null);
    }

    /// <summary>
    /// Creates a default chain client.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="uri">The URI of the chain.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="sender">The sender.</param>
    /// <returns>The chain client.</returns>
    internal static ChainClient CreateDefault(ulong chainId, Uri uri, ILoggerFactory loggerFactory, Sender sender)
    {
        var jsonRpc = new JsonRpcClient(uri, loggerFactory);
        var rlpEncoder = new RlpEncoder();
        var transactionSigner = TransactionSigner.CreateDefault(sender.SenderAccount.PrivateKey.ToByteArray());

        return new ChainClient(chainId, jsonRpc, loggerFactory, rlpEncoder, transactionSigner);
    }
}