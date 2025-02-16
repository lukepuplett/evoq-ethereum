namespace Evoq.Ethereum.ABI;

/// <summary>
/// Contains constant names for Solidity types.
/// </summary>
public static class AbiTypeNames
{
    /// <summary>
    /// The Solidity address type, representing a 20-byte Ethereum address.
    /// </summary>
    public const string Address = "address";

    /// <summary>
    /// The Solidity boolean type.
    /// </summary>
    public const string Bool = "bool";

    /// <summary>
    /// The Solidity dynamic string type.
    /// </summary>
    public const string String = "string";

    /// <summary>
    /// The Solidity dynamic bytes type.
    /// </summary>
    public const string Bytes = "bytes";

    /// <summary>
    /// The Solidity byte type (alias for bytes1).
    /// </summary>
    public const string Byte = "byte";

    /// <summary>
    /// Contains integer type names.
    /// </summary>
    public static class IntegerTypes
    {
        /// <summary>
        /// The Solidity signed integer type (alias for int256).
        /// </summary>
        public const string Int = "int";

        /// <summary>
        /// The Solidity 8-bit signed integer type.
        /// </summary>
        public const string Int8 = "int8";

        /// <summary>
        /// The Solidity 16-bit signed integer type.
        /// </summary>
        public const string Int16 = "int16";

        /// <summary>
        /// The Solidity 32-bit signed integer type.
        /// </summary>
        public const string Int32 = "int32";

        /// <summary>
        /// The Solidity 64-bit signed integer type.
        /// </summary>
        public const string Int64 = "int64";

        /// <summary>
        /// The Solidity 128-bit signed integer type.
        /// </summary>
        public const string Int128 = "int128";

        /// <summary>
        /// The Solidity 256-bit signed integer type.
        /// </summary>
        public const string Int256 = "int256";

        // unsigned integer types

        /// <summary>
        /// The Solidity unsigned integer type (alias for uint256).
        /// </summary>
        public const string Uint = "uint";

        /// <summary>
        /// The Solidity 8-bit unsigned integer type.
        /// </summary>
        public const string Uint8 = "uint8";

        /// <summary>
        /// The Solidity 16-bit unsigned integer type.
        /// </summary>
        public const string Uint16 = "uint16";

        /// <summary>
        /// The Solidity 32-bit unsigned integer type.
        /// </summary>
        public const string Uint32 = "uint32";

        /// <summary>
        /// The Solidity 64-bit unsigned integer type.
        /// </summary>
        public const string Uint64 = "uint64";

        /// <summary>
        /// The Solidity 128-bit unsigned integer type.
        /// </summary>
        public const string Uint128 = "uint128";

        /// <summary>
        /// The Solidity 256-bit unsigned integer type.
        /// </summary>
        public const string Uint256 = "uint256";
    }

    /// <summary>
    /// Contains fixed-point decimal type names.
    /// </summary>
    public static class FixedTypes
    {
        /// <summary>
        /// The Solidity signed fixed-point type (alias for fixed128x18).
        /// </summary>
        public const string Fixed = "fixed";

        /// <summary>
        /// The Solidity unsigned fixed-point type (alias for ufixed128x18).
        /// </summary>
        public const string Ufixed = "ufixed";

        /// <summary>
        /// The Solidity 128-bit signed fixed-point type with 18 decimals.
        /// </summary>
        public const string Fixed128x18 = "fixed128x18";

        /// <summary>
        /// The Solidity 128-bit unsigned fixed-point type with 18 decimals.
        /// </summary>
        public const string Ufixed128x18 = "ufixed128x18";
    }

    /// <summary>
    /// Contains fixed-size byte array type names.
    /// </summary>
    public static class FixedByteArrays
    {
        /// <summary>
        /// The Solidity single-byte type.
        /// </summary>
        public const string Bytes1 = "bytes1";

        /// <summary>   
        /// The Solidity 2-byte type.
        /// </summary>
        public const string Bytes2 = "bytes2";

        /// <summary>   
        /// The Solidity 3-byte type.
        /// </summary>
        public const string Bytes3 = "bytes3";

        /// <summary>
        /// The Solidity 4-byte type.
        /// </summary>
        public const string Bytes4 = "bytes4";

        /// <summary>
        /// The Solidity 5-byte type.
        /// </summary>
        public const string Bytes5 = "bytes5";

        /// <summary>
        /// The Solidity 32-byte type, commonly used for hashes and signatures.
        /// </summary>
        public const string Bytes32 = "bytes32";

        /// <summary>
        /// The Solidity 6-byte type.
        /// </summary>
        public const string Bytes6 = "bytes6";

        /// <summary>
        /// The Solidity 7-byte type.
        /// </summary>
        public const string Bytes7 = "bytes7";

        /// <summary>
        /// The Solidity 8-byte type.
        /// </summary>
        public const string Bytes8 = "bytes8";

        /// <summary>
        /// The Solidity 9-byte type.
        /// </summary>
        public const string Bytes9 = "bytes9";

        /// <summary>
        /// The Solidity 10-byte type.
        /// </summary>
        public const string Bytes10 = "bytes10";

        /// <summary>
        /// The Solidity 11-byte type.
        /// </summary>
        public const string Bytes11 = "bytes11";

        /// <summary>
        /// The Solidity 12-byte type.
        /// </summary>
        public const string Bytes12 = "bytes12";

        /// <summary>
        /// The Solidity 13-byte type.
        /// </summary>
        public const string Bytes13 = "bytes13";

        /// <summary>
        /// The Solidity 14-byte type.
        /// </summary>
        public const string Bytes14 = "bytes14";

        /// <summary>
        /// The Solidity 15-byte type.
        /// </summary>
        public const string Bytes15 = "bytes15";

        /// <summary>
        /// The Solidity 16-byte type.
        /// </summary>
        public const string Bytes16 = "bytes16";

        /// <summary>
        /// The Solidity 17-byte type.
        /// </summary>
        public const string Bytes17 = "bytes17";

        /// <summary>
        /// The Solidity 18-byte type.
        /// </summary>
        public const string Bytes18 = "bytes18";

        /// <summary>
        /// The Solidity 19-byte type.
        /// </summary>
        public const string Bytes19 = "bytes19";

        /// <summary>
        /// The Solidity 20-byte type.
        /// </summary>
        public const string Bytes20 = "bytes20";

        /// <summary>
        /// The Solidity 21-byte type.
        /// </summary>
        public const string Bytes21 = "bytes21";

        /// <summary>
        /// The Solidity 22-byte type.
        /// </summary>
        public const string Bytes22 = "bytes22";

        /// <summary>
        /// The Solidity 23-byte type.
        /// </summary>
        public const string Bytes23 = "bytes23";

        /// <summary>
        /// The Solidity 24-byte type.
        /// </summary>
        public const string Bytes24 = "bytes24";

        /// <summary>
        /// The Solidity 25-byte type.
        /// </summary>
        public const string Bytes25 = "bytes25";

        /// <summary>
        /// The Solidity 26-byte type.
        /// </summary>
        public const string Bytes26 = "bytes26";

        /// <summary>
        /// The Solidity 27-byte type.
        /// </summary>
        public const string Bytes27 = "bytes27";

        /// <summary>
        /// The Solidity 28-byte type.
        /// </summary>
        public const string Bytes28 = "bytes28";

        /// <summary>
        /// The Solidity 29-byte type.
        /// </summary>
        public const string Bytes29 = "bytes29";

        /// <summary>
        /// The Solidity 30-byte type.
        /// </summary>
        public const string Bytes30 = "bytes30";

        /// <summary>
        /// The Solidity 31-byte type.
        /// </summary>
        public const string Bytes31 = "bytes31";
    }
}