using System.Numerics;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Examples;

[TestClass]
public class ExampleTransfer
{
    [TestMethod]
    [Ignore]
    public async Task Should_Send_Eth()
    {
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;

        ExampleEAS.SetupLocalHardhatBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        //

        EthereumAddress senderAddress;
        SenderAccount senderAccount;
        ExampleEAS.SetupLocalHardhatAccount(configuration, out senderAddress, out senderAccount);

        //

        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);

        Sender sender = ExampleEAS.SetupSender(loggerFactory, senderAddress, senderAccount, chain, useInMemoryNonces: true);

        //

        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);

        var context = new JsonRpcContext();

        //

        var runner = TransferRunnerNative.CreateDefault(endpoint, sender);

        var gasOptions = new LegacyGasOptions(21_000, BigInteger.Parse("1000000000000000000"));

        // send 0 eth to self

        var receipt = await runner.RunTransferAsync(
            context, new TransferInvocationOptions(gasOptions, EtherAmount.FromWei(0), senderAddress), null);

        Assert.IsNotNull(receipt);
        Assert.IsTrue(receipt.Success);

        // last run May 15 2025, successful
    }
}
