namespace Evoq.Ethereum.ABI;

/// <summary>
/// The result of decoding ABI parameters.
/// </summary>
public class AbiDecodingResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiDecodingResult"/> class.
    /// </summary>
    /// <param name="parameters">The parameters to decode.</param>
    public AbiDecodingResult(AbiParameters parameters)
    {
        this.Parameters = parameters;
    }

    //

    /// <summary>
    /// Gets the decoded parameters.
    /// </summary>
    public AbiParameters Parameters { get; }
}
