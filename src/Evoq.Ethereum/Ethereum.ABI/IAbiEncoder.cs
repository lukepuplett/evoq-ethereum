using System;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI
{
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
        AbiEncodingResult EncodeParameters(EvmParameters parameters, ITuple values);

        /// <summary>
        /// Resolves the encoder for a given type.
        /// </summary>
        /// <param name="abiType">The type to resolve the encoder for.</param>
        /// <param name="value">The value to encode.</param>
        /// <param name="encoder">The encoder for the given type.</param>
        /// <returns>True if the encoder was resolved, false otherwise.</returns>
        bool TryResolveEncoder(string abiType, object value, out Func<object, Slot>? encoder);
    }
}