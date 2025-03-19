using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Interface for ABI decoders.
/// </summary>
public interface IAbiDecoder
{
    /// <summary>
    /// Decodes the parameters.
    /// </summary>
    /// <param name="parameters">The parameters to decode.</param>
    /// <param name="data">The data to decode.</param>
    /// <returns>The decoded parameters.</returns>
    AbiDecodingResult DecodeParameters(AbiParameters parameters, byte[] data);
}

internal static class AbiDecoderExtensions
{
    /// <summary>
    /// Decodes a single parameter.
    /// </summary>
    /// <param name="decoder">The decoder.</param>
    /// <param name="parameter">The parameter to decode.</param>
    /// <param name="data">The data to decode.</param>
    /// <returns>The decoded parameter.</returns>
    public static object? DecodeParameter(this IAbiDecoder decoder, AbiParam parameter, byte[] data)
    {
        var parameters = new AbiParameters(new[] { parameter });
        var r = decoder.DecodeParameters(parameters, data);

        return r.Parameters.First().Value;
    }
}