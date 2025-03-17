using System;
using System.Collections.Generic;
using System.Linq;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// An encoder for Ethereum ABI parameters that packs the parameters into a single byte array.
/// </summary>
public class AbiEncoderPacked : IAbiEncoder
{
    private const int NaturalLength = -1;
    private const int ArrayElementLength = 32;

    //

    private readonly IReadOnlyList<IAbiEncode> typeEncoders;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncoderPacked"/> class.
    /// </summary>
    public AbiEncoderPacked()
    {
        this.typeEncoders = new AbiStaticTypeEncoders().Union(new AbiDynamicTypeEncoders()).ToList();

        // this.Validator = new AbiTypeValidator(staticTypeEncoders, dynamicTypeEncoders);
    }

    //

    /// <summary>
    /// Encodes the parameters into a single byte array using Ethereum ABI packed mode.
    /// Throws an exception if any value is null; use empty values (e.g., "", new byte[0], []) instead.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="values">The values to encode. Must not contain null values.</param>
    /// <returns>The encoded parameters as a single byte array.</returns>
    /// <exception cref="ArgumentException">Thrown if a parameter type is not supported for packing or a value is null.</exception>
    /// <exception cref="AbiTypeException">Thrown if a value cannot be encoded for its type.</exception>
    public AbiEncodingResult EncodeParameters(AbiParameters parameters, IDictionary<string, object?> values)
    {
        var unsupported = parameters.FirstOrDefault(p => !AbiTypes.IsPackingSupported(p.AbiType));
        if (unsupported != null)
        {
            throw new ArgumentException(
                $"Unable to encode. Parameter '{unsupported.SafeName}' is not supported for packing");
        }

        for (int i = 0; i < values.Count; i++)
        {
            var value = values.Values.ElementAt(i);
            var parameter = parameters.ElementAt(i);

            if (value == null)
            {
                throw new ArgumentException(
                    $"Unable to encode. Value for parameter '{parameter.SafeName}' is null");
            }
        }

        // encode each parameter in place, there is no nesting and no dynamic types
        // and only array elements are padded to 32 bytes

        // this will mean detecting arrays by pulling out their inner type

        List<byte[]> results = new();

        foreach (var parameter in parameters)
        {
            if (!values.TryGetValue(parameter.SafeName, out var value))
            {
                throw new InvalidOperationException(
                    $"Unable to encode. Value for parameter '{parameter.SafeName}' is missing");
            }

            if (AbiTypes.TryGetArrayInnerType(parameter.AbiType, out var innerType))
            {
                // array

                if (AbiEncoder.TryConvertToArray(value, out var array))
                {
                    // array

                    // encode each element in place

                    // pad to 32 bytes

                    foreach (var v in array)
                    {
                        if (!TryFindSlotEncoder(innerType!, v, ArrayElementLength, out var bytes))
                        {
                            throw new AbiTypeException(
                                $"Unable to encode. Value '{parameter.SafeName}' does not have a valid encoder for " +
                                $"{v.GetType()} -> {innerType}");
                        }

                        results.Add(bytes);
                    }
                }
                else
                {
                    throw new AbiEncodingException(
                        $"Unable to encode. Value for parameter '{parameter.SafeName}' is not an array");
                }
            }
            else
            {
                if (!TryFindSlotEncoder(parameter.AbiType, value!, NaturalLength, out var bytes))
                {
                    throw new AbiTypeException(
                        $"Unable to encode. Value '{parameter.SafeName}' does not have a valid encoder for " +
                        $"{value!.GetType()} -> {parameter.AbiType}");
                }

                results.Add(bytes);
            }
        }

        return new AbiEncodingResult(results.SelectMany(b => b).ToArray());
    }

    //

    private bool TryFindSlotEncoder(string abiType, object value, int length, out byte[] bytes)
    {
        if (value == null)
        {
            throw new AbiEncodingException($"Unable to encode. The value is null");
        }

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new AbiEncodingException(
                $"Internal error: Failed to resolve canonical type for '{abiType}'. " +
                $"This is likely a bug in the ABI encoder implementation.");
        }

        foreach (var enc in this.typeEncoders)
        {
            if (enc.TryEncode(canonicalType, value, out bytes, length))
            {
                return true;
            }
        }

        bytes = Array.Empty<byte>();
        return false;
    }
}
