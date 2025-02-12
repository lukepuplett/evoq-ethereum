using Evoq.Blockchain;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// A struct that contains the information needed to connect to an Ethereum endpoint.
/// </summary>
/// <param name="ProviderName">The name of the provider.</param>
/// <param name="NetworkName">The name of the network.</param>
/// <param name="URL">The URL of the endpoint.</param>
/// <param name="LoggerFactory">The logger factory.</param>
public record struct Endpoint(string ProviderName, string NetworkName, string URL, ILoggerFactory LoggerFactory);

/// <summary>
/// A struct that contains the information needed to send a transaction.
/// </summary>
/// <param name="PrivateKey">The private key of the sender.</param>
/// <param name="NonceStore">The nonce store.</param>
public record struct Sender(Hex PrivateKey, INonceStore NonceStore);
