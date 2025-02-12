using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum;

[Serializable]
public class FeeCalculationException : EthereumException
{
    public FeeCalculationException(string message) : base(message) { }

    public FeeCalculationException(string message, Exception innerException) : base(message, innerException) { }

    public FeeCalculationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
