using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.ABI.Conversion;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Examples;

[TestClass]
public class ExampleEAS
{
    [TestMethod]
    [Ignore]
    public async Task Should_GetOrRegisterSchema_WhenSchemaDoesNotExist()
    {
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;

        SetupLocalHardhatBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        //

        EthereumAddress senderAddress;
        SenderAccount senderAccount;
        SetupLocalHardhatAccount(configuration, out senderAddress, out senderAccount);

        //

        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);

        Sender sender = SetupSender(loggerFactory, senderAddress, senderAccount, chain, useInMemoryNonces: true);

        //

        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);

        Contract schemaRegistry = SetupSchemaRegistryContract(endpoint, chain, sender);

        //

        string attestationSignature = "bool isTestTwo";

        var schemaUIDSchema = AbiParameters.Parse("(string schema, address resolver, bool revocable)");
        var simpleRevocableBool = AbiKeyValues.Create(
            ("schema", attestationSignature),
            ("resolver", EthereumAddress.Zero),
            ("revocable", true));

        var packer = new AbiEncoderPacked();
        var schemaUID = KeccakHash.ComputeHash(packer.EncodeParameters(schemaUIDSchema, simpleRevocableBool).ToByteArray());
        // var schemaUID = KeccakHash.ComputeHash(Encoding.UTF8.GetBytes("fake"));

        Console.WriteLine("schemaUID: " + schemaUID.ToHexStruct());

        var caller = new RawContractCaller(endpoint);
        var context = new JsonRpcContext();

        var schemaUidReturnedHex = await caller.CallAsync(
            context,
            schemaRegistry.Address,
            "getSchema(bytes32 uid)",
            ("uid", schemaUID));

        Assert.IsTrue(schemaUidReturnedHex.Length > 0);

        //

        var getSchemaDecodedResult = caller.DecodeParameters(
            "((bytes32 uid, address resolver, bool revocable, string schema) record)", schemaUidReturnedHex);

        Assert.IsTrue(getSchemaDecodedResult.Parameters.TryFirst(out var first));

        var record = first.Value as IDictionary<string, object?>;
        Assert.IsNotNull(record);
        Assert.IsTrue(record.Count == 4);
        Assert.IsTrue(record.TryGetValue("uid", out var uid));
        Assert.IsTrue(record.TryGetValue("schema", out var schema));
        Assert.IsTrue(record.TryGetValue("resolver", out var resolver));
        Assert.IsTrue(record.TryGetValue("revocable", out var revocable));

        if (uid is not byte[] uidBytes)
        {
            throw new Exception("uid is not a byte array");
        }

        var existingUIDHex = Hex.FromBytes(uidBytes);

        if (existingUIDHex.IsZeroValue())
        {
            // schema does not exist

            var registerEstimate = await schemaRegistry.EstimateTransactionFeeAsync(
                context,
                "register",
                senderAddress,
                null,
                simpleRevocableBool);

            registerEstimate = registerEstimate.InEther();

            //

            var registerOptions = new ContractInvocationOptions(registerEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);

            var runner = new TransactionRunnerNative(sender, loggerFactory);

            var registerReceipt = await runner.RunTransactionAsync(
                context,
                schemaRegistry,
                "register",
                registerOptions,
                simpleRevocableBool);

            Assert.IsNotNull(registerReceipt);
            Assert.IsTrue(registerReceipt.Success);

            Console.WriteLine(registerReceipt);

            // read the event data

            bool hasRegistered = schemaRegistry.TryReadEventLogsFromReceipt(
                registerReceipt, "Registered", out var indexed, out var data);

            if (!hasRegistered)
            {
                throw new Exception("The event was not found in the receipt");
            }

            if (data == null || data.None())
            {
                throw new Exception("The event data was not found in the receipt");
            }

            foreach (var (key, value) in data)
            {
                Console.WriteLine($"{key}: {value}");
            }

            Assert.IsNotNull(registerReceipt);
            Assert.IsTrue(registerReceipt.Success);
        }
        else
        {
            // schema exists
        }



        throw new NotImplementedException("Not implemented");

        //

    }

    [TestMethod]
    [Ignore]
    public async Task Should_RegisterNewSchema_AndEmitRegisteredEvent()
    {
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;

        SetupLocalHardhatBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        //

        EthereumAddress senderAddress;
        SenderAccount senderAccount;
        SetupLocalHardhatAccount(configuration, out senderAddress, out senderAccount);

        //

        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);

        Sender sender = SetupSender(loggerFactory, senderAddress, senderAccount, chain);

        //

        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);

        Contract schemaRegistry = SetupSchemaRegistryContract(endpoint, chain, sender);

        //

        string attestationSignature = "bool isTestTwo";

        var registerArgs = AbiKeyValues.Create(
            ("schema", attestationSignature),
            ("resolver", EthereumAddress.Zero),
            ("revocable", true));

        var context = new JsonRpcContext();

        var registerEstimate = await schemaRegistry.EstimateTransactionFeeAsync(
            context,
            "register",
            senderAddress,
            null,
            registerArgs);

        registerEstimate = registerEstimate.InEther();

        //

        var registerOptions = new ContractInvocationOptions(registerEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);

        var runner = new TransactionRunnerNative(sender, loggerFactory);

        var registerReceipt = await runner.RunTransactionAsync(
            context,
            schemaRegistry,
            "register",
            registerOptions,
            registerArgs);

        Console.WriteLine(registerReceipt);

        // read the event data

        bool hasRegistered = schemaRegistry.TryReadEventLogsFromReceipt(
            registerReceipt, "Registered", out var indexed, out var data);

        if (!hasRegistered)
        {
            throw new Exception("The event was not found in the receipt");
        }

        if (data == null || data.None())
        {
            throw new Exception("The event data was not found in the receipt");
        }

        foreach (var (key, value) in data)
        {
            Console.WriteLine($"{key}: {value}");
        }

        var first = data.First().Value as IDictionary<string, object?>;
        var uidValue = first!.Values.First();
        var uid = Hex.FromBytes((byte[])uidValue!); // "0x2fb3f7363a44f93b647d58a3090f2b64106c76ed9f9c73a99a0a11f963d3940e"

        Assert.IsNotNull(registerReceipt);
        Assert.IsTrue(registerReceipt.Success);
    }

    [TestMethod]
    [Ignore]
    public async Task Should_Attest_ThenGetAttestation()
    {
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;

        SetupLocalHardhatBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        //

        EthereumAddress senderAddress;
        SenderAccount senderAccount;
        SetupLocalHardhatAccount(configuration, out senderAddress, out senderAccount);

        //

        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);

        Sender sender = SetupSender(loggerFactory, senderAddress, senderAccount, chain);

        //

        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);

        Contract eas = SetupEASContract(endpoint, chain, sender);

        //

        var schemaUID = Hex.Parse("0x2fb3f7363a44f93b647d58a3090f2b64106c76ed9f9c73a99a0a11f963d3940e");

        // Create the inner AttestationRequestData tuple
        var attestationRequestData = AbiKeyValues.Create(
            ("recipient", senderAddress),           // address
            ("expirationTime", 0UL),               // uint64 - no expiration
            ("revocable", true),                   // bool
            ("refUID", Hex.Zero),                  // bytes32 - no reference
            ("data", Hex.Empty),                   // see how it handles empty bytes
            ("value", BigInteger.Zero)             // uint256 - no value
        );

        // Create the outer AttestationRequest tuple
        var attestationRequest = AbiKeyValues.Create(
            ("schema", schemaUID),                 // bytes32
            ("data", attestationRequestData)       // tuple
        );

        var context = new JsonRpcContext();

        var attestationEstimate = await eas.EstimateTransactionFeeAsync(
            context,
            "attest",
            senderAddress,
            null,
            AbiKeyValues.Create("request", attestationRequest));

        attestationEstimate = attestationEstimate.InEther();

        // attest

        var attestationOptions = new ContractInvocationOptions(attestationEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);
        var runner = new TransactionRunnerNative(sender, loggerFactory);

        var receipt = await runner.RunTransactionAsync(
            context,
            eas,
            "attest",
            attestationOptions,
            AbiKeyValues.Create("request", attestationRequest));

        Console.WriteLine(receipt);

        // get the event to get the attestation UID

        Assert.IsTrue(eas.TryReadEventLogsFromReceipt(receipt, "Attested", out var _, out var attested));
        Assert.IsTrue(attested!.TryGetValue("uid", out var uid));
        Assert.IsTrue(uid is byte[]);

        var uidHex = Hex.FromBytes((byte[])uid);

        // get the attestation we just made

        AttestationDTO dto;
        bool useManualConversion = false;

        if (useManualConversion)
        {
            var rootDic = await eas.CallAsync(
                context,
                "getAttestation",
                senderAddress,
                AbiKeyValues.Create("uid", uidHex));

            Assert.IsNotNull(rootDic);

            var attestationDic = rootDic.First().Value as IReadOnlyDictionary<string, object?>;

            Assert.IsNotNull(attestationDic);

            var converter = new AbiConverter();

            dto = converter.DictionaryToObject<AttestationDTO>(attestationDic);
        }
        else
        {
            dto = await eas.CallAsync<AttestationDTO>(
                context,
                "getAttestation",
                senderAddress,
                AbiKeyValues.Create("uid", uidHex));
        }

        Assert.IsNotNull(dto);
        Assert.AreEqual(uidHex, dto.UID);
        Assert.AreEqual(schemaUID, dto.Schema);

    }

    [TestMethod]
    [Ignore]
    public async Task Should_SendTransactionManually_WithGasValidation()
    {
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;

        SetupLocalHardhatBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        EthereumAddress senderAddress;
        SenderAccount senderAccount;

        SetupLocalHardhatAccount(configuration, out senderAddress, out senderAccount);

        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);
        var context = new JsonRpcContext();

        var getStartingNonce = async () => await chain.GetTransactionCountAsync(context, senderAddress);

        var noncePath = Path.Combine(Path.GetTempPath(), Path.Combine("hardhat-nonces", senderAddress.ToString()));
        var nonceStore = new FileSystemNonceStore(noncePath, loggerFactory, getStartingNonce);

        var schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");
        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);
        var sender = new Sender(senderAccount, nonceStore);
        var abiStream = AbiFileHelper.GetAbiStream("EASSchemaRegistry.abi.json");
        var contract = chain.GetContract(schemaRegistryAddress, endpoint, sender, abiStream);

        // guess gas price

        var registerEstimate = await contract.EstimateTransactionFeeAsync(
            context,
            "register",
            senderAddress,
            null,
            AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true));

        registerEstimate = registerEstimate.InEther();

        // throw new Exception();

        BigInteger etherPriceInCents = 193045; // $1,930.45 as of 15 March 2025

        Assert.IsTrue(100_000 > registerEstimate.EstimatedGasLimit);
        Assert.AreEqual(EtherAmount.FromWei(2_000_000_000), registerEstimate.SuggestedMaxFeePerGas);
        Assert.IsTrue(registerEstimate.SuggestedMaxPriorityFeePerGas >= EtherAmount.FromWei(2_000_000_000), $"MaxPriorityFeePerGas is {registerEstimate.SuggestedMaxPriorityFeePerGas}");
        Assert.AreEqual(EtherAmount.FromWei(0), registerEstimate.CurrentBaseFeePerGas, $"BaseFeePerGas is {registerEstimate.CurrentBaseFeePerGas}");
        Assert.IsTrue(registerEstimate.EstimatedTotalFee.ToLocalCurrency(etherPriceInCents) < 90, $"EstimatedFee is {registerEstimate.EstimatedTotalFee.ToLocalCurrency(etherPriceInCents)}c");

        // call register

        var n = 1u;// await nonceStore.BeforeSubmissionAsync();

        var registerOptions = new ContractInvocationOptions(registerEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);
        var registerArgs = AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true);

        try
        {
            Hex registerResult = await contract.InvokeMethodAsync(
                context,
                "register",
                n,
                registerOptions,
                registerArgs);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
        }

        // get schema

        var abiPacker = new AbiEncoderPacked();
        var easSchemaUidSchema = AbiParameters.Parse("string schema, address resolver, bool revocable");
        var easSchemaUidSchemaValues = AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true);
        var easSchemaUid = KeccakHash.ComputeHash(abiPacker.EncodeParameters(easSchemaUidSchema, easSchemaUidSchemaValues).ToByteArray());

        var getSchemaResult = await contract.CallAsync(
            context,
            "getSchema",
            senderAddress,
            AbiKeyValues.Create("uid", easSchemaUid));

        Assert.IsNotNull(getSchemaResult, "The call to getSchema returned a null result");

        if (!getSchemaResult.Values.TryFirst(out var first))
        {
            throw new Exception("The call to getSchema returned an empty dictionary");
        }

        if (first is not IReadOnlyDictionary<string, object?> firstDict)
        {
            throw new Exception("The call to getSchema returned an unexpected result");
        }

        var converter = new AbiConverter();
        var obj = converter.DictionaryToObject<SchemaRecordDto>(firstDict);

        Assert.IsNotNull(obj, "The call to getSchema returned an unexpected result");

        getSchemaResult.DeepVisitEach(pair => Console.WriteLine($"Result: {pair.Key}: {pair.Value}"));
    }

    //

    private static Contract SetupSchemaRegistryContract(Endpoint endpoint, Chain chain, Sender sender)
    {
        var abiStream = AbiFileHelper.GetAbiStream("EASSchemaRegistry.abi.json");
        var schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");

        return chain.GetContract(schemaRegistryAddress, endpoint, sender, abiStream);
    }

    private static Contract SetupEASContract(Endpoint endpoint, Chain chain, Sender sender)
    {
        var abiStream = AbiFileHelper.GetAbiStream("EAS.abi.json");
        var easAddress = new EthereumAddress("0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512");

        return chain.GetContract(easAddress, endpoint, sender, abiStream);
    }

    internal static Sender SetupSender(ILoggerFactory loggerFactory, EthereumAddress senderAddress, SenderAccount senderAccount, Chain chain, bool useInMemoryNonces = false)
    {
        INonceStore nonceStore;
        var getStartingNonce = async () => await chain.GetTransactionCountAsync(new JsonRpcContext(), senderAddress);

        if (useInMemoryNonces)
        {
            nonceStore = new InMemoryNonceStore(loggerFactory);
        }
        else
        {
            var noncePath = Path.Combine(Path.GetTempPath(), Path.Combine("hardhat-nonces", senderAddress.ToString()));
            nonceStore = new FileSystemNonceStore(noncePath, loggerFactory, getStartingNonce);
        }

        return new Sender(senderAccount, nonceStore);
    }

    internal static void SetupLocalHardhatAccount(IConfigurationRoot configuration, out EthereumAddress senderAddress, out SenderAccount senderAccount)
    {
        var privateKeyStr = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1PrivateKey")!;
        var privateKeyHex = Hex.Parse(privateKeyStr);

        var senderAddressStr = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1Address")!;
        senderAddress = new EthereumAddress(senderAddressStr);
        senderAccount = new SenderAccount(privateKeyHex, senderAddress);
    }

    internal static void SetupLocalHardhatBasics(out string baseUrl, out IConfigurationRoot configuration, out string chainName, out ulong chainId, out ILoggerFactory loggerFactory)
    {
        // baseUrl = "https://mainnet.infura.io/v3/";
        baseUrl = "http://localhost:8545";
        configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "GoogleCloud:ProjectName", "evoq-capricorn-timesheets" },                                 // for GCP
                    { "Blockchain:Ethereum:JsonRPC:GoogleSepolia:ProjectId", "evoq-capricorn-timesheets" }      // for GCP
                })
            .Build();
        chainName = ChainNames.Hardhat;
        chainId = ulong.Parse(ChainNames.GetChainId(chainName));
        loggerFactory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole(
                options => options.SingleLine = true).SetMinimumLevel(LogLevel.Debug));
    }
}

