using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// Extension methods for system types.
/// </summary>
internal static class SystemExtensions
{
    /// <summary>
    /// Gets an enumerable of the tuple's elements.
    /// </summary>
    public static IEnumerable<object?> GetElements(this ITuple tuple)
    {
        for (int i = 0; i < tuple.Length; i++)
        {
            yield return tuple[i];
        }
    }
}