using System;
using System.Numerics;

namespace Evoq.Ethereum.Contracts;

/// <summary>
/// Options for invoking a contract method.
/// </summary>
public class ContractInvocationOptions
{
    /// <summary>
    /// Initializes a new instance of the ContractInvocationOptions class.
    /// </summary>
    /// <param name="gas">Gas pricing and limit configuration for the transaction</param>
    /// <param name="value">Amount of ETH (in wei) to send with the transaction</param>
    public ContractInvocationOptions(GasOptions gas, EtherAmount value)
    {
        this.Gas = gas;
        this.Value = value;
    }

    /// <summary>
    /// Gas pricing and limit configuration for the transaction
    /// </summary>
    public GasOptions Gas { get; }

    /// <summary>
    /// Amount of ETH (in wei) to send with the transaction
    /// </summary>
    public EtherAmount Value { get; }

    /// <summary>
    /// The amount of time to wait for the transaction to be mined.
    /// </summary>
    public TimeSpan WaitForReceiptTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Options for the gas price of a transaction.
/// </summary>
public abstract class GasOptions
{
    /// <summary>
    /// Initializes a new instance of the GasOptions class. 
    /// </summary>
    /// <param name="gasLimit">Maximum gas units the transaction can consume</param>
    protected GasOptions(ulong gasLimit)
    {
        GasLimit = gasLimit;
    }

    /// <summary>
    /// Maximum gas units the transaction can consume
    /// </summary>
    public ulong GasLimit { get; }
}

/// <summary>
/// Options for the gas price of a legacy transaction.
/// </summary>
public class LegacyGasOptions : GasOptions
{
    /// <summary>
    /// Initializes a new instance of the LegacyGasOptions class.
    /// </summary>
    /// <param name="gasLimit">Maximum gas units the transaction can consume</param>
    /// <param name="price">Price per gas unit in wei (higher = faster processing)</param>
    public LegacyGasOptions(ulong gasLimit, BigInteger price) : base(gasLimit)
    {
        Price = price;
    }

    /// <summary>
    /// Price per gas unit in wei (higher = faster processing)
    /// </summary>
    public BigInteger Price { get; }
}

/// <summary>
/// Options for the gas price of an EIP-1559 transaction.
/// </summary>
public class EIP1559GasOptions : GasOptions
{
    /// <summary>
    /// Initializes a new instance of the EIP1559GasOptions class.
    /// </summary>
    /// <param name="gasLimit">Maximum gas units the transaction can consume</param>
    /// <param name="maxFeePerGas">Maximum total fee per gas unit in wei (base fee + priority fee)</param>
    /// <param name="maxPriorityFeePerGas">Maximum tip to miners per gas unit in wei</param>
    public EIP1559GasOptions(ulong gasLimit, EtherAmount maxFeePerGas, EtherAmount maxPriorityFeePerGas) : base(gasLimit)
    {
        MaxFeePerGas = maxFeePerGas;
        MaxPriorityFeePerGas = maxPriorityFeePerGas;
    }

    /// <summary>
    /// Maximum total fee per gas unit in wei (base fee + priority fee)
    /// </summary>
    public EtherAmount MaxFeePerGas { get; }

    /// <summary>
    /// Maximum tip to miners per gas unit in wei
    /// </summary>
    public EtherAmount MaxPriorityFeePerGas { get; }
}