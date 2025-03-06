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
    /// <returns>The estimated gas amount as a hex value</returns>
    Task<Hex> EstimateGasAsync(TransactionParamsDto transactionParams, int id = 1);
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
    /// <returns>The transaction hash as a hex value</returns>
    Task<Hex> SendTransactionAsync(TransactionParamsDto transactionParams, int id = 1);
}

/// <summary>
/// Interface for the eth_sendRawTransaction RPC method
/// </summary>
public interface ISendRawTransaction
{
    /// <summary>
    /// Sends a signed transaction to the network
    /// </summary>
    /// <param name="signedTransaction">The RLP encoded signed transaction</param>
    /// <param name="id">The request identifier</param>
    /// <returns>The transaction hash as a hex value</returns>
    Task<Hex> SendRawTransactionAsync(Hex signedTransaction, int id = 1);
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
    /// <returns>The return data from the call as a hex value</returns>
    Task<Hex> CallAsync(EthCallParamObjectDto ethCallParams, string blockParameter = "latest", int id = 1);
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
    /// <returns>The transaction count as a hex value</returns>
    Task<Hex> GetTransactionCountAsync(EthereumAddress address, string blockParameter = "latest", int id = 1);
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
    /// <returns>The balance as a hex value</returns>
    Task<Hex> GetBalanceAsync(EthereumAddress address, string blockParameter = "latest", int id = 1);
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
    /// <returns>The code of the contract.</returns>
    Task<Hex> GetCodeAsync(EthereumAddress address);
}

/// <summary>
/// Composite interface for all Ethereum JSON-RPC methods
/// </summary>
public interface IEthereumJsonRpc :
   IEstimateGas,
   ISendTransaction,
   ISendRawTransaction,
   IEthCall,
   IGetTransactionCount,
   IGetBalance,
   IGetCode
{
    // This interface combines all the individual method interfaces
    // for convenience when you need access to all methods
}
