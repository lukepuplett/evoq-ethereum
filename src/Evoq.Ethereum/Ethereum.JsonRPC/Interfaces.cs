using System.Threading;
using System.Threading.Tasks;
using global::Evoq.Blockchain;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Interface for the eth_estimateGas RPC method
/// </summary>
public interface IEstimateGas
{
    /// <summary>
    /// Estimates the gas needed to execute a transaction
    /// </summary>
    /// <param name="transactionParams">The transaction parameters</param>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The estimated gas amount as a hex value</returns>
    Task<Hex> EstimateGasAsync(TransactionParamsDto transactionParams, int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_gasPrice RPC method
/// </summary>
public interface IGasPrice
{
    /// <summary>
    /// Gets the current gas price
    /// </summary>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The current gas price as a hex value</returns>
    Task<Hex> GasPriceAsync(int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_sendTransaction RPC method
/// </summary>
public interface ISendTransaction
{
    /// <summary>
    /// Sends a transaction to the network
    /// </summary>
    /// <param name="transactionParams">The transaction parameters</param>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The transaction hash as a hex value</returns>
    Task<Hex> SendTransactionAsync(TransactionParamsDto transactionParams, int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_sendRawTransaction RPC method
/// </summary>
public interface ISendRawTransaction
{
    /// <summary>
    /// Sends a signed transaction to the network
    /// </summary>
    /// <param name="signedRlpTransaction">The RLP encoded signed transaction</param>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The transaction hash as a hex value</returns>
    Task<Hex> SendRawTransactionAsync(Hex signedRlpTransaction, int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_call RPC method
/// </summary>
public interface IEthCall
{
    /// <summary>
    /// Executes a call without creating a transaction on the blockchain
    /// </summary>
    /// <param name="ethCallParams">The eth_call parameters, which looks similar to a transaction object but is not a transaction</param>
    /// <param name="blockParameter">The block parameter</param>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The return data from the call as a hex value</returns>
    Task<Hex> CallAsync(EthCallParamObjectDto ethCallParams, string blockParameter = "latest", int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_getTransactionCount RPC method
/// </summary>
public interface IGetTransactionCount
{
    /// <summary>
    /// Gets the transaction count for an address
    /// </summary>
    /// <param name="address">The address to get the transaction count for</param>
    /// <param name="blockParameter">The block parameter</param>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The transaction count as a hex value</returns>
    Task<Hex> GetTransactionCountAsync(EthereumAddress address, string blockParameter = "latest", int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_getBalance RPC method
/// </summary>
public interface IGetBalance
{
    /// <summary>
    /// Gets the balance for an address
    /// </summary>
    /// <param name="address">The address to get the balance for</param>
    /// <param name="blockParameter">The block parameter</param>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The balance as a hex value</returns>
    Task<Hex> GetBalanceAsync(EthereumAddress address, string blockParameter = "latest", int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// The JSON-RPC interface for the `eth_getCode` method.
/// </summary>
public interface IGetCode
{
    /// <summary>
    /// Gets the code of a contract.
    /// </summary>
    /// <param name="address">The address of the contract to get the code for.</param>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The code of the contract.</returns>
    Task<Hex> GetCodeAsync(EthereumAddress address, int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_chainId RPC method
/// </summary>
public interface IChainId
{
    /// <summary>
    /// Gets the chain ID of the current network
    /// </summary>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The chain ID as a hex value</returns>
    Task<Hex> ChainIdAsync(int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_blockNumber RPC method
/// </summary>
public interface IBlockNumber
{
    /// <summary>
    /// Gets the number of the most recent block
    /// </summary>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The block number as a hex value</returns>
    Task<Hex> BlockNumberAsync(int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_getBlockByNumber RPC method
/// </summary>
public interface IGetBlockByNumber
{
    /// <summary>
    /// Gets information about a block by block number or tag with transaction hashes
    /// </summary>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The block information.</returns>
    Task<BlockDataDto<string>?> GetBlockByNumberWithTxHashesAsync(string blockNumberOrTag, int id = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a block by block number or tag with full transaction objects
    /// </summary>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The block information.</returns>
    Task<BlockDataDto<TransactionDataDto>?> GetBlockByNumberWithTxObjectsAsync(string blockNumberOrTag, int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_feeHistory RPC method
/// </summary>
public interface IFeeHistory
{
    /// <summary>
    /// Gets a collection of historical gas information
    /// </summary>
    /// <param name="id">The request identifier</param>
    /// <param name="blockCount">Number of blocks to analyze</param>
    /// <param name="newestBlock">The newest block to consider ("latest" or block number)</param>
    /// <param name="rewardPercentiles">Percentiles to sample for priority fees</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Fee history data including base fees and priority fee percentiles</returns>
    Task<FeeHistoryDto?> FeeHistoryAsync(Hex blockCount, string newestBlock, double[] rewardPercentiles, int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the eth_getTransactionReceipt RPC method
/// </summary>
public interface IGetTransactionReceipt
{
    /// <summary>
    /// Gets the transaction receipt for a transaction hash
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction</param>
    /// <param name="id">The request identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The transaction receipt, or null if the transaction is not found or pending</returns>
    Task<TransactionReceiptDto?> GetTransactionReceiptAsync(Hex transactionHash, int id = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// Composite interface for all Ethereum JSON-RPC methods
/// </summary>
public interface IEthereumJsonRpc :
   IEstimateGas,
   IGasPrice,
   ISendTransaction,
   ISendRawTransaction,
   IEthCall,
   IGetTransactionCount,
   IGetBalance,
   IGetCode,
   IChainId,
   IBlockNumber,
   IGetBlockByNumber,
   IFeeHistory,
   IGetTransactionReceipt
{
    // This interface combines all the individual method interfaces
    // for convenience when you need access to all methods
}
