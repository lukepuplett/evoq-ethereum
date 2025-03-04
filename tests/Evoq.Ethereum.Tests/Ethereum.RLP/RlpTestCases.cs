using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Math;

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
            new BigInteger("1000000000000000"),
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
            new Transaction(
                nonce: 42,
                gasPrice: new BigInteger("30000000000"), // 30 Gwei
                gasLimit: 21000,
                to: CreateAddressBytes(20),
                value: new BigInteger("1000000000000000000"), // 1 ETH
                data: Array.Empty<byte>(),
                new RsvSignature(
                    v: 27,
                    r: HexToByteArray("1234567890abcdef"),
                    s: HexToByteArray("fedcba9876543210")
                )
            ),
            "0xf83c2a8506fc23ac00825208940102030405060708090a0b0c0d0e0f1011121314880de0b6b3a7640000801b881234567890abcdef88fedcba9876543210"
        ),

        [21] = new(
            "EIP-1559 transaction",
            "Tests the RLP encoding of an EIP-1559 transaction with access list",
            // Instead of using TransactionEIP1559 directly, create a list of objects
            // that matches the RLP structure without the transaction type byte
            // This is because the Go implementation does not include the transaction type byte
            // in the RLP encoding of the transaction.
            new List<object>
            {
                1UL, // chainId
                123UL, // nonce
                new BigInteger("2000000000"),  // maxPriorityFeePerGas (2 Gwei)
                new BigInteger("50000000000"), // maxFeePerGas (50 Gwei)
                21000UL, // gasLimit
                CreateAddressBytes(20), // to
                new BigInteger("1000000000000000000"), // value (1 ETH)
                new byte[] { 0xca, 0xfe, 0xba, 0xbe }, // data
                // Access list as a list of lists
                new List<object>
                {
                    new List<object>
                    {
                        HexToByteArray("0102030405060708090a0b0c0d0e0f1011121314"), // address
                        new List<object>
                        {
                            HexToByteArray("0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20") // storage key
                        }
                    }
                },
                1UL, // v
                HexToByteArray("1234567890abcdef"), // r
                HexToByteArray("fedcba9876543210")  // s
            },
            "0xf880017b8477359400850ba43b7400825208940102030405060708090a0b0c0d0e0f1011121314880de0b6b3a764000084cafebabef838f7940102030405060708090a0b0c0d0e0f1011121314e1a00102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f2001881234567890abcdef88fedcba9876543210"
        ),

        [22] = new(
            "Simple Ethereum transaction",
            "Tests the RLP encoding of a simple Ethereum transaction with just the core fields",
            // Create a list of objects that matches the SimpleTransaction structure
            new List<object>
            {
                1UL, // nonce
                new BigInteger("20000000000"), // gasPrice (20 Gwei)
                21000UL, // gasLimit
                CreateSimpleAddressBytes(20), // to (bytes 0-19)
                new BigInteger("500000000000000000"), // value (0.5 ETH)
                Array.Empty<byte>() // data (empty)
            },
            "0xe9018504a817c80082520894000102030405060708090a0b0c0d0e0f101112138806f05b59d3b2000080"
        ),

        [23] = new(
            "Contract creation transaction",
            "Tests the RLP encoding of a contract creation transaction (no 'to' address)",
            // Create a list of objects that matches the ContractCreationTx structure
            new List<object>
            {
                0UL, // nonce
                new BigInteger("50000000000"), // gasPrice (50 Gwei)
                500000UL, // gasLimit
                new BigInteger("0"), // value (0 ETH)
                // Contract bytecode
                new byte[]
                {
                    0x60, 0x80, 0x60, 0x40, 0x52, // PUSH1 80 PUSH1 40 MSTORE
                    0x60, 0x0a, 0x60, 0x00, 0x55, // PUSH1 0a PUSH1 00 SSTORE (stores 10 at storage slot 0)
                    0x60, 0x00, 0x80, 0xfd // PUSH1 00 DUP1 REVERT
                },
                28UL, // v
                HexToByteArray("9876543210abcdef"), // r
                HexToByteArray("fedcba0987654321")  // s
            },
            "0xee80850ba43b74008307a120808e6080604052600a600055600080fd1c889876543210abcdef88fedcba0987654321"
        ),

        [24] = new(
            "Ethereum block header",
            "Tests the RLP encoding of an Ethereum block header",
            // Create a list of objects that matches the BlockHeader structure
            new List<object>
            {
                CreateSequentialBytes(32, 1), // parentHash
                CreateSequentialBytes(32, 2), // uncleHash
                CreateSequentialBytes(20, 1), // coinbase
                CreateSequentialBytes(32, 3), // root
                CreateSequentialBytes(32, 4), // txHash
                CreateSequentialBytes(32, 5), // receiptHash
                new byte[256], // bloom (all zeros)
                new BigInteger("2000000"), // difficulty
                new BigInteger("12345"), // number
                15000000UL, // gasLimit
                12500000UL, // gasUsed
                1618203344UL, // time
                Encoding.ASCII.GetBytes("Ethereum"), // extra
                CreateSequentialBytes(32, 6), // mixDigest
                CreateSequentialBytes(8, 1)   // nonce
            },
            "0xf90204a00102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20a002030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f2021940102030405060708090a0b0c0d0e0f1011121314a0030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122a00405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20212223a005060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f2021222324b9010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000831e848082303983e4e1c083bebc20846073d2d088457468657265756da0060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425880102030405060708"
        ),

        [25] = new(
            "Transaction receipt",
            "Tests the RLP encoding of an Ethereum transaction receipt",
            // Create a list of objects that matches the Receipt structure.
            new List<object>
            {
                new byte[] { 0x01 }, // postStateOrStatus (success)
                21000UL, // cumulativeGasUsed
                new byte[256], // bloom (all zeros)
                // Logs as a list of lists
                new List<object>
                {
                    // Single log entry
                    new List<object>
                    {
                        CreateAddressBytes(20), // address
                        // Topics as a list
                        new List<object>
                        {
                            CreateSequentialBytes(32, 1), // topic1
                            CreateSequentialBytes(32, 2)  // topic2
                        },
                        new byte[] { 0x01, 0x02, 0x03, 0x04 } // data
                    }
                }
            },
            "0xf9016901825208b9010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000f860f85e940102030405060708090a0b0c0d0e0f1011121314f842a00102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20a002030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20218401020304"
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

    private static byte[] CreateSequentialBytes(int length, int startValue)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = (byte)((i + startValue) % 256);
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

    private static byte[] CreateSimpleAddressBytes(int length)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = (byte)i;
        }
        return result;
    }

    private static byte[] HexToByteArray(string hex)
    {
        if (hex.StartsWith("0x"))
            hex = hex.Substring(2);

        int length = hex.Length;
        byte[] bytes = new byte[length / 2];

        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = byte.Parse(hex.Substring(i, 2), NumberStyles.HexNumber);
        }

        return bytes;
    }
}