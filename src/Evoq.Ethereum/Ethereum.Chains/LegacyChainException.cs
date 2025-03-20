using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Exception thrown when a chain is detected to be a legacy chain.
/// </summary>
[Serializable]
internal class LegacyChainException : Exception
{
    public LegacyChainException()
    {
    }

    public LegacyChainException(string message) : base(message)
    {
    }

    public LegacyChainException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected LegacyChainException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}