public record SchemaRecordDto(
    Hex Uid,
    EthereumAddress Resolver,
    bool Revocable,
    Hex Schema);


/// <summary>
/// A DTO for an attestation.
/// </summary>
public class AttestationDTO
{
    /// <summary>
    /// The UID of the attestation.
    /// </summary>
    [AbiParameter("uid", AbiType = AbiTypeNames.FixedByteArrays.Bytes32)]
    public Hex UID { get; set; } = Hex.Empty;

    /// <summary>
    /// The schema of the attestation.
    /// </summary>
    [AbiParameter("schema", AbiType = AbiTypeNames.FixedByteArrays.Bytes32)]
    public Hex Schema { get; set; } = Hex.Empty;

    /// <summary>
    /// The time of the attestation.
    /// </summary>
    [AbiParameter("time", AbiType = AbiTypeNames.IntegerTypes.Uint64)]
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// The expiration time of the attestation.
    /// </summary>
    [AbiParameter("expirationTime", AbiType = AbiTypeNames.IntegerTypes.Uint64)]
    public DateTimeOffset ExpirationTime { get; set; }

    /// <summary>
    /// The revocation time of the attestation.
    /// </summary>
    [AbiParameter("revocationTime", AbiType = AbiTypeNames.IntegerTypes.Uint64)]
    public DateTimeOffset RevocationTime { get; set; }

    /// <summary>
    /// The refUID of the attestation.
    /// </summary>
    [AbiParameter("refUID", AbiType = AbiTypeNames.FixedByteArrays.Bytes32)]
    public Hex RefUID { get; set; } = Hex.Empty;

    /// <summary>
    /// The recipient of the attestation.
    /// </summary>
    [AbiParameter("recipient", AbiType = AbiTypeNames.Address)]
    public EthereumAddress Recipient { get; set; } = EthereumAddress.Zero;

    /// <summary>
    /// The address that created the attestation.
    /// </summary>
    [AbiParameter("attester", AbiType = AbiTypeNames.Address)]
    public EthereumAddress Attester { get; set; } = EthereumAddress.Zero;

    /// <summary>
    /// Whether the attestation can be revoked.
    /// </summary>
    [AbiParameter("revocable", AbiType = AbiTypeNames.Bool)]
    public bool Revocable { get; set; }

    /// <summary>
    /// The data of the attestation.
    /// </summary>
    [AbiParameter("data", AbiType = AbiTypeNames.Bytes)]
    public byte[] Data { get; set; } = Array.Empty<byte>();
}