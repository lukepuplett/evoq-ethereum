using System.Linq;
using System.Text;
using Evoq.Blockchain;
using Nethereum.Util;

namespace Evoq.Ethereum.EIP165;

/// <summary>
/// EIP-165 interface ID and ABI.
/// </summary>
/// <remarks>
/// https://eips.ethereum.org/EIPS/eip-165
/// 
/// EIP-165 is a standard for checking if a contract implements an interface. The
/// interface ID is a 4-byte identifier that is computed by XORing the function
/// selectors of all its functions. Each function selector is the first 4 bytes
/// of the keccak256 hash of the function signature.
/// </remarks>
public static class Help165
{
    /// <summary>
    /// The EIP-165 interface ID for the EIP-165 standard itself
    /// </summary>
    public static readonly Hex EIP165SelfInterfaceID = "0x01ffc9a7";

    /// <summary>
    /// The ABI for the EIP-165 standard
    /// </summary>
    public static readonly string ABI = @"[{
        ""inputs"": [{
            ""internalType"": ""bytes4"",
            ""name"": ""interfaceId"",
            ""type"": ""bytes4""
        }],
        ""name"": ""supportsInterface"",
        ""outputs"": [{
            ""internalType"": ""bool"",
            ""name"": """",
            ""type"": ""bool""
        }],
        ""stateMutability"": ""view"",
        ""type"": ""function""
    }]";

    /// <summary>
    /// Calculates an interface ID by XORing the function selectors of all its functions.
    /// Each function selector is the first 4 bytes of the keccak256 hash of the function signature.
    /// </summary>
    /// <param name="functionSignatures">Array of function signatures (e.g., "transfer(address,uint256)")</param>
    /// <returns>The interface ID as a 0x-prefixed hex string</returns>
    public static Hex ComputeInterfaceID(params string[] functionSignatures)
    {
        var result = new byte[4];

        foreach (var signature in functionSignatures)
        {
            byte[] selector = ComputeSelector(signature).ToByteArray();

            // XOR with running result
            for (int i = 0; i < 4; i++)
            {
                result[i] ^= selector[i];
            }
        }

        return result.ToHexStruct();
    }

    /// <summary>
    /// Computes a function selector from a function signature.
    /// </summary>
    /// <param name="functionSignature">The function signature</param>
    /// <returns>The function selector</returns>
    public static Hex ComputeSelector(string functionSignature)
    {
        return Sha3Keccack.Current.CalculateHash(Encoding.ASCII.GetBytes(functionSignature))
            .Take(4)
            .ToArray()
            .ToHexStruct();
    }
}
