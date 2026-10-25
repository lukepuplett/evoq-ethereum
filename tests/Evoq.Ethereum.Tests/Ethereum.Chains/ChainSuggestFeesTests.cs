using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Tests.Ethereum.Chains;

/// <summary>
/// Tests that EIP-1559 fee suggestions use the tip observed on the network (eth_feeHistory)
/// rather than the hard-coded 2 gwei default.
/// </summary>
[TestClass]
public class ChainSuggestFeesTests
{
    private const string BaseFeeHex = "0x1770"; // 6,000 wei, i.e. a very cheap L2 like Base Sepolia.

    [TestMethod]
    public async Task SuggestEip1559FeesAsync_FeeHistoryHasLowTips_UsesNewestBlockTipNotDefault()
    {
        // Rewards are ordered oldest to newest; only the last block's tip (1,000 wei) should be used.
        var rewards = "[[\"0x3b9aca00\"],[\"0x1\"],[\"0x2\"],[\"0x3\"],[\"0x4\"],[\"0x5\"],[\"0x6\"],[\"0x7\"],[\"0x8\"],[\"0x3e8\"]]";
        var chain = CreateChain(feeHistoryResult: FeeHistoryJson(rewards));

        var (maxFee, tip) = await chain.SuggestEip1559FeesAsync(new JsonRpcContext());

        Assert.AreEqual(new BigInteger(1000), tip,
            "The tip should be the median reward of the newest block, not the 2 gwei default or the oldest block's reward.");
        Assert.AreEqual(new BigInteger(6000 * 2 + 1000), maxFee,
            "Max fee should be base fee * 2 + the observed tip.");
    }

    [TestMethod]
    public async Task SuggestEip1559FeesAsync_FeeHistoryHasNoRewards_FallsBackToStandardPriorityFee()
    {
        var chain = CreateChain(feeHistoryResult: FeeHistoryJson("[]"));

        var (_, tip) = await chain.SuggestEip1559FeesAsync(new JsonRpcContext());

        Assert.AreEqual(WeiAmounts.StandardPriorityFee, tip,
            "With no reward data the 2 gwei default should be kept.");
    }

    private static string FeeHistoryJson(string rewards) =>
        "{\"oldestBlock\":\"0x1\",\"baseFeePerGas\":[\"0x1770\"],\"gasUsedRatio\":[0.5]," +
        $"\"reward\":{rewards}}}";

    private static Chain CreateChain(string feeHistoryResult)
    {
        var handler = new MockJsonRpcHandler(async request =>
        {
            var body = JsonSerializer.Deserialize<JsonElement>(await request.Content!.ReadAsStringAsync());
            var id = body.GetProperty("id").GetInt32();
            var method = body.GetProperty("method").GetString();

            var result = method switch
            {
                "eth_getBlockByNumber" => $"{{\"number\":\"0x1\",\"baseFeePerGas\":\"{BaseFeeHex}\",\"transactions\":[]}}",
                "eth_feeHistory" => feeHistoryResult,
                _ => "\"0x1\"",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{result}}}", Encoding.UTF8, "application/json")
            };
        });

        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));

        return Chain.CreateDefault(1, new Uri("http://test.example.com"), loggerFactory, new MockHttpClientFactory(handler));
    }
}
