using System.Text;
using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;

namespace Evoq.Ethereum.MessageSigning;

[TestClass]
public class MessageSignerTests
{
    private const string PrivateKeyEnvironmentKey = "Blockchain__Ethereum__Addresses__TZContractDevTestPrivateKey";
    private const string AddressEnvironmentKey = "Blockchain__Ethereum__Addresses__TZContractDevTestAddress";

    //

    private Hex privateKey;
    private Hex address;

    //

    public MessageSignerTests()
    {
        this.privateKey = Hex.Parse(System.Environment.GetEnvironmentVariable(PrivateKeyEnvironmentKey)!);
        this.address = Hex.Parse(System.Environment.GetEnvironmentVariable(AddressEnvironmentKey)!);
    }

    //

    [TestMethod]
    public void SignAndVerifyMessage_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var message = "Hello, Ethereum!";
        var messageSigner = new MessageSigner();
        var signer = new Secp256k1Signer(this.privateKey.ToByteArray());
        var signerAddress = new EthereumAddress(this.address);

        // Act
        var signatureBytes = messageSigner.GetPersonalSignSignature(signer, message);
        var rsv = RsvSignature.FromBytes(signatureBytes);
        var payload = new PersonalSignSigningPayload(message);
        var isValid = messageSigner.VerifyMessage(payload, rsv, signerAddress);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void SignerSign_PersonalSignPayload_ReturnsTrue()
    {
        // Arrange
        var message = "Hello, Ethereum!";
        var signer = new Secp256k1Signer(this.privateKey.ToByteArray());
        var signerAddress = new EthereumAddress(this.address);
        var payload = new PersonalSignSigningPayload(message);

        // Act
        var signature = signer.Sign(payload);

        // Assert
        var isValid = new MessageSigner().VerifyMessage(payload, signature, signerAddress);

        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void Test()
    {
        // Arrange
        var message = "Hello, Ethereum!";
        var signer = new Secp256k1Signer(this.privateKey.ToByteArray());
        var signerAddress = new EthereumAddress(this.address);
        var payload = new PersonalSignSigningPayload(message);

        // Act
        var signature = signer.Sign(payload);

        // Assert
        var isValid = signerAddress.HasSigned(payload.Data, signature);

        Assert.IsTrue(isValid);
    }
}
