using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI;

public static class ArrayComparer
{
    /// <summary>
    /// Compares two arrays for equality, including support for jagged arrays.
    /// Throws an exception with detailed information if arrays don't match.
    /// </summary>
    /// <param name="expected">The expected array</param>
    /// <param name="actual">The actual array</param>
    /// <param name="message">Optional custom message prefix</param>
    /// <param name="path">Current path for error reporting (used in recursion)</param>
    public static void AssertEqual(Array expected, Array actual, string message = "Arrays are not equal", string path = "root")
    {
        // Check if both arrays are null
        if (expected == null && actual == null)
        {
            return;
        }

        // Check if only one array is null
        if (expected == null || actual == null)
        {
            throw new Exception($"{message}: At {path}: One array is null and the other is not");
        }

        // Check array types
        Type expectedType = expected.GetType();
        Type actualType = actual.GetType();

        if (expectedType != actualType)
        {
            throw new Exception($"{message}: At {path}: Type mismatch - Expected {expectedType.Name}, actual {actualType.Name}");
        }

        // Check array lengths
        if (expected.Length != actual.Length)
        {
            throw new Exception($"{message}: At {path}: Length mismatch - Expected {expected.Length}, actual {actual.Length}");
        }

        // Check array elements
        for (int i = 0; i < expected.Length; i++)
        {
            object? expectedItem = expected.GetValue(i);
            object? actualItem = actual.GetValue(i);
            string currentPath = $"{path}[{i}]";

            // Handle null values
            if (expectedItem == null && actualItem == null)
            {
                continue;
            }

            if (expectedItem == null || actualItem == null)
            {
                throw new Exception($"{message}: At {currentPath}: One value is null and the other is not");
            }

            // Check types of elements
            Type expectedItemType = expectedItem.GetType();
            Type actualItemType = actualItem.GetType();

            // Recursively compare nested arrays
            if (TryArray(expectedItem, out var exItemArr) && TryArray(actualItem, out var actItemArr))
            {
                AssertEqual(exItemArr!, actItemArr!, message, currentPath);
            }
            else if (expectedItemType != actualItemType)
            {
                throw new Exception($"{message}: At {currentPath}: Type mismatch - Expected {expectedItemType.Name}, actual {actualItemType.Name}");
            }
            else if (!Equals(expectedItem, actualItem))
            {
                throw new Exception($"{message}: At {currentPath}: Value mismatch - Expected '{expectedItem}', actual '{actualItem}'");
            }
        }
    }

    /// <summary>
    /// Checks if two arrays are equal, returning a boolean result
    /// </summary>
    public static bool AreEqual(Array expected, Array actual)
    {
        try
        {
            AssertEqual(expected, actual);
            return true;
        }
        catch
        {
            return false;
        }
    }

    //

    internal static bool TryArray(object obj, out Array? array)
    {
        if (obj is Array y)
        {
            array = y;
            return true;
        }

        if (obj is ITuple tuple)
        {
            array = tuple.ToList().ToArray();
            return true;
        }

        if (obj is IEnumerable<object> objects)
        {
            array = objects.ToArray();
            return true;
        }

        if (obj is IEnumerable<object?> maybeObjects)
        {
            array = maybeObjects.ToArray();
            return true;
        }

        array = null;
        return false;
    }
}