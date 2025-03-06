using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Contains constants related to Ethereum transaction signatures and their encoding.
/// </summary>
/// <remarks>
/// Ethereum uses a specific encoding scheme for transaction signatures, particularly
/// for the V value which has evolved through several Ethereum Improvement Proposals (EIPs).
/// 
/// The V value serves multiple purposes:
/// 1. It encodes the recovery ID (0 or 1) needed to recover the public key from the signature
/// 2. In EIP-155, it also encodes the chain ID to prevent replay attacks across different networks
/// 
/// The constants in this class represent the special values and formulas used in these encoding schemes.
/// </remarks>
public static class Constants
{
    // Primitive type constants for common values

    /// <summary>Legacy base value (27) for V in pre-EIP-155 transactions.</summary>
    public const byte LEGACY_BASE_VALUE_27 = 27;

    /// <summary>Legacy V value (27) for recovery ID 0.</summary>
    public const byte LEGACY_V_ZERO_27 = 27;

    /// <summary>Legacy V value (28) for recovery ID 1.</summary>
    public const byte LEGACY_V_ONE_28 = 28;

    /// <summary>Offset (8) added to legacy base to create EIP-155 base value.</summary>
    public const byte EIP155_OFFSET_8 = 8;

    /// <summary>Base value (35) used in EIP-155 signatures.</summary>
    public const byte EIP155_BASE_VALUE_35 = 35;

    /// <summary>Multiplier (2) for chain ID in EIP-155 formula.</summary>
    public const byte EIP155_CHAIN_ID_MULTIPLIER_2 = 2;

    /// <summary>V value (37) for Ethereum mainnet with recovery ID 0.</summary>
    public const byte MAINNET_EIP155_V_ZERO_37 = 37;

    /// <summary>V value (38) for Ethereum mainnet with recovery ID 1.</summary>
    public const byte MAINNET_EIP155_V_ONE_38 = 38;

    /// <summary>Y-parity value (0) for even y-coordinates in EIP-1559.</summary>
    public const byte EIP1559_Y_PARITY_EVEN_0 = 0;

    /// <summary>Y-parity value (1) for odd y-coordinates in EIP-1559.</summary>
    public const byte EIP1559_Y_PARITY_ODD_1 = 1;

    // BigInteger versions of the constants for use with BouncyCastle

    /// <summary>
    /// The base value (27) used in legacy Ethereum signatures.
    /// </summary>
    /// <remarks>
    /// In the original Ethereum implementation (pre-EIP-155), the V value was calculated as:
    /// V = LegacyBaseValue27 + recovery_id
    /// 
    /// Where recovery_id is either 0 or 1, resulting in V values of 27 or 28.
    /// 
    /// The value 27 was chosen historically to avoid confusion with other signing schemes
    /// that used values below 27. This convention was established in the Ethereum Yellow Paper
    /// and became part of the protocol specification.
    /// </remarks>
    public static readonly BigInteger LegacyBaseValue27 = new BigInteger(LEGACY_BASE_VALUE_27.ToString());

    /// <summary>
    /// The V value (27) for legacy transactions with recovery ID 0.
    /// </summary>
    /// <remarks>
    /// This is the V value for a legacy transaction (pre-EIP-155) where the recovery ID is 0.
    /// In ECDSA signature recovery, this indicates that the y-coordinate of the public key point is even.
    /// 
    /// When recovering an address from a signature with V=27, the recovery algorithm will
    /// select the public key variant with an even y-coordinate.
    /// </remarks>
    public static readonly BigInteger LegacyVZero27 = new BigInteger(LEGACY_V_ZERO_27.ToString());

    /// <summary>
    /// The V value (28) for legacy transactions with recovery ID 1.
    /// </summary>
    /// <remarks>
    /// This is the V value for a legacy transaction (pre-EIP-155) where the recovery ID is 1.
    /// In ECDSA signature recovery, this indicates that the y-coordinate of the public key point is odd.
    /// 
    /// When recovering an address from a signature with V=28, the recovery algorithm will
    /// select the public key variant with an odd y-coordinate.
    /// </remarks>
    public static readonly BigInteger LegacyVOne28 = new BigInteger(LEGACY_V_ONE_28.ToString());

