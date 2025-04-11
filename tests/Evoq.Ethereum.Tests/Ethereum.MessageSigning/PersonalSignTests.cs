using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;

namespace Evoq.Ethereum.MessageSigning;

[TestClass]
public class PersonalSignTests
{
    private const string PrivateKeyEnvironmentKey = "Blockchain__Ethereum__Addresses__TZContractDevTestPrivateKey";
    private const string AddressEnvironmentKey = "Blockchain__Ethereum__Addresses__TZContractDevTestAddress";

    //

    private Hex privateKey;
    private Hex address;

    //

    public PersonalSignTests()
    {
        this.privateKey = Hex.Parse(System.Environment.GetEnvironmentVariable(PrivateKeyEnvironmentKey)!);
        this.address = Hex.Parse(System.Environment.GetEnvironmentVariable(AddressEnvironmentKey)!);
    }

    //

    [TestMethod]
    public void GetSignature_WithValidMessage_ReturnsValidSignature()
    {
        // Arrange
        string message = "Hello, Ethereum!";
        var signer = new Secp256k1Signer(privateKey.ToByteArray());
        var personalSign = new PersonalSign(message, signer);

        // Act
        byte[] signature = personalSign.GetSignature();

        // Assert
        Assert.IsNotNull(signature);
        Assert.AreEqual(65, signature.Length); // Ethereum signatures are 65 bytes (r, s, v)
    }

    [TestMethod]
    public void GetSignature__WhenChecked_ReturnsValidSignature()
    {
        // Arrange
        string message = "Hello, Ethereum!";
        var signer = new Secp256k1Signer(this.privateKey.ToByteArray());
        var signerAddress = new EthereumAddress(this.address);
        var personalSign = new PersonalSign(message, signer);

        // Act
        byte[] signature = personalSign.GetSignature();
        bool isValid = signerAddress.HasSigned(new PersonalSignSigningPayload(message), RsvSignature.FromBytes(signature));

        // Assert
        Assert.IsNotNull(signature);
        Assert.AreEqual(65, signature.Length); // Ethereum signatures are 65 bytes (r, s, v)
    }
}
