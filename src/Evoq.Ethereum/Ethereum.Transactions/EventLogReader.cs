using System;
using System.Collections.Generic;
using System.Linq;
using Evoq.Ethereum.ABI;

namespace Evoq.Ethereum.Transactions;

internal class EventLogReader
{
    private readonly IAbiDecoder decoder;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLogReader"/> class.
    /// </summary>
    /// <param name="decoder">The ABI decoder.</param>
    public EventLogReader(IAbiDecoder decoder)
    {
        this.decoder = decoder;
    }

    //

    /// <summary>
    /// Reads logged events from a transaction receipt.
    /// </summary>
    /// <param name="receipt">The transaction receipt.</param>
    /// <param name="eventSignature">The event signature.</param>
    /// <param name="indexed">The logged indexed events.</param>
    /// <param name="data">The logged data events.</param>
    /// <returns>True if the events were read successfully, false otherwise.</returns>
    public bool TryRead(
        TransactionReceipt receipt,
        AbiSignature eventSignature,
        out IReadOnlyDictionary<string, object?>? indexed,
        out IReadOnlyDictionary<string, object?>? data)
    {
        if (eventSignature.ItemType != AbiItemType.Event)
        {
            throw new ArgumentException("Event signature must be an event", nameof(eventSignature));
        }

        if (receipt.Logs.Length == 0)
        {
            indexed = null;
            data = null;
            return false;
        }

        // Find matching log
        TransactionLog? log;
        if (eventSignature.IsAnonymous)
        {
            // For anonymous events, we can't match by signature hash
            // Just take the first log that has the right number of topics
            var indexedCount = eventSignature.Inputs.Count(p => p.IsIndexed);
            log = receipt.Logs.FirstOrDefault(l => l.Topics.Count == indexedCount);
        }
        else
        {
            // For normal events, match by signature hash in topics[0]
            var eventSignatureHash = eventSignature.GetSignatureHash();
            log = receipt.Logs.FirstOrDefault(l => l.Topics.Any() && l.Topics[0] == eventSignatureHash);
        }

        if (log == null)
        {
            indexed = null;
            data = null;
            return false;
        }

        var indexedResults = new Dictionary<string, object?>();
        var dataResults = new Dictionary<string, object?>();

        // Deal with indexed params
        var indexedParams = eventSignature.Inputs.Where(p => p.IsIndexed).ToList();
        foreach (var param in indexedParams)
        {
            // Get the correct topic index - for anonymous events start at 0, for normal events start at 1
            var topicIndex = eventSignature.IsAnonymous ?
                indexedParams.IndexOf(param) :
                indexedParams.IndexOf(param) + 1;

            var topic = log.Topics[topicIndex];

            if (AbiTypes.IsHashedInTopic(param.AbiType))
            {
                indexedResults[param.Name] = topic;
            }
            else
            {
                var obj = this.decoder.DecodeParameter(param, topic.ToByteArray());
                indexedResults[param.Name] = obj;
            }
        }

        // Deal with data params
        var dp = eventSignature.Inputs.Where(p => !p.IsIndexed).ToList();
        if (dp.Count > 0)
        {
            var dataParams = new AbiParameters(dp);
            var r = this.decoder.DecodeParameters(dataParams, log.Data.ToByteArray());

            foreach (var param in r.Parameters)
            {
                dataResults[param.Name] = param.Value;
            }
        }

        indexed = indexedResults;
        data = dataResults;

        return true;
    }
}
