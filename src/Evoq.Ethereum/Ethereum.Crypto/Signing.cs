using System;
using Evoq.Ethereum.Transactions;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

/// <summary>
/// Static class for signing operations.
/// </summary>
internal static class Signing
{
    /// <summary>
    /// Compares two BigInteger instances for equality using CompareTo.
    /// </summary>
    /// <param name="a">The first BigInteger.</param>
    /// <param name="b">The second BigInteger.</param>
    /// <returns>True if the values are equal, false otherwise.</returns>
    /// <remarks>
    /// **DO NOT REMOVE OR CHANGE THIS METHOD TO USE BigInteger.Equals(BigInteger) DIRECTLY!**
    /// 
    /// This method exists solely to work around a frustrating BouncyCastle versioning conflict
    /// that can occur in consuming applications. Specifically:
    /// 
    /// 1.  **Dependency Hell:** This library (`Evoq.Ethereum`) depends on a newer BouncyCastle
    ///     package (`BouncyCastle.NetCore`, e.g., 2.2.1) which provides the convenient
    ///     `Org.BouncyCastle.Math.BigInteger.Equals(BigInteger)` overload.
    /// 2.  **Transitive Nightmares:** A consuming application might *also* depend on another library
    ///     (e.g., Yoti.Client) which, in turn, depends on an *older*, incompatible BouncyCastle library
    ///     (`Portable.BouncyCastle` or the base `BouncyCastle.Crypto`, e.g., 1.8.5.0).
    ///     This older version *lacks* the `BigInteger.Equals(BigInteger)` overload.
    /// 3.  **.NET Runtime Woes:** Despite best efforts using standard .NET mechanisms like assembly
    ///     binding redirects (`runtimeconfig.template.json`, `app.config`) or even custom
    ///     `AssemblyResolve` event handlers, the .NET runtime can stubbornly fail to load the
    ///     *newer* BouncyCastle version or correctly redirect the method call. It often insists
    ///     on loading the older version pulled in by the transitive dependency, leading to a
    ///     `System.MissingMethodException` at runtime when code in *this* library calls the
    ///     non-existent `Equals(BigInteger)` method.
    /// 4.  **The Workaround:** Since forcing the runtime to behave is unreliable across different
    ///     environments, the only robust solution is to avoid the problematic method call entirely.
    ///     `BigInteger.CompareTo(BigInteger)` *is* present in both old and new versions and returns 0
    ///     for logically equal values. This method encapsulates that `CompareTo(b) == 0` check,
    ///     ensuring compatibility regardless of which BouncyCastle version the runtime decides to load.
    /// 
    /// Yes, this is ugly. Yes, binding redirects *should* work. But welcome to reality.
    /// Using this helper method prevents runtime crashes in environments afflicted by this
    /// dependency mess.
    /// </remarks>
    internal static bool Equals(BigInteger a, BigInteger b)
    {
        // Handle null cases defensively, although BigInteger is a struct in some versions
        // and a class in others (like BouncyCastle.Math).
        if (ReferenceEquals(a, b)) // Same instance or both null
        {
            return true;
        }
        if (a is null || b is null)
        {
            return false;
        }

        return a.CompareTo(b) == 0;
    }

    /// <summary>
    /// Calculates the EIP-155 V value for a given chain ID and recovery ID.
    /// </summary>
    /// <param name="chainId">The chain ID of the network.</param>
    /// <param name="recoveryId">The recovery ID (0 or 1).</param>
    /// <returns>The EIP-155 V value.</returns>
    /// <remarks>
    /// This method implements the EIP-155 formula:
    /// V = recovery_id + chain_id * 2 + 35
    /// 
    /// For example, on Ethereum mainnet (chain_id = 1):
    /// - If recovery_id = 0: V = 0 + 1*2 + 35 = 37
    /// - If recovery_id = 1: V = 1 + 1*2 + 35 = 38
    /// </remarks>
    public static BigInteger CalculateEIP155V(BigInteger chainId, byte recoveryId)
    {
        return new BigInteger(recoveryId.ToString())
            .Add(chainId.Multiply(Constants.Eip155ChainIdMultiplier2))
            .Add(Constants.Eip155BaseValue35);
    }

    /// <summary>
    /// Extracts the recovery ID from a V value, handling both legacy and EIP-155 formats.
    /// </summary>
    /// <param name="v">The V value.</param>
    /// <param name="chainId">The chain ID of the network.</param>
    /// <returns>The recovery ID (0 or 1).</returns>
    /// <remarks>
    /// This method handles both transaction formats:
    /// 
    /// 1. Legacy Transactions (pre-EIP-155):
    ///    - V = 27 + recovery_id
    ///    - Where recovery_id is 0 or 1
    ///    - So V is either 27 or 28
    /// 
    /// 2. EIP-155 Transactions (replay protection):
    ///    - V = recovery_id + chain_id * 2 + 35
    ///    - Where recovery_id is 0 or 1
    ///    - For Ethereum mainnet (chain_id = 1), V is either 37 or 38
    /// 
    /// The method determines which format to use based on the chainId and V value.
    /// </remarks>
    public static byte ExtractRecoveryId(BigInteger v, BigInteger chainId)
    {
        // EIP-155: v = recoveryId + chainId * 2 + 35
        // Pre-EIP-155: v = recoveryId + 27

        // Calculate the base value for EIP-155
        var eip155Base = Constants.Eip155BaseValue35.Add(chainId.Multiply(Constants.Eip155ChainIdMultiplier2));

        // Check if this is an EIP-155 transaction
        // Only apply EIP-155 logic if chainId > 0 and V >= eip155Base
        if (chainId.CompareTo(BigInteger.Zero) > 0 && v.CompareTo(eip155Base) >= 0)
        {
            return (byte)v.Subtract(eip155Base).IntValue;
        }

        // Otherwise, treat as legacy transaction
        return (byte)v.Subtract(Constants.LegacyBaseValue27).IntValue;
    }

    /// <summary>
    /// Extracts the recovery ID from a V value for personal-sign messages.
    /// </summary>
    /// <param name="v">The V value.</param>
    /// <returns>The recovery ID (0 or 1).</returns>
    public static byte GetRecoveryId(BigInteger v) => VToYParity(v);

    /// <summary>
    /// Converts a V value to the corresponding y-parity bit (0 or 1).
    /// </summary>
    /// <param name="v">The V value.</param>
    /// <returns>The y-parity bit (0 or 1).</returns>
    /// <remarks>
    /// The y-parity bit is used to determine which of the two possible public keys
    /// should be recovered from the signature. In Ethereum, this information is encoded
    /// in the V value of the signature, but the encoding has evolved over time:
    /// 
    /// 1. Legacy Transactions (pre-EIP-155):
    ///    - V = 27 + recovery_id
    ///    - Where recovery_id is 0 or 1
    ///    - So V is either 27 or 28
    ///    - The value 27 was chosen historically to avoid confusion with other signing schemes
    ///      that used values below 27
    /// 
    /// 2. EIP-155 Transactions (replay protection):
    ///    - V = recovery_id + chain_id * 2 + 35
    ///    - Where recovery_id is 0 or 1
    ///    - For Ethereum mainnet (chain_id = 1), V is either 37 or 38
    ///    - The formula ensures that V values are unique across different chains
    ///    - The value 35 = 27 + 8 was chosen to maintain compatibility with pre-EIP-155
    ///      while allowing for chain_id values up to 2^30
    /// 
    /// 3. EIP-1559 Transactions:
    ///    - Uses a separate y-parity field (0 or 1) instead of encoding it in V
    ///    - This method helps convert from legacy V values to the y-parity bit
    /// 
    /// The y-parity bit itself relates to the elliptic curve point recovery:
    /// - For secp256k1 (Ethereum's curve), each x-coordinate has two possible y-coordinates
    /// - The y-parity bit tells us which of these two points was used in signing
    /// - 0 means the y-coordinate is even
    /// - 1 means the y-coordinate is odd
    /// </remarks>
    public static byte VToYParity(BigInteger v)
    {
        // Case 1: Legacy transaction format (pre-EIP-155)
        // V = 27 + recovery_id
        // If V is 27, recovery_id is 0
        // If V is 28, recovery_id is 1
        if (Equals(v, Constants.LegacyVZero27))
        {
            // V = 27 means recovery_id = 0
            // In EIP-1559, this corresponds to y-parity = 0
            return Constants.EIP1559_Y_PARITY_EVEN_0;
        }
        if (Equals(v, Constants.LegacyVOne28))
        {
            // V = 28 means recovery_id = 1
            // In EIP-1559, this corresponds to y-parity = 1
            return Constants.EIP1559_Y_PARITY_ODD_1;
        }

        // Case 2: EIP-155 transaction format (with replay protection)
        // V = recovery_id + chain_id * 2 + 35
        // For example, on Ethereum mainnet (chain_id = 1):
        //   If recovery_id = 0, V = 0 + 1*2 + 35 = 37
        //   If recovery_id = 1, V = 1 + 1*2 + 35 = 38

        // To extract the recovery_id from V:
        // 1. Check if V is odd or even (using the least significant bit)
        // 2. If V is odd (e.g., 37, 39, 41...), recovery_id = 0
        // 3. If V is even (e.g., 38, 40, 42...), recovery_id = 1

        // TestBit(0) checks the least significant bit:
        // - If it's 1, the number is odd
        // - If it's 0, the number is even
        bool isOdd = v.TestBit(0);

        // For EIP-155, the mapping is:
        // - Odd V value → recovery_id = 0 → y-parity = 0
        // - Even V value → recovery_id = 1 → y-parity = 1
        return isOdd ? Constants.EIP1559_Y_PARITY_EVEN_0 : Constants.EIP1559_Y_PARITY_ODD_1;
    }

    /// <summary>
    /// Converts a y-parity bit to the corresponding legacy V value.
    /// </summary>
    /// <param name="yParity">The y-parity bit (0 or 1).</param>
    /// <returns>The legacy V value (27 or 28).</returns>
    /// <remarks>
    /// This method converts from the EIP-1559 y-parity format back to the legacy V format:
    /// - y-parity = 0 → V = 27
    /// - y-parity = 1 → V = 28
    /// </remarks>
    public static BigInteger YParityToLegacyV(byte yParity)
    {
        return yParity == 0 ? Constants.LegacyVZero27 : Constants.LegacyVOne28;
    }

    /// <summary>
    /// Converts a y-parity bit to the corresponding EIP-155 V value for a given chain ID.
    /// </summary>
    /// <param name="yParity">The y-parity bit (0 or 1).</param>
    /// <param name="chainId">The chain ID of the network.</param>
    /// <returns>The EIP-155 V value.</returns>
    /// <remarks>
    /// This method converts from the EIP-1559 y-parity format to the EIP-155 V format:
    /// - y-parity = 0 → recovery_id = 0 → V = 0 + chain_id * 2 + 35
    /// - y-parity = 1 → recovery_id = 1 → V = 1 + chain_id * 2 + 35
    /// 
    /// For example, on Ethereum mainnet (chain_id = 1):
    /// - y-parity = 0 → V = 0 + 1*2 + 35 = 37
    /// - y-parity = 1 → V = 1 + 1*2 + 35 = 38
    /// </remarks>
    public static BigInteger YParityToEIP155V(byte yParity, BigInteger chainId)
    {
        return CalculateEIP155V(chainId, yParity);
    }

    /// <summary>
    /// Gets the appropriate y-parity value based on the transaction features.
    /// </summary>
    /// <param name="v">The V value from the signature.</param>
    /// <param name="flags">The transaction features.</param>
    /// <returns>The y-parity value (0 or 1) appropriate for the transaction type.</returns>
    /// <remarks>
    /// This method handles different transaction types:
    /// 
    /// 1. For EIP-1559 transactions (TypedTransaction + FeeMarket):
    ///    - Direct y-parity values (0 or 1) are returned as-is
    ///    - Legacy V values (27 or 28) are converted to y-parity (0 or 1)
    ///    - EIP-155 V values are converted based on their parity
    /// 
    /// 2. For legacy transactions:
    ///    - Odd V values map to y-parity=1
    ///    - Even V values map to y-parity=0
    /// </remarks>
    public static byte GetYParityForFeatures(BigInteger v, TransactionFeatures flags)
    {
        // For EIP-1559 transactions, we need the y-parity bit (0 or 1)
        if (flags.HasFlag(TransactionFeatures.TypedTransaction) &&
            flags.HasFlag(TransactionFeatures.FeeMarket))
        {
            // For EIP-1559, we need to convert from V to y-parity
            // V=27 -> y-parity=0, V=28 -> y-parity=1
            // V=0 -> y-parity=0, V=1 -> y-parity=1

            // Check for direct y-parity values (0 or 1)
            if (Equals(v, BigInteger.Zero) || Equals(v, BigInteger.One))
            {
                return (byte)v.IntValue;
            }

            // Check for legacy V values (27 or 28)
            if (Equals(v, Constants.LegacyVZero27))
            {
                return Constants.EIP1559_Y_PARITY_EVEN_0;
            }
            if (Equals(v, Constants.LegacyVOne28))
            {
                return Constants.EIP1559_Y_PARITY_ODD_1;
            }

            // For EIP-155 V values, extract the recovery ID
            // V = recovery_id + chain_id * 2 + 35
            // recovery_id is either 0 or 1
            // If V is odd, recovery_id is 0, y-parity is 0
            // If V is even, recovery_id is 1, y-parity is 1
            return v.TestBit(0) ? Constants.EIP1559_Y_PARITY_EVEN_0 : Constants.EIP1559_Y_PARITY_ODD_1;
        }

        // For legacy transactions, we use a different logic
        // For legacy transactions, odd V means y-parity=1, even V means y-parity=0
        return v.TestBit(0) ? Constants.EIP1559_Y_PARITY_ODD_1 : Constants.EIP1559_Y_PARITY_EVEN_0;
    }

    /// <summary>
    /// Determines whether a signature has EIP-155 replay protection for a specific chain ID.
    /// </summary>
    /// <param name="v">The V value from the signature.</param>
    /// <param name="chainId">The chain ID to check against.</param>
    /// <returns>True if the signature has EIP-155 replay protection for the specified chain ID; otherwise, false.</returns>
    /// <remarks>
    /// EIP-155 introduced replay protection by incorporating the chain ID into the V value:
    /// V = recovery_id + chain_id * 2 + 35
    /// 
    /// This method checks if the V value matches the expected values for the given chain ID.
    /// For example, on Ethereum mainnet (chain_id = 1), valid EIP-155 V values are 37 and 38.
    /// </remarks>
    public static bool HasEIP155ReplayProtection(BigInteger v, BigInteger chainId)
    {
        // Calculate the expected V values for this chainId
        // EIP-155: v = recovery_id + chainId * 2 + 35
        var baseV = Constants.Eip155BaseValue35.Add(chainId.Multiply(Constants.Eip155ChainIdMultiplier2));

        // For chainId = 1, this would be 37 and 38
        var v0 = baseV;                      // recovery_id = 0
        var v1 = baseV.Add(BigInteger.One);  // recovery_id = 1

        // Check if V matches either of the expected values
        return Equals(v, v0) || Equals(v, v1);
    }
}
