using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;

namespace Evoq.Ethereum.Accounts.Blockchain;

/// <summary>
/// A transaction runner that depends on Nethereum to submit transactions.
/// </summary>
public sealed class TransactionRunnerNethereum : TransactionRunner<Contract, object[], TransactionReceipt>
{
    private readonly Hex privateKey;

    //

    public TransactionRunnerNethereum(
        Hex privateKey,
        INonceStore nonceStore,
        ILoggerFactory loggerFactory)
        : base(nonceStore, loggerFactory)
    {
        this.privateKey = privateKey;
    }

    //

    protected async override Task<TransactionReceipt> SubmitTransactionAsync(
        Contract contract, string functionName, TransactionFees fees, uint nonce, object[] args)
    {
        var f = contract.GetFunction(functionName);

        if (this.privateKey.IsZeroValue())
        {
            throw new InvalidOperationException("Private key is required to submit a transaction.");
        }

        var account = new Nethereum.Web3.Accounts.Account(this.privateKey.ToString());

        var input = new TransactionInput
        {
            From = account.Address,
            Gas = new Nethereum.Hex.HexTypes.HexBigInteger(fees.GasLimit),
            MaxFeePerGas = new Nethereum.Hex.HexTypes.HexBigInteger(fees.MaxFeePerGas),
            MaxPriorityFeePerGas = new Nethereum.Hex.HexTypes.HexBigInteger(fees.MaxPriorityFeePerGas),
            Value = new Nethereum.Hex.HexTypes.HexBigInteger(fees.Value),
            Nonce = new Nethereum.Hex.HexTypes.HexBigInteger(nonce)
        };

        try
        {
            this.logger.LogDebug(
                "Submitting transaction: Nonce={Nonce}, Contract={ContractAddress}, Function={FunctionName}, " +
                "GasLimit={GasLimit}, MaxFeePerGas={MaxFeePerGas}, MaxPriorityFeePerGas={MaxPriorityFeePerGas}, Value={Value}",
                nonce, contract.Address, functionName,
                fees.GasLimit, fees.MaxFeePerGas, fees.MaxPriorityFeePerGas, fees.Value);

            var startTime = DateTime.UtcNow;

            var receipt = await f.SendTransactionAndWaitForReceiptAsync(input, null, args);

            var duration = DateTime.UtcNow - startTime;

            this.logger.LogDebug(
                "Transaction completed in {Duration:g}: Nonce={Nonce}, Contract={ContractAddress}, Function={FunctionName}, " +
                "TransactionHash={TransactionHash}, From={From}, GasUsed={GasUsed}, " +
                "CumulativeGasUsed={CumulativeGasUsed}, EffectiveGasPrice={EffectiveGasPrice}, Status={Status}",
                duration, nonce, contract.Address, functionName,
                receipt.TransactionHash, receipt.From, receipt.GasUsed,
                receipt.CumulativeGasUsed, receipt.EffectiveGasPrice, receipt.Status);

            return receipt;
        }
        catch (Exception tooManyArgs)
        when (tooManyArgs.Message.Contains("Too many arguments"))
        {
            var message = $"Transaction failed to submit. The number of arguments" +
                $" {args.Length} does not match the number of parameters" +
                $" in the function '{functionName}' according to the ABI.";

            this.logger.LogError(tooManyArgs, message);

            throw new FailedToSubmitTransactionException(message, tooManyArgs);
        }
    }

    //

    /// <summary>
    /// Decodes events from a transaction receipt.
    /// </summary>
    /// <typeparam name="TEventDto">The type of the event to decode.</typeparam>
    /// <param name="receipt">The transaction receipt to decode events from.</param>
    /// <returns>A list of decoded events.</returns>
    public IReadOnlyList<TEventDto> DecodeEvents<TEventDto>(TransactionReceipt receipt)
        where TEventDto : IEventDTO, new()
    {
        this.logger.LogDebug(
            "Decoding events of type {EventDto} from receipt for transaction {TransactionHash}",
            typeof(TEventDto).Name, receipt.TransactionHash);

        var events = receipt.Logs.Select(log => log.ToObject<FilterLog>()).ToArray();

        if (events.Length == 0)
        {
            this.logger.LogDebug(
                "No events found in receipt for transaction {TransactionHash}",
                receipt.TransactionHash);

            return new List<TEventDto>();
        }

        var decodedEvents = Event<TEventDto>.DecodeAllEvents(events);

        if (decodedEvents.Count == 0)
        {
            this.logger.LogDebug(
                "No events decoded from receipt for transaction {TransactionHash}",
                receipt.TransactionHash);

            return new List<TEventDto>();
        }

        return decodedEvents.Select(e => e.Event).ToList();
    }

    /// <summary>
    /// Decodes the first event of the given type from a transaction receipt.
    /// </summary>
    /// <typeparam name="TEventDto">The type of the event to decode.</typeparam>
    /// <param name="receipt">The transaction receipt to decode the event from.</param>
    /// <returns>The decoded event.</returns>
    /// <exception cref="MissingEventLogException">Thrown when no events are found in the receipt.</exception>
    public TEventDto DecodeEvent<TEventDto>(TransactionReceipt receipt) where TEventDto : IEventDTO, new()
    {
        var events = this.DecodeEvents<TEventDto>(receipt);

        if (events.Count == 0)
        {
            throw new MissingEventLogException(
                $"No events of type {typeof(TEventDto).Name} decoded from receipt.")
            {
                TransactionHash = receipt.TransactionHash
            };
        }

        return events.First();
    }
}
