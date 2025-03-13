using System.Numerics;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.Conversion;

[TestClass]
public class AbiConverterTests
{
    private AbiConverter converter = new();

    //

    [TestMethod]
    public void SimpleUser_ConvertFromDictionary_Success()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "Name", "John Doe" },
            { "Age", BigInteger.Parse("25") },
            { "IsActive", true }
        };

        // Act
        // This is a placeholder for the actual conversion method we'll implement
        var user = this.converter.DictionaryToObject<SimpleUser>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("John Doe", user.Name);
        Assert.AreEqual(BigInteger.Parse("25"), user.Age);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void UserWithAddress_ConvertFromDictionary_Success()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "Name", "Alice" },
            { "WalletAddress", "0x1234567890123456789012345678901234567890" }
        };

        // Act
        var user = this.converter.DictionaryToObject<UserWithAddress>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice", user.Name);
        Assert.IsTrue(string.Equals(
            "0x1234567890123456789012345678901234567890",
            user.WalletAddress.ToString(),
            StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void UserWithArrays_ConvertFromDictionary_Success()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "Name", "Bob" },
            { "Tags", new[] { "developer", "blockchain", "ethereum" } },
            { "Scores", new[] { BigInteger.Parse("95"), BigInteger.Parse("87"), BigInteger.Parse("92") } }
        };

        // Act
        var user = this.converter.DictionaryToObject<UserWithArrays>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Bob", user.Name);
        CollectionAssert.AreEqual(new[] { "developer", "blockchain", "ethereum" }, user.Tags);
        Assert.AreEqual(3, user.Scores.Length);
        Assert.AreEqual(BigInteger.Parse("95"), user.Scores[0]);
        Assert.AreEqual(BigInteger.Parse("87"), user.Scores[1]);
        Assert.AreEqual(BigInteger.Parse("92"), user.Scores[2]);
    }

    [TestMethod]
    public void UserWithNestedObject_ConvertFromDictionary_Success()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "Name", "Charlie" },
            { "Profile", new Dictionary<string, object?>
                {
                    { "Bio", "Blockchain enthusiast" },
                    { "AvatarUrl", "https://example.com/avatar.jpg" }
                }
            }
        };

        // Act
        var user = this.converter.DictionaryToObject<UserWithNestedObject>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Charlie", user.Name, "Name should match");
        Assert.IsNotNull(user.Profile, "Profile should not be null");
        Assert.AreEqual("Blockchain enthusiast", user.Profile.Bio, "Bio should match");
        Assert.AreEqual("https://example.com/avatar.jpg", user.Profile.AvatarUrl, "AvatarUrl should match");
    }

    [TestMethod]
    public void UserWithFriends_ConvertFromDictionary_Success()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "Name", "Dave" },
            { "Friends", new[]
                {
                    new Dictionary<string, object?>
                    {
                        { "Name", "Friend1" },
                        { "Age", BigInteger.Parse("30") },
                        { "IsActive", true }
                    },
                    new Dictionary<string, object?>
                    {
                        { "Name", "Friend2" },
                        { "Age", BigInteger.Parse("28") },
                        { "IsActive", false }
                    }
                }
            }
        };

        // Act
        var user = this.converter.DictionaryToObject<UserWithFriends>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Dave", user.Name);
        Assert.IsNotNull(user.Friends);
        Assert.AreEqual(2, user.Friends.Length);

        Assert.AreEqual("Friend1", user.Friends[0].Name);
        Assert.AreEqual(BigInteger.Parse("30"), user.Friends[0].Age);
        Assert.IsTrue(user.Friends[0].IsActive);

        Assert.AreEqual("Friend2", user.Friends[1].Name);
        Assert.AreEqual(BigInteger.Parse("28"), user.Friends[1].Age);
        Assert.IsFalse(user.Friends[1].IsActive);
    }

    [TestMethod]
    public void PositionalUser_ConvertFromDictionary_Success()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "0", "John" },
            { "1", "Doe" },
            { "2", BigInteger.Parse("35") }
        };

        // Act
        var user = this.converter.DictionaryToObject<PositionalUser>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("John", user.FirstName);
        Assert.AreEqual("Doe", user.LastName);
        Assert.AreEqual(BigInteger.Parse("35"), user.Age);
    }

    [TestMethod]
    public void AttributeMappedUser_ConvertFromDictionary_Success()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "username", "JaneDoe" },
            { "years", BigInteger.Parse("27") },
            { "wallet", "0xabcdef1234567890abcdef1234567890abcdef12" }
        };

        // Act
        var user = this.converter.DictionaryToObject<AttributeMappedUser>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("JaneDoe", user.Name);
        Assert.AreEqual(BigInteger.Parse("27"), user.Age);
        Assert.IsTrue(string.Equals(
            "0xabcdef1234567890abcdef1234567890abcdef12",
            user.Address.ToString(),
            StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TokenBalance_ConvertFromDictionary_Success()
    {
        // Arrange - Simulating ERC20 token balance response
        var dictionary = new Dictionary<string, object?>
        {
            { "TokenAddress", "0x6b175474e89094c44da98b954eedeac495271d0f" }, // DAI token
            { "Symbol", "DAI" },
            { "Balance", BigInteger.Parse("1000000000000000000") }, // 1 DAI with 18 decimals
            { "Decimals", (byte)18 }
        };

        // Act
        var tokenBalance = this.converter.DictionaryToObject<TokenBalance>(dictionary);

        // Assert
        Assert.IsNotNull(tokenBalance);
        Assert.IsTrue(string.Equals(
            "0x6b175474e89094c44da98b954eedeac495271d0f",
            tokenBalance.TokenAddress.ToString(),
            StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("DAI", tokenBalance.Symbol);
        Assert.AreEqual(BigInteger.Parse("1000000000000000000"), tokenBalance.Balance);
        Assert.AreEqual(18, tokenBalance.Decimals);
    }

    [TestMethod]
    public void ERC721Token_ConvertFromDictionary_Success()
    {
        // Arrange - Simulating NFT token data
        var dictionary = new Dictionary<string, object?>
        {
            { "ContractAddress", "0xbc4ca0eda7647a8ab7c2061c2e118a18a936f13d" }, // BAYC
            { "TokenId", BigInteger.Parse("1234") },
            { "Owner", "0x1234567890123456789012345678901234567890" },
            { "TokenURI", "ipfs://QmeSjSinHpPnmXmspMjwiXyN6zS4E9zccariGR3jxcaWtq/1234" }
        };

        // Act
        var nft = this.converter.DictionaryToObject<ERC721Token>(dictionary);

        // Assert
        Assert.IsNotNull(nft);
        Assert.IsTrue(string.Equals(
            "0xbc4ca0eda7647a8ab7c2061c2e118a18a936f13d",
            nft.ContractAddress.ToString(),
            StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(BigInteger.Parse("1234"), nft.TokenId);
        Assert.IsTrue(string.Equals(
            "0x1234567890123456789012345678901234567890",
            nft.Owner.ToString(),
            StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("ipfs://QmeSjSinHpPnmXmspMjwiXyN6zS4E9zccariGR3jxcaWtq/1234", nft.TokenURI);
    }

    [TestMethod]
    public void Attestation_ConvertFromDictionary_Success()
    {
        // Arrange - Simulating EAS attestation data
        var dictionary = new Dictionary<string, object?>
        {
            { "Uid", Hex.Parse("0x1234567890123456789012345678901234567890123456789012345678901234").ToByteArray() },
            { "Schema", Hex.Parse("0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890").ToByteArray() },
            { "Time", BigInteger.Parse("1678901234") },
            { "ExpirationTime", BigInteger.Parse("1778901234") },
            { "RevocationTime", BigInteger.Parse("0") },
            { "RefUID", Hex.Parse("0x0000000000000000000000000000000000000000000000000000000000000000").ToByteArray() },
            { "Recipient", "0x1234567890123456789012345678901234567890" },
            { "Attester", "0xabcdef1234567890abcdef1234567890abcdef12" },
            { "Revocable", true },
            { "Data", Hex.Parse("0x1234567890").ToByteArray() }
        };

        // Act
        var attestation = this.converter.DictionaryToObject<Attestation>(dictionary);

        // Assert
        Assert.IsNotNull(attestation);
        Assert.AreEqual(Hex.Parse("0x1234567890123456789012345678901234567890123456789012345678901234"), attestation.Uid);
        Assert.AreEqual(Hex.Parse("0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890"), attestation.Schema);
        Assert.AreEqual(BigInteger.Parse("1678901234"), attestation.Time);
        Assert.AreEqual(BigInteger.Parse("1778901234"), attestation.ExpirationTime);
        Assert.AreEqual(BigInteger.Parse("0"), attestation.RevocationTime);
        Assert.AreEqual(Hex.Parse("0x0000000000000000000000000000000000000000000000000000000000000000"), attestation.RefUID);

        // Use case-insensitive comparison for Ethereum addresses
        Assert.IsTrue(string.Equals(
            "0x1234567890123456789012345678901234567890",
            attestation.Recipient.ToString(),
            StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(string.Equals(
            "0xabcdef1234567890abcdef1234567890abcdef12",
            attestation.Attester.ToString(),
            StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(attestation.Revocable);
        Assert.AreEqual(Hex.Parse("0x1234567890"), attestation.Data);
    }

    [TestMethod]
    public void NullableUser_ConvertFromDictionary_Success()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "Name", "Optional User" },
            { "Age", null },
            { "IsActive", true },
            { "WalletAddress", null }
        };

        // Act
        var user = this.converter.DictionaryToObject<NullableUser>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Optional User", user.Name);
        Assert.IsNull(user.Age);
        Assert.IsTrue(user.IsActive);
        Assert.IsNull(user.WalletAddress);
    }

    [TestMethod]
    public void ConvertFromAbiParameters_Success()
    {
        // Arrange
        var parameters = AbiParameters.Parse("(string name, uint256 age, bool isActive)");
        parameters[0].Value = "John Doe";
        parameters[1].Value = BigInteger.Parse("25");
        parameters[2].Value = true;

        // Act
        var user = parameters.ToObject<SimpleUser>();

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("John Doe", user.Name);
        Assert.AreEqual(BigInteger.Parse("25"), user.Age);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void ConvertFromContractAbi_Success()
    {
        // Arrange - Simulating a contract function call result
        var contractAbi = new ContractAbi(new List<ContractAbiItem>
        {
            new ContractAbiItem
            {
                Type = "function",
                Name = "getUserInfo",
                Outputs = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Name = "name", Type = "string" },
                    new ContractAbiParameter { Name = "age", Type = "uint256" },
                    new ContractAbiParameter { Name = "isActive", Type = "bool" }
                }
            }
        });

        // Simulate decoded function output
        var outputValues = new Dictionary<string, object?>
        {
            { "name", "John Doe" },
            { "age", BigInteger.Parse("25") },
            { "isActive", true }
        };

        // Act
        var user = this.converter.ContractFunctionOutputToObject<SimpleUser>(
            contractAbi, "getUserInfo", outputValues);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("John Doe", user.Name);
        Assert.AreEqual(BigInteger.Parse("25"), user.Age);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void NameMapping_TakesPriorityOverPosition_WhenKeysOutOfOrder()
    {
        // Arrange - Dictionary with keys in different order than POCO properties
        var dictionary = new Dictionary<string, object?>
        {
            { "Age", BigInteger.Parse("30") },      // This is the second property in the POCO
            { "Name", "John Doe" },                 // This is the first property in the POCO
            { "IsActive", true }                    // This is the third property in the POCO
        };

        // Act
        var user = this.converter.DictionaryToObject<SimpleUser>(dictionary);

        // Assert - Properties should be mapped by name, not position
        Assert.IsNotNull(user);
        Assert.AreEqual("John Doe", user.Name);     // Should match "Name" key, not first position
        Assert.AreEqual(BigInteger.Parse("30"), user.Age); // Should match "Age" key, not second position
        Assert.IsTrue(user.IsActive);               // Should match "IsActive" key, not third position
    }

    [TestMethod]
    public void NameMapping_WorksWithMixedCasing()
    {
        // Arrange - Dictionary with keys that have different casing than POCO properties
        var dictionary = new Dictionary<string, object?>
        {
            { "AGE", BigInteger.Parse("30") },      // Uppercase vs. PascalCase in POCO
            { "name", "John Doe" },                 // Lowercase vs. PascalCase in POCO
            { "isActive", true }                    // Camel case vs. PascalCase in POCO
        };

        // Act
        var user = this.converter.DictionaryToObject<SimpleUser>(dictionary);

        // Assert - Properties should be mapped by name case-insensitively
        Assert.IsNotNull(user);
        Assert.AreEqual("John Doe", user.Name);
        Assert.AreEqual(BigInteger.Parse("30"), user.Age);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void PositionalMapping_UsedWhenNoNameMatch()
    {
        // Arrange - Dictionary with no name matches but valid positional values
        var dictionary = new Dictionary<string, object?>
        {
            { "0", "John Doe" },                    // First position
            { "1", BigInteger.Parse("30") },        // Second position
            { "2", true },                          // Third position
            { "UnrelatedKey", "Ignored value" }     // Unrelated key
        };

        // Act
        var user = this.converter.DictionaryToObject<SimpleUser>(dictionary);

        // Assert - Properties should be mapped by position when no name matches
        Assert.IsNotNull(user);
        Assert.AreEqual("John Doe", user.Name);
        Assert.AreEqual(BigInteger.Parse("30"), user.Age);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void AttributeMappedUser_ComplexMapping_Success()
    {
        // Arrange - Dictionary with keys that don't match property names and are out of order
        var dictionary = new Dictionary<string, object?>
        {
            { "customName", "John Doe" },                   // 0 / Maps to .Name by attribute name
            { "someRandomKey", "This should be ignored" },  // 1 / Should be ignored (no matching attribute)
            { "active_in_pos_2", true },                    // 2 / Maps to .IsActive by position (3rd property)
            { "years", BigInteger.Parse("30") },            // 3 / Maps to .Age by attribute name
            { "0", "This should be ignored too" },          // 4 / Should be ignored (.Name mapped by customName)
            { "wallet", "0xabcdef1234567890abcdef1234567890abcdef12" } // 5 / Maps to .Address by attribute name
        };

        // Act
        var user = this.converter.DictionaryToObject<ComplexAttributeMappedUser>(dictionary);

        // Assert
        Assert.IsNotNull(user, "User should not be null");
        Assert.AreEqual("John Doe", user.Name, "Name should match");
        Assert.AreEqual(BigInteger.Parse("30"), user.Age, "Age should match");
        Assert.IsTrue(user.IsActive, "IsActive should match");
        Assert.IsTrue(string.Equals(
            "0xabcdef1234567890abcdef1234567890abcdef12",
            user.Address.ToString(),
            StringComparison.OrdinalIgnoreCase));
        Assert.IsNull(user.IgnoredProperty, $"IgnoredProperty should remain null, contains '{user.IgnoredProperty}'");
    }

    [TestMethod]
    public void ArrayWithTypeConversion_ConvertFromDictionary_Success()
    {
        // Arrange - Array of integers that should be converted to BigInteger[]
        var dictionary = new Dictionary<string, object?>
    {
        { "Name", "TypeConversionUser" },
        { "Scores", new[] { 95, 87, 92 } } // Array of integers, not BigIntegers
    };

        // Act
        var user = this.converter.DictionaryToObject<UserWithBigIntegerArray>(dictionary);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("TypeConversionUser", user.Name);
        Assert.IsNotNull(user.Scores);
        Assert.AreEqual(3, user.Scores.Length);
        Assert.AreEqual(BigInteger.Parse("95"), user.Scores[0]);
        Assert.AreEqual(BigInteger.Parse("87"), user.Scores[1]);
        Assert.AreEqual(BigInteger.Parse("92"), user.Scores[2]);
    }

}
