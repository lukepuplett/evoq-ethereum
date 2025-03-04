using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Represents a test case for RLP encoding, containing the input value,
/// description, and expected hex output.
/// </summary>
public record RlpTestCase(
    string Name,
    string Description,
    object Value,
    string ExpectedHex
);

public static class RlpTestCases
{
    public static readonly Dictionary<int, RlpTestCase> Cases = new()
    {
        [1] = new(
            "Empty string",
            "Tests the RLP encoding of an empty string",
            string.Empty,
            "0x80"
        ),

        [2] = new(
            "Single byte (< 0x80)",
            "Tests the RLP encoding of a single byte in the [0x00, 0x7f] range",
            new byte[] { 0x7f },
            "0x7f"
        ),

        [3] = new(
            "Short string (< 56 bytes)",
            "Tests the RLP encoding of a short string",
            "hello world",
            "0x8b68656c6c6f20776f726c64"
        ),

        [4] = new(
            "Long string (>= 56 bytes)",
            "Tests the RLP encoding of a long string",
            CreateLongByteArray(100),
            "0xb864000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f60616263"
        ),

        [5] = new(
            "Zero",
            "Tests the RLP encoding of zero",
            0UL,
            "0x80"
        ),

        [6] = new(
            "Small integer",
            "Tests the RLP encoding of a small integer",
            42UL,
            "0x2a"
        ),

        [7] = new(
            "Medium integer",
            "Tests the RLP encoding of a medium-sized integer",
            1024UL,
            "0x820400"
        ),

        [8] = new(
            "Large integer",
            "Tests the RLP encoding of a large integer using BigInteger",
            BigInteger.Parse("1000000000000000"),
            "0x87038d7ea4c68000"
        ),

        [9] = new(
            "Negative integer",
            "RLP itself is byte-agnostic. For this test, we pre-convert the negative number to its big-endian byte representation.",
            // Pre-convert -1000000 to its big-endian byte representation (0x0f4240)
            new byte[] { 0x0f, 0x42, 0x40 },
            "0x830f4240"
        ),

        [10] = new(
            "Empty list",
            "Tests the RLP encoding of an empty list",
            Array.Empty<object>(),
            "0xc0"
        ),

        [11] = new(
            "List with a single element",
            "Tests the RLP encoding of a list with one item",
            new object[] { 1UL },
            "0xc101"
        ),

        [12] = new(
            "List with multiple elements of the same type",
            "Tests the RLP encoding of a homogeneous list",
            new object[] { 1UL, 2UL, 3UL },
            "0xc3010203"
        ),

        [13] = new(
            "List with mixed types",
            "Tests the RLP encoding of a heterogeneous list",
            new object[] { 1UL, "hello", new byte[] { 0x42 } },
            "0xc8018568656c6c6f42"
        ),

        [14] = new(
            "Nested list",
            "Tests the RLP encoding of a list containing another list",
            new object[] {
                1UL,
                new object[] { 2UL, 3UL },
                "hello"
            },
            "0xca01c202038568656c6c6f"
        ),

        [15] = new(
            "Deeply nested list",
            "Tests the RLP encoding of a list with multiple levels of nesting",
            new object[] {
                1UL,
                new object[] {
                    2UL,
                    new object[] { 3UL, "nested" }
                },
                "hello"
            },
            "0xd201ca02c803866e65737465648568656c6c6f"
        ),

        [16] = new(
            "Simple struct",
            "Tests the RLP encoding of a struct (represented as a list in C#)",
            new object[] { "Alice", 30UL },
            "0xc785416c6963651e"
        ),

        [17] = new(
            "Struct with nested struct",
            "Tests the RLP encoding of a struct containing another struct (represented as nested lists in C#)",
            new object[] {
                "Bob",
                25UL,
                new object[] { "123 Main St", "Anytown", "09" } // ZipCode simplified to string for this test
            },
            "0xdd83426f6219d78b313233204d61696e20537487416e79746f776e823039"
        ),

        [18] = new(
            "Struct with slice",
            "Tests the RLP encoding of a struct containing a slice (represented as a list in C#)",
            new object[] {
                "Team A",
                new object[] { "Alice", "Bob", "Charlie" }
            },
            "0xda865465616d2041d285416c69636583426f6287436861726c6965"
        ),

        [19] = new(
            "Byte arrays of different sizes",
            "Tests the RLP encoding of fixed-size byte arrays",
            new object[] {
                new byte[] { 0x01 },
                new byte[] { 0x02, 0x03 },
                new byte[] { 0x04, 0x05, 0x06 },
                new byte[] { 0x07, 0x08, 0x09, 0x0a }
            },
            "0xcd0182020383040506840708090a"
        ),

        [20] = new(
            "Basic Ethereum transaction (legacy format)",
            "Tests the RLP encoding of a legacy Ethereum transaction",
            new object[] {
                42UL, // nonce
                BigInteger.Parse("30000000000"), // gasPrice (30 Gwei)
                21000UL, // gasLimit
                CreateAddressBytes(20), // to
                BigInteger.Parse("1000000000000000000"), // value (1 ETH)
                Array.Empty<byte>(), // data
                27UL, // v
                new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xcd, 0xef }, // r
                new byte[] { 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10 }  // s
            },
            "0xf83c2a8506fc23ac00825208940102030405060708090a0b0c0d0e0f1011121314880de0b6b3a7640000801b881234567890abcdef88fedcba9876543210"
        )
    };

    private static byte[] CreateLongByteArray(int length)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = (byte)(i % 256);
        }
        return result;
    }

    private static byte[] CreateBytes(int length, byte startValue)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = (byte)((i + startValue) % 256);
        }
        return result;
    }

    private static byte[] CreateAddressBytes(int length)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = (byte)(i + 1);
        }
        return result;
    }
}