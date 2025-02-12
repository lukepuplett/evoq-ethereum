using System.Threading.Tasks;
using Evoq.Blockchain;
using Nethereum.Web3;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Issues a JSON-RPC request to get the code of a contract using the `eth_getCode` method.
/// </summary>
public class GetCode : IGetCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCode"/> class.
    /// </summary>
    /// <param name="endpoint">The endpoint to use for the request.</param>
    public GetCode(Endpoint endpoint)
    {
        this.Endpoint = endpoint;
    }

    //

    public Endpoint Endpoint { get; }

    //

    /// <summary>
    /// Issues a JSON-RPC request to get the code of a contract using the `eth_getCode` method.
    /// </summary>
    /// <param name="address">The address of the contract to get the code for.</param>
    /// <returns>The code of the contract.</returns>
    public async Task<Hex> GetCodeAsync(EthereumAddress address)
    {
        var web3 = new Web3(this.Endpoint.URL);

        var result = await web3.Eth.GetCode.SendRequestAsync(address.ToString());

        return Hex.Parse(result);
    }
}