    /// <summary>
    /// The offset (8) added to the legacy base value to create the EIP-155 base value.
    /// </summary>
    /// <remarks>
    /// When EIP-155 was introduced to add replay protection, a new formula for V was defined:
    /// V = recovery_id + chain_id * 2 + Eip155BaseValue35
    /// 
    /// The Eip155BaseValue35 is calculated as LegacyBaseValue27 + Eip155Offset8 = 27 + 8 = 35.
    /// 
    /// The offset of 8 was chosen to ensure that:
    /// 1. EIP-155 V values would not conflict with legacy V values (27 and 28)
    /// 2. It would allow for chain IDs up to 2^30 without collision
    /// 3. The math would work out cleanly for various chain ID values
    /// </remarks>
    public static readonly BigInteger Eip155Offset8 = new BigInteger(EIP155_OFFSET_8.ToString());

    /// <summary>
    /// The base value (35) used in EIP-155 signatures.
    /// </summary>
    /// <remarks>
    /// In EIP-155 transactions, the V value is calculated as:
    /// V = recovery_id + chain_id * 2 + Eip155BaseValue35
    /// 
    /// Where Eip155BaseValue35 = LegacyBaseValue27 + Eip155Offset8 = 27 + 8 = 35.
    /// 
    /// This formula ensures that:
    /// 1. Each network (identified by chain_id) has its own unique range of V values
    /// 2. The recovery ID (0 or 1) can still be extracted from the V value
    /// 3. Transactions cannot be replayed across different networks
    /// 
    /// For example, on Ethereum mainnet (chain_id = 1):
    /// - If recovery_id = 0: V = 0 + 1*2 + 35 = 37
    /// - If recovery_id = 1: V = 1 + 1*2 + 35 = 38
    /// </remarks>
    public static readonly BigInteger Eip155BaseValue35 = new BigInteger(EIP155_BASE_VALUE_35.ToString());

    /// <summary>
    /// The multiplier (2) used in the EIP-155 formula.
    /// </summary>
    /// <remarks>
    /// In the EIP-155 formula: V = recovery_id + chain_id * Eip155ChainIdMultiplier2 + Eip155BaseValue35
    /// 
    /// The multiplier of 2 ensures that:
    /// 1. There's enough space between V values for different chain IDs
    /// 2. The recovery ID (0 or 1) can be extracted by checking if V is odd or even
    /// 
    /// This works because:
    /// - chain_id * 2 is always even (regardless of whether chain_id is odd or even)
    /// - Eip155BaseValue35 (35) is odd
    /// - So V = recovery_id + chain_id*2 + 35 will be:
    ///   - Odd when recovery_id = 0 (regardless of chain_id)
    ///   - Even when recovery_id = 1 (regardless of chain_id)
    /// </remarks>
    public static readonly BigInteger Eip155ChainIdMultiplier2 = new BigInteger(EIP155_CHAIN_ID_MULTIPLIER_2.ToString());

    /// <summary>
    /// The V value (37) for Ethereum mainnet (chain_id = 1) with recovery ID 0.
    /// </summary>
    /// <remarks>
    /// This is the V value for an EIP-155 transaction on Ethereum mainnet (chain_id = 1)
    /// where the recovery ID is 0.
    /// 
    /// Calculated as: V = recovery_id (0) + chain_id (1) * 2 + Eip155BaseValue35 (35) = 37
    /// 
    /// When recovering an address from a signature with V=37 on Ethereum mainnet,
    /// the recovery algorithm will select the public key variant with an even y-coordinate.
    /// </remarks>
    public static readonly BigInteger MainnetEip155VZero37 = new BigInteger(MAINNET_EIP155_V_ZERO_37.ToString());

    /// <summary>
    /// The V value (38) for Ethereum mainnet (chain_id = 1) with recovery ID 1.
    /// </summary>
    /// <remarks>
    /// This is the V value for an EIP-155 transaction on Ethereum mainnet (chain_id = 1)
    /// where the recovery ID is 1.
    /// 
    /// Calculated as: V = recovery_id (1) + chain_id (1) * 2 + Eip155BaseValue35 (35) = 38
    /// 
    /// When recovering an address from a signature with V=38 on Ethereum mainnet,
    /// the recovery algorithm will select the public key variant with an odd y-coordinate.
    /// </remarks>
    public static readonly BigInteger MainnetEip155VOne38 = new BigInteger(MAINNET_EIP155_V_ONE_38.ToString());
}