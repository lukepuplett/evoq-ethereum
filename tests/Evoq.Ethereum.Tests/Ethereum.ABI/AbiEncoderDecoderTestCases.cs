using System.Numerics;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represents a test case for ABI encoding, containing the function signature,
/// input values, and expected hex output.
/// </summary>
public record AbiTestCase(
    string Name,
    string Signature,
    IDictionary<string, object?> Values,
    List<string> ExpectedLines,
    string ReferenceHex = ""  // Default empty until Go tool output is provided
);

public static class AbiEncoderDecoderTestCases
{
    public static readonly Dictionary<int, AbiTestCase> Cases = new()
    {
        [1] = new(
            "Simple uint256",                        // Name
            "function foo(uint256)",                 // Signature
            AbiKeyValues.Create("0", BigInteger.One), // Values
            new List<string> {                       // Expected lines
                "0x0000000000000000000000000000000000000000000000000000000000000001  // uint256 value of 1"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
        ),

        [2] = new(
            "Simple bool",
            "function foo(bool)",
            AbiKeyValues.Create("0", true),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // bool value (true)"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
        ),

        [3] = new(
            "Simple uint8 and uint256",
            "function foo(uint8, uint256)",
            AbiKeyValues.Create("0", (byte)1, "1", BigInteger.One),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // uint8 value of 1",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // uint256 value of 1"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000001"
        ),

        [4] = new(
            "Simple static uint8 array",
            "function foo(uint8[2])",
            AbiKeyValues.Create("0", new byte[] { 1, 2 }),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // element 0",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // element 1"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000002"
        ),

        [5] = new(
            "Jagged static uint8 array",
            "function foo(uint8[4][2])",
            AbiKeyValues.Create("0", new byte[][] { new byte[] { 10, 20, 30, 40 }, new byte[] { 1, 2, 3, 4 } }),
            new List<string> {
                "0x000000000000000000000000000000000000000000000000000000000000000a  // element 0 of uint8[4] first element of outer array",
                "0x0000000000000000000000000000000000000000000000000000000000000014  // element 1 of uint8[4]",
                "0x000000000000000000000000000000000000000000000000000000000000001e  // element 2 of uint8[4]",
                "0x0000000000000000000000000000000000000000000000000000000000000028  // element 3 of uint8[4]",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // element 0 of uint8[4] second element of outer array",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // element 1 of uint8[4]",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // element 2 of uint8[4]",
                "0x0000000000000000000000000000000000000000000000000000000000000004  // element 3 of uint8[4]"
            },
            "0x000000000000000000000000000000000000000000000000000000000000000a"
            + "0000000000000000000000000000000000000000000000000000000000000014"
            + "000000000000000000000000000000000000000000000000000000000000001e"
            + "0000000000000000000000000000000000000000000000000000000000000028"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "0000000000000000000000000000000000000000000000000000000000000004"
        ),

        [6] = new(
            "Triple jagged static uint8 array",
            "function foo(uint8[3][2][1])",
            AbiKeyValues.Create("0", new byte[][][] { new byte[][] { new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 } } }),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // first array: element 0",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // first array: element 1",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // first array: element 2",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // second array: element 0",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // second array: element 1",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // second array: element 2"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000003"
        ),

        [7] = new(
            "Simple static tuple with two uint256",
            "function foo((uint256 id, uint256 balance) account)",
            AbiKeyValues.Create("account", AbiKeyValues.Create("id", (BigInteger)3u, "balance", (BigInteger)10u)),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000003  // account.id",
                "0x000000000000000000000000000000000000000000000000000000000000000a  // account.balance"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000003"
            + "000000000000000000000000000000000000000000000000000000000000000a"
        ),

        [8] = new(
            "Bool and static tuple with two uint256",
            "function foo(bool isActive, (uint256 id, uint256 balance) account)",
            AbiKeyValues.Create("isActive", true, "account", AbiKeyValues.Create("id", (BigInteger)3u, "balance", (BigInteger)10u)),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // isActive",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // account.id",
                "0x000000000000000000000000000000000000000000000000000000000000000a  // account.balance"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "000000000000000000000000000000000000000000000000000000000000000a"
        ),

        [9] = new(
            "Two static tuples with mixed static types",
            "function foo((bool isActive, uint256 seenUnix) prof, (uint256 id, uint256 balance) account)",
            AbiKeyValues.Create("prof", AbiKeyValues.Create("isActive", true, "seenUnix", (BigInteger)20u), "account", AbiKeyValues.Create("id", (BigInteger)3u, "balance", (BigInteger)10u)),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // prof.isActive",
                "0x0000000000000000000000000000000000000000000000000000000000000014  // prof.seenUnix",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // account.id",
                "0x000000000000000000000000000000000000000000000000000000000000000a  // account.balance"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000014"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "000000000000000000000000000000000000000000000000000000000000000a"
        ),

        [10] = new(
            "Nested static tuple with mixed static types",
            "function foo(((bool isActive, uint256 seenUnix) prof, uint256 id, uint256 balance) account)",
            AbiKeyValues.Create("account", AbiKeyValues.Create("prof", AbiKeyValues.Create("isActive", true, "seenUnix", (BigInteger)20u), "id", (BigInteger)3u, "balance", (BigInteger)10u)),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // account.prof.isActive",
                "0x0000000000000000000000000000000000000000000000000000000000000014  // account.prof.seenUnix",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // account.id",
                "0x000000000000000000000000000000000000000000000000000000000000000a  // account.balance"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000014"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "000000000000000000000000000000000000000000000000000000000000000a"
        ),

        [11] = new(
            "Simple bytes",
            "function foo(bytes)",
            AbiKeyValues.Create("0", new byte[] { 1 }),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000020  // offset to start of bytes data",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // length of bytes",
                "0x0100000000000000000000000000000000000000000000000000000000000000  // bytes data, right padded"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000020"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0100000000000000000000000000000000000000000000000000000000000000"
        ),

        [12] = new(
            "Simple dynamic uint8 array",
            "function foo(uint8[])",
            AbiKeyValues.Create("0", new byte[] { 1, 2 }),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000020  // offset to start of array data",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // length of array",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // element 0",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // element 1"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000020"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000002"
        ),

        [13] = new(
            "Dynamic array of static uint8 arrays",
            "function foo(uint8[2][])",
            AbiKeyValues.Create("0", new byte[][] { new byte[] { 1, 2 }, new byte[] { 3, 4 } }),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000020  // offset to start of array data",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // length of outer array",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // first fixed array: element 0",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // first fixed array: element 1",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // second fixed array: element 0",
                "0x0000000000000000000000000000000000000000000000000000000000000004  // second fixed array: element 1"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000020"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "0000000000000000000000000000000000000000000000000000000000000004"
        ),

        [14] = new(
            "Dynamic array of dynamic uint8 arrays",
            "function foo(uint8[][])",
            AbiKeyValues.Create("0", new byte[][] { new byte[] { 1, 2 }, new byte[] { 3, 4 } }),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000020  // offset to start of array of arrays",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // length of outer array",
                "0x0000000000000000000000000000000000000000000000000000000000000040  // offset to first inner array",
                "0x00000000000000000000000000000000000000000000000000000000000000a0  // offset to second inner array",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // length of first inner array",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // first array: element 0",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // first array: element 1",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // length of second inner array",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // second array: element 0",
                "0x0000000000000000000000000000000000000000000000000000000000000004  // second array: element 1"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000020"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000040"
            + "00000000000000000000000000000000000000000000000000000000000000a0"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "0000000000000000000000000000000000000000000000000000000000000004"
        ),

        [15] = new(
            "Dynamic string in tuple with bool",
            "function foo(bool isActive, (string id, uint256 balance) account)",
            AbiKeyValues.Create("isActive", true, "account", AbiKeyValues.Create("id", "abc", "balance", (BigInteger)9u)),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // isActive true",
                "0x0000000000000000000000000000000000000000000000000000000000000040  // offset to dynamic tuple 'account'",
                "0x0000000000000000000000000000000000000000000000000000000000000040  // (dyn) offset to dynamic string",
                "0x0000000000000000000000000000000000000000000000000000000000000009  // (dyn) uint256 value (9)",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // (dyn) string length",
                "0x6162630000000000000000000000000000000000000000000000000000000000  // (dyn) string data ('abc' padded)"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000040"
            + "0000000000000000000000000000000000000000000000000000000000000040"
            + "0000000000000000000000000000000000000000000000000000000000000009"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "6162630000000000000000000000000000000000000000000000000000000000"
        ),

        [16] = new(
            "Nested tuple with two dynamic strings",
            "function foo(bool isActive, ((string id, string name) user, uint256 balance) account)",
            AbiKeyValues.Create("isActive", true, "account", AbiKeyValues.Create("user", AbiKeyValues.Create("id", "a", "name", "abc"), "balance", (BigInteger)9u)),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000001  // isActive true",
                "0x0000000000000000000000000000000000000000000000000000000000000040  // offset to outer dynamic tuple 'account'",
                "0x0000000000000000000000000000000000000000000000000000000000000040  // (dyn) outer tuple: offset to inner dynamic tuple 'user'",
                "0x0000000000000000000000000000000000000000000000000000000000000009  // (dyn) uint256 value (9)",
                "0x0000000000000000000000000000000000000000000000000000000000000040  // (dyn) inner tuple: offset to string id",
                "0x0000000000000000000000000000000000000000000000000000000000000080  // (dyn) inner tuple: offset to string name",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) length of string id",
                "0x6100000000000000000000000000000000000000000000000000000000000000  // (dyn) string data ('a' padded)",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // (dyn) length of string name",
                "0x6162630000000000000000000000000000000000000000000000000000000000  // (dyn) string data ('abc' padded)"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000040"
            + "0000000000000000000000000000000000000000000000000000000000000040"
            + "0000000000000000000000000000000000000000000000000000000000000009"
            + "0000000000000000000000000000000000000000000000000000000000000040"
            + "0000000000000000000000000000000000000000000000000000000000000080"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "6100000000000000000000000000000000000000000000000000000000000000"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "6162630000000000000000000000000000000000000000000000000000000000"
        ),

        // Examples from the formal spec

        [17] = new(
            "Formal spec: Fixed array of bytes3",
            "function bar(bytes3[2])",
            AbiKeyValues.Create("0", new string[] { "abc", "def" }),
            new List<string> {
                "0x6162630000000000000000000000000000000000000000000000000000000000  // first bytes3 value ('abc')",
                "0x6465660000000000000000000000000000000000000000000000000000000000  // second bytes3 value ('def')"
            },
            "0x6162630000000000000000000000000000000000000000000000000000000000"
            + "6465660000000000000000000000000000000000000000000000000000000000"
        ),

        [18] = new(
            "Formal spec: Simple uint256 and bool",
            "function baz(uint256 x,bool y)",
            AbiKeyValues.Create("x", (BigInteger)69u, "y", true),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000045  // uint256 value (69)",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // bool value (true)"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000045"
            + "0000000000000000000000000000000000000000000000000000000000000001"
        ),

        [19] = new(
            "Formal spec: Dynamic bytes with bool and uint array",
            "function sam(bytes,bool,uint[])",
            AbiKeyValues.Create("0", "dave", "1", true, "2", new BigInteger[] { 1, 2, 3 }),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000060  // offset to start of bytes data",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // bool value (true)",
                "0x00000000000000000000000000000000000000000000000000000000000000a0  // offset to start of uint[] data",
                "0x0000000000000000000000000000000000000000000000000000000000000004  // (dyn) length of bytes",
                "0x6461766500000000000000000000000000000000000000000000000000000000  // (dyn) bytes data ('dave' padded)",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // (dyn) length of uint array",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) first array element",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) second array element",
                "0x0000000000000000000000000000000000000000000000000000000000000003  // (dyn) third array element"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000060"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "00000000000000000000000000000000000000000000000000000000000000a0"
            + "0000000000000000000000000000000000000000000000000000000000000004"
            + "6461766500000000000000000000000000000000000000000000000000000000"
            + "0000000000000000000000000000000000000000000000000000000000000003"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000003"
        ),

        [20] = new(
            "Formal spec: Mixed static and dynamic types",
            "function foo(uint256,uint32[],bytes10,bytes)",
            AbiKeyValues.Create(("0", new BigInteger(291)), ("1", new uint[] { 0x456u, 0x789u }), ("2", "1234567890"), ("3", "Hello, world!")),
            new List<string> {
                "0x0000000000000000000000000000000000000000000000000000000000000123  // uint256 value (0x123)",
                "0x0000000000000000000000000000000000000000000000000000000000000080  // offset to start of uint32[] data",
                "0x3132333435363738393000000000000000000000000000000000000000000000  // bytes10 value ('1234567890' padded)",
                "0x00000000000000000000000000000000000000000000000000000000000000e0  // offset to start of bytes data",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length of uint32 array",
                "0x0000000000000000000000000000000000000000000000000000000000000456  // (dyn) first array element",
                "0x0000000000000000000000000000000000000000000000000000000000000789  // (dyn) second array element",
                "0x000000000000000000000000000000000000000000000000000000000000000d  // (dyn) length of bytes",
                "0x48656c6c6f2c20776f726c642100000000000000000000000000000000000000  // (dyn) bytes data ('Hello, world!' padded)"
            },
            "0x0000000000000000000000000000000000000000000000000000000000000123"
            + "0000000000000000000000000000000000000000000000000000000000000080"
            + "3132333435363738393000000000000000000000000000000000000000000000"
            + "00000000000000000000000000000000000000000000000000000000000000e0"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000456"
            + "0000000000000000000000000000000000000000000000000000000000000789"
            + "000000000000000000000000000000000000000000000000000000000000000d"
            + "48656c6c6f2c20776f726c642100000000000000000000000000000000000000"
        ),

        [21] = new(
            "Array of tuples for coffee orders",
            "function foo(uint256 orderNumber, (bool isLatte, bool hasMilk, bool hasSugar)[] coffeeOrders)",
            AbiKeyValues.Create(
                "orderNumber", new BigInteger(42),
                "coffeeOrders", new AbiKeyValues[]
                {
                    AbiKeyValues.Create("isLatte", true, "hasMilk", false, "hasSugar", true),
                    AbiKeyValues.Create("isLatte", false, "hasMilk", true, "hasSugar", false)
                }
            ),
            new List<string> {
                "0x000000000000000000000000000000000000000000000000000000000000002a  // uint256 orderNumber (42)",
                "0x0000000000000000000000000000000000000000000000000000000000000040  // offset to start of coffeeOrders array",
                "0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length of coffeeOrders array",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) first order: isLatte (true)",
                "0x0000000000000000000000000000000000000000000000000000000000000000  // (dyn) first order: hasMilk (false)",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) first order: hasSugar (true)",
                "0x0000000000000000000000000000000000000000000000000000000000000000  // (dyn) second order: isLatte (false)",
                "0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) second order: hasMilk (true)",
                "0x0000000000000000000000000000000000000000000000000000000000000000  // (dyn) second order: hasSugar (false)"
            },
            "0x000000000000000000000000000000000000000000000000000000000000002a"
            + "0000000000000000000000000000000000000000000000000000000000000040"
            + "0000000000000000000000000000000000000000000000000000000000000002"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000000"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000000"
            + "0000000000000000000000000000000000000000000000000000000000000001"
            + "0000000000000000000000000000000000000000000000000000000000000000"
        )
    };
}
