using System.Collections.Generic;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// An encoder for Ethereum ABI parameters.
/// </summary>
public interface IAbiEncoder
{
    /// <summary>
    /// Encodes the parameters.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="values">The values to encode.</param>
    /// <returns>The encoded parameters.</returns>
    AbiEncodingResult EncodeParameters(AbiParameters parameters, IDictionary<string, object?> values);
}
