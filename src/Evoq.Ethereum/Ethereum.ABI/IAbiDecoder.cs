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