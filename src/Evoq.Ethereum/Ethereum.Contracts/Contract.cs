using System;
using System.IO;
using System.Linq;
using Evoq.Ethereum.ABI;

namespace Evoq.Ethereum.Contracts;

/// <summary>
/// A class that represents a contract.
/// </summary>
public class Contract
{
    private readonly ContractAbi abi;

    //

    /// <summary>
    /// Initializes a new instance of the Contract class.   
    /// </summary>
    /// <param name="stream">The stream containing the ABI.</param>
    public Contract(Stream stream)
    {
        this.abi = ContractAbiReader.Read(stream);
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
}