using System.Numerics;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.Conversion;


// Simple POCO with basic types
public class SimpleUser
{
    public string? Name { get; set; }
    public BigInteger Age { get; set; }
    public bool IsActive { get; set; }
}

// POCO with an Ethereum address
public class UserWithAddress
{
    public string? Name { get; set; }
    public EthereumAddress WalletAddress { get; set; }
}

// POCO with array properties
public class UserWithArrays
{
    public string? Name { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public BigInteger[] Scores { get; set; } = Array.Empty<BigInteger>();
}

// POCO with nested object
public class UserWithNestedObject
{
    public string? Name { get; set; }
    public UserProfile Profile { get; set; } = new();
}

public class UserProfile
{
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}

// POCO with array of objects
public class UserWithFriends
{
    public string? Name { get; set; }
    public SimpleUser[] Friends { get; set; } = Array.Empty<SimpleUser>();
}

// POCO with numeric property names (for positional mapping)
public class PositionalUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public BigInteger Age { get; set; }
}

// POCO with custom attribute mapping
public class AttributeMappedUser
{
    [AbiParameter("username")]
    public string? Name { get; set; }

    [AbiParameter("years")]
    public BigInteger Age { get; set; }

    [AbiParameter("wallet")]
    public EthereumAddress Address { get; set; }
}

// POCO with tuple structure
public class UserWithTuple
{
    public string? Name { get; set; }
    public (string City, string Country) Location { get; set; }
}

// Ethereum Token Balance POCO
public class TokenBalance
{
    public EthereumAddress TokenAddress { get; set; }
    public string? Symbol { get; set; }
    public BigInteger Balance { get; set; }
    public byte Decimals { get; set; }
}

// Ethereum ERC721 Token POCO
public class ERC721Token
{
    public EthereumAddress ContractAddress { get; set; }
    public BigInteger TokenId { get; set; }
    public EthereumAddress Owner { get; set; }
    public string? TokenURI { get; set; }
}

// Ethereum Attestation POCO (based on EAS schema)
public class Attestation
{
    public Hex Uid { get; set; }
    public Hex Schema { get; set; }
    public BigInteger Time { get; set; }
    public BigInteger ExpirationTime { get; set; }
    public BigInteger RevocationTime { get; set; }
    public Hex RefUID { get; set; }
    public EthereumAddress Recipient { get; set; }
    public EthereumAddress Attester { get; set; }
    public bool Revocable { get; set; }
    public Hex Data { get; set; }
}

// POCO with nullable properties
public class NullableUser
{
    public string? Name { get; set; }
    public BigInteger? Age { get; set; }
    public bool? IsActive { get; set; }
    public EthereumAddress? WalletAddress { get; set; }
}

// Test class with BigInteger array
public class UserWithBigIntegerArray
{
    public string? Name { get; set; }
    public BigInteger[]? Scores { get; set; }
}

// Test class with complex attribute mapping
public class ComplexAttributeMappedUser
{
    [AbiParameter("customName", Position = 0)]
    public string? Name { get; set; }

    [AbiParameter("years", Position = 1, AbiType = "uint256")]
    public BigInteger Age { get; set; }

    [AbiParameter("active", Position = 2)]
    public bool IsActive { get; set; }

    [AbiParameter("wallet", AbiType = "address")]
    public EthereumAddress Address { get; set; }

    [AbiParameter("ignoredProperty", Ignore = true)]
    public string? IgnoredProperty { get; set; }

    // No attribute - should not be mapped by attribute
    public string? UnmappedProperty { get; set; }
}

// Record struct for testing
public record struct TokenInfo(
    EthereumAddress ContractAddress,
    string Symbol,
    byte Decimals,
    BigInteger TotalSupply);

// Record struct with nullable properties
public record struct UserStats(
    string Username,
    BigInteger? Reputation,
    int TransactionCount,
    bool IsVerified);