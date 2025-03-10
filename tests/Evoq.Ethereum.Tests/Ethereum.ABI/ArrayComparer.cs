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
            return;

        // Check if only one array is null
        if (expected == null || actual == null)
            throw new Exception($"{message}: At {path}: One array is null and the other is not");

        // Check array types
        Type expectedType = expected.GetType();
        Type actualType = actual.GetType();

        if (expectedType != actualType)
            throw new Exception($"{message}: At {path}: Type mismatch - Expected {expectedType.Name}, got {actualType.Name}");

        // Check array lengths
        if (expected.Length != actual.Length)
            throw new Exception($"{message}: At {path}: Length mismatch - Expected {expected.Length}, got {actual.Length}");

        // Check array elements
        for (int i = 0; i < expected.Length; i++)
        {
            object? expectedItem = expected.GetValue(i);
            object? actualItem = actual.GetValue(i);
            string currentPath = $"{path}[{i}]";

            // Handle null values
            if (expectedItem == null && actualItem == null)
                continue;

            if (expectedItem == null || actualItem == null)
                throw new Exception($"{message}: At {currentPath}: One value is null and the other is not");

            // Check types of elements
            Type expectedItemType = expectedItem.GetType();
            Type actualItemType = actualItem.GetType();

            if (expectedItemType != actualItemType)
                throw new Exception($"{message}: At {currentPath}: Type mismatch - Expected {expectedItemType.Name}, got {actualItemType.Name}");

            // Recursively compare nested arrays
            if (expectedItem is Array expectedNestedArray && actualItem is Array actualNestedArray)
            {
                AssertEqual(expectedNestedArray, actualNestedArray, message, currentPath);
            }
            // Compare regular values
            else if (!Equals(expectedItem, actualItem))
            {
                throw new Exception($"{message}: At {currentPath}: Value mismatch - Expected '{expectedItem}', got '{actualItem}'");
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
}