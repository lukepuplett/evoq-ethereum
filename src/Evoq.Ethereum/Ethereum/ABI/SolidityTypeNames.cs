namespace Evoq.Ethereum.ABI;

/// <summary>
/// Contains constant names for Solidity types.
/// </summary>
public static class SolidityTypeNames
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
        /// The Solidity unsigned integer type (alias for uint256).
        /// </summary>
        public const string Uint = "uint";

        /// <summary>
        /// The Solidity 256-bit signed integer type.
        /// </summary>
        public const string Int256 = "int256";

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
    public static class ByteArrays
    {
        /// <summary>
        /// The Solidity single-byte type.
        /// </summary>
        public const string Bytes1 = "bytes1";

        /// <summary>
        /// The Solidity 32-byte type, commonly used for hashes and signatures.
        /// </summary>
        public const string Bytes32 = "bytes32";
    }
}