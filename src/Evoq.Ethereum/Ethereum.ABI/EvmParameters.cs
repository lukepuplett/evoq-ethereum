using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Static class for parsing and manipulating Ethereum function parameters.
/// </summary>
public class EvmParameters : System.Collections.ObjectModel.ReadOnlyCollection<EvmParam>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvmParameters"/> class.
    /// </summary>
    /// <param name="list">The list of parameters.</param>
    public EvmParameters(IList<EvmParam> list) : base(list)
    {
    }

    //

    /// <summary>
    /// Encodes the parameters.
    /// </summary>
    /// <param name="values">The values to encode.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <returns>The encoded parameters.</returns>
    public AbiEncodingResult Encode(ITuple values, IAbiEncoder encoder)
    {
        return encoder.EncodeParameters(this, values);
    }

    /// <summary>
    /// Returns the canonical type of the parameters.
    /// </summary>
    /// <param name="includeNames">Whether to include the names of the parameters.</param>
    /// <param name="includeSpaces">Whether to include spaces between the parameters.</param>
    /// <returns>The canonical type of the parameters.</returns>
    public string GetCanonicalType(bool includeNames = false, bool includeSpaces = false)
    {
        return EvmParam.GetCanonicalType(this, null, includeNames, includeSpaces);
    }

    /// <summary>
    /// Returns a list of single parameters from the current level and all nested levels, in order.
    /// </summary>
    /// <returns>A list of single parameters.</returns>
    public IReadOnlyList<EvmParam> DeepSingleParams()
    {
        var singles = new List<EvmParam>();

        this.DeepVisit(child =>
        {
            if (child.HasComponents)
            {
                singles.Add(child);
            }
        });

        return singles;
    }

    /// <summary>
    /// Counts the real number of parameters needing to be encoded.
    /// </summary>
    /// <returns>The number of parameters that need to be encoded.</returns>
    public int DeepCount()
    {
        int singles = 0;

        this.DeepVisit(child =>
        {
            if (child.HasComponents)
            {
                singles++;
            }
        });

        return singles;
    }

    /// <summary>
    /// Visits the parameters and their components, recursively.
    /// </summary>
    /// <param name="visit">The visitor.</param>
    /// <param name="depth">The depth of the parameters to visit where 0 is the current level.</param>
    internal void DeepVisit(Action<EvmParam> visit, int depth = int.MaxValue)
    {
        foreach (var param in this)
        {
            param.DeepVisit(visit, depth);
        }
    }

    //

    /// <summary>
    /// Returns the string representation of the parameters.
    /// </summary>
    /// <returns>The string representation of the parameters.</returns>
    public override string ToString()
    {
        return this.GetCanonicalType(includeNames: true, includeSpaces: true);
    }

    //

    /// <summary>
    /// Parses a parameter string into a list of tuples.
    /// </summary>
    /// <param name="descriptor">The parameter string or function signature, e.g. ((string first, string last) name, uint256 age, address wallet) person or ((string,uint256,address),bool).</param>
    /// <returns>An enumerable of tuples.</returns>
    /// <exception cref="ArgumentException">Thrown when the parameter string is not a valid function signature.</exception>
    public static EvmParameters Parse(string descriptor)
    {
        return new EvmParameters(SplitParams(descriptor));
    }

    private static List<EvmParam> SplitParams(string descriptor)
    {
        // we're expecting a parameter string of the form:
        //
        // ((string first, string last) name, uint256 age, address wallet) person
        //
        // or
        //
        // ((string,uint256,address)[],bool)
        //
        // ((string,uint256,(bool,uint256)) item)
        //
        // ((string name, uint256 value) item)

        // collect characters until we hit a comma at depth 0, i.e. we are in the
        // first level of parameters, in which case we yield the current parameter
        // and reset

        // check it starts with a '(' and ends with a ')'
        if (descriptor[0] != '(' || descriptor[^1] != ')')
            throw new ArgumentException($"Invalid descriptor '{descriptor}'. Missing parentheses.", nameof(descriptor));

        // remove the outermost parentheses and add a final comma, e.g. string, (uint256, bool), address,
        descriptor = descriptor[1..^1].Trim() + ',';

        var ordinal = 0;
        var isHarvestingParamType = true;
        var skipWhitespace = false;
        var currentParamType = new List<char>();
        var currentParamName = new List<char>();
        var currentParams = new List<EvmParam>();
        var currentArrayLengths = new List<int>();
        IReadOnlyList<EvmParam>? currentNestedParams = null;

        for (int i = 0; i < descriptor.Length; i++)
        {
            var c = descriptor[i];

            switch (c)
            {
                case '(': // start of nested descriptor, recurse
                    var nextCloseParen = findClosingParen(descriptor, i);
                    var nestedDesc = descriptor[i..(nextCloseParen + 1)];
                    currentNestedParams = SplitParams(nestedDesc); // recurse
                    i = nextCloseParen; // jump to the closing parenthesis
                    Debug.Assert(descriptor[i] == ')');
                    isHarvestingParamType = false; // we're now harvesting the param name
                    skipWhitespace = true; // expect a space then a name, or another paren, or a comma next
                    break;
                case ')':
                    break;
                case ' ':
                    if (skipWhitespace)
                        break;
                    isHarvestingParamType = !isHarvestingParamType; // toggle between type and name
                    skipWhitespace = true; // don't process consecutive spaces
                    break;
                case ',': // end of param or descriptor, perform reset ready for next param
                    var currentParam = makeParam(currentNestedParams); // make the param, add it to the components and reset
                    currentParams.Add(currentParam);
                    currentNestedParams = null;
                    currentParamType.Clear();
                    currentParamName.Clear();
                    currentArrayLengths.Clear();
                    isHarvestingParamType = true;
                    skipWhitespace = true; // don't process whitespace after the comma
                    ordinal++;
                    break;
                case '[': // start of array, collect the length
                    int nextCloseBracket = descriptor.IndexOf(']', i);
                    if (nextCloseBracket < i)
                        throw new ArgumentException($"Invalid descriptor '{descriptor}'. Missing closing square bracket.", nameof(descriptor));
                    var number = descriptor[(i + 1)..nextCloseBracket];
                    if (string.IsNullOrEmpty(number))
                        currentArrayLengths.Add(-1);
                    else if (int.TryParse(number, out int length))
                        currentArrayLengths.Add(length);
                    else
                        throw new ArgumentException($"Invalid descriptor '{descriptor}'. Array length '{number}' must be a number.", nameof(descriptor));
                    i = nextCloseBracket; // jump to the closing square bracket
                    break;
                case ']':
                    break;
                default:
                    skipWhitespace = false;
                    if (isHarvestingParamType)
                        currentParamType.Add(c);
                    else
                        currentParamName.Add(c);
                    break;
            }
        }

        return currentParams;

        EvmParam makeParam(IReadOnlyList<EvmParam>? components = null)
        {
            var name = new string(currentParamName.ToArray());

            string type = "";
            if (components == null) // only pass in a type if there are no components
            {
                type = new string(currentParamType.ToArray());
            }
            return new EvmParam(ordinal, name, type, currentArrayLengths.ToArray(), components);
        }

        int findClosingParen(string descriptor, int start)
        {
            var depth = 0;
            for (int i = start; i < descriptor.Length; i++)
            {
                if (descriptor[i] == '(')
                {
                    depth++;
                }
                else if (descriptor[i] == ')')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            throw new ArgumentException($"Invalid descriptor '{descriptor}'. Missing closing parenthesis.", nameof(descriptor));
        }
    }

    //


}