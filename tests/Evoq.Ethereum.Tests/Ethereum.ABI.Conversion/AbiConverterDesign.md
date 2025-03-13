# ABI Converter Design

## Overview

The ABI Converter is a utility for converting between Ethereum ABI values and .NET POCOs (Plain Old CLR Objects). It provides a simple and flexible way to map ABI values to .NET objects, similar to how JSON deserialization works.

## Key Components

### 1. AbiConverter Class

The main class that provides static methods for converting between ABI values and .NET objects.

#### Methods:

- `DictionaryToObject<T>(dictionary)`: Converts a dictionary of values to a strongly-typed object.
- `ContractFunctionOutputToObject<T>(contractAbi, functionName, outputValues)`: Converts contract function output values to a strongly-typed object using the contract ABI.
- `FunctionOutputToObject<T>(signature, outputValues)`: Converts function output values to a strongly-typed object using the function signature.
- `TupleToObject<T>(tuple)`: Converts a tuple of values to a strongly-typed object.
- `ArrayToObject<T>(values)`: Converts an array of values to a strongly-typed object using positional mapping.

### 2. AbiParameterAttribute

An attribute for specifying ABI parameter mapping for a property.

#### Properties:

- `Name`: The name of the ABI parameter.
- `Position`: The position of the ABI parameter.
- `AbiType`: The ABI type of the parameter.
- `IsIndexed`: Whether the parameter is indexed (for events).
- `Ignore`: Whether the parameter should be ignored during conversion.

### 3. AbiParametersExtensions

Extension methods for AbiParameters to provide additional functionality.

#### Methods:

- `ToObject<T>(parameters)`: Converts ABI parameters to a strongly-typed object.
- `ToDictionary(parameters, forStringification)`: Converts ABI parameters to a dictionary.

## Mapping Strategies

The ABI Converter supports several mapping strategies:

1. **Name-based mapping**: Maps ABI values to properties with matching names.
2. **Position-based mapping**: Maps ABI values to properties based on their position.
3. **Attribute-based mapping**: Maps ABI values to properties based on attributes.

## Type Conversion

The ABI Converter handles conversion between ABI types and .NET types:

1. **Basic types**: Converts between ABI basic types (uint, int, bool, etc.) and .NET types.
2. **Complex types**: Converts between ABI complex types (tuples, arrays) and .NET types.
3. **Special types**: Handles special types like EthereumAddress, BigInteger, and byte arrays.

## Usage Examples

### Simple Mapping

```csharp
// Define a POCO
public class User
{
    public string Name { get; set; }
    public BigInteger Age { get; set; }
    public bool IsActive { get; set; }
}

// Convert from dictionary
var dictionary = new Dictionary<string, object?>
{
    { "Name", "John Doe" },
    { "Age", BigInteger.Parse("25") },
    { "IsActive", true }
};

var user = AbiConverter.DictionaryToObject<User>(dictionary);
```

### Attribute-based Mapping

```csharp
// Define a POCO with attributes
public class CustomUser
{
    [AbiParameter("username")]
    public string Name { get; set; }
    
    [AbiParameter("years")]
    public BigInteger Age { get; set; }
    
    [AbiParameter("wallet")]
    public EthereumAddress Address { get; set; }
}

// Convert from dictionary
var dictionary = new Dictionary<string, object?>
{
    { "username", "JaneDoe" },
    { "years", BigInteger.Parse("27") },
    { "wallet", "0xabcdef1234567890abcdef1234567890abcdef12" }
};

var user = AbiConverter.DictionaryToObject<CustomUser>(dictionary);
```

### Contract Function Output

```csharp
// Define a POCO
public class User
{
    public string Name { get; set; }
    public BigInteger Age { get; set; }
    public bool IsActive { get; set; }
}

// Get contract ABI
var contractAbi = new ContractAbi(/* ... */);

// Get function output values
var outputValues = new Dictionary<string, object?>
{
    { "name", "John Doe" },
    { "age", BigInteger.Parse("25") },
    { "isActive", true }
};

// Convert to POCO
var user = AbiConverter.ContractFunctionOutputToObject<User>(
    contractAbi, "getUserInfo", outputValues);
```

## Implementation Considerations

1. **Performance**: Use reflection efficiently to minimize overhead.
2. **Error Handling**: Provide clear error messages for mapping failures.
3. **Extensibility**: Allow for custom type converters.
4. **Compatibility**: Ensure compatibility with existing ABI types and encoders.
5. **Nullability**: Handle nullable types properly.

## Next Steps

1. Implement the `AbiConverter` class with the core conversion logic.
2. Implement the `AbiParameterAttribute` for attribute-based mapping.
3. Implement extension methods for `AbiParameters`.
4. Add comprehensive unit tests for all conversion scenarios.
5. Document the API and provide usage examples. 