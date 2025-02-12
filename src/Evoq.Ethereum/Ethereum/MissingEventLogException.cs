using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum;

[Serializable]
public class MissingEventLogException : EthereumException
{
    public MissingEventLogException(string message) : base(message) { }

    public MissingEventLogException(string message, Exception inner) : base(message, inner) { }

    public MissingEventLogException(SerializationInfo info, StreamingContext context) : base(info, context) { }


}
