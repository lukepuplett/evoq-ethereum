
using System;
using System.Runtime.Serialization;

namespace Evoq.Blockchain;

[Serializable]
public class InvalidNonceException : Exception
{
    public InvalidNonceException(string message) : base(message)
    {
    }

    public InvalidNonceException(string message, Exception inner) : base(message, inner)
    {
    }

    protected InvalidNonceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}