using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Chains;

[Serializable]
internal class ChainRequestException : Exception
{
    public ChainRequestException()
    {
    }

    public ChainRequestException(string message) : base(message)
    {
    }

    public ChainRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected ChainRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}