using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Reads contract ABI documents.
/// </summary>
public static class AbiJsonReader
{
    /// <summary>
    /// Reads a contract ABI from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the ABI JSON.</param>
    /// <returns>The parsed contract ABI.</returns>
    public static ContractAbi Read(Stream stream)
    {
        using var document = JsonDocument.Parse(stream);
        return ParseAbiDocument(document.RootElement);
    }

    /// <summary>
    /// Reads a contract ABI from a JSON string.
    /// </summary>
    /// <param name="json">The ABI JSON string.</param>
    /// <returns>The parsed contract ABI.</returns>
    public static ContractAbi ReadFromString(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ParseAbiDocument(document.RootElement);
    }

    //

    private static ContractAbi ParseAbiDocument(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Contract ABI must be a JSON array");

        var items = new List<ContractAbiItem>();

        foreach (var element in root.EnumerateArray())
        {
            var item = ParseAbiItem(element);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return new ContractAbi(items);
    }

    private static ContractAbiItem? ParseAbiItem(JsonElement element)
    {
        var type = element.GetProperty("type").GetString();
        if (string.IsNullOrEmpty(type))
        {
            return null;
        }

        var item = new ContractAbiItem
        {
            Type = type
        };

        if (element.TryGetProperty("name", out var nameElement))
            item.Name = nameElement.GetString();

        if (element.TryGetProperty("stateMutability", out var stateMutabilityElement))
            item.StateMutability = stateMutabilityElement.GetString();

        if (element.TryGetProperty("anonymous", out var anonymousElement))
            item.Anonymous = anonymousElement.GetBoolean();

        if (element.TryGetProperty("inputs", out var inputsElement))
            item.Inputs = ParseParameters(inputsElement);

        if (element.TryGetProperty("outputs", out var outputsElement))
            item.Outputs = ParseParameters(outputsElement);

        return item;
    }

    private static List<ContractAbiParameter> ParseParameters(JsonElement element)
    {
        var parameters = new List<ContractAbiParameter>();

        foreach (var param in element.EnumerateArray())
        {
            parameters.Add(new ContractAbiParameter
            {
                Name = param.GetProperty("name").GetString() ?? string.Empty,
                Type = param.GetProperty("type").GetString() ?? string.Empty,
                InternalType = param.TryGetProperty("internalType", out var internalType) ?
                    internalType.GetString() : null,
                Indexed = param.TryGetProperty("indexed", out var indexed) &&
                    indexed.GetBoolean(),
                Components = param.TryGetProperty("components", out var components) ?
                    ParseParameters(components) : null
            });
        }

        return parameters;
    }
}