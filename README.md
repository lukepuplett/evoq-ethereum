# Evoq.Ethereum

A lightweight .NET library focused on core Ethereum operations with minimal dependencies. This package provides essential utilities for:
- Creating and signing transactions
- Interacting with smart contracts
- Handling transaction receipts and events
- Managing accounts and nonces
- Estimating gas and fees

The library is designed to be simple and focused, with two main approaches for contract interaction:

1. **Chain and Contract Classes**: For type-safe contract interaction when you have an ABI file
   ```csharp
   var contract = chain.GetContract(contractAddress, endpoint, sender, abiStream);
   var context = new JsonRpcContext();
   await contract.InvokeMethodAsync(context, "transfer", nonce, options, args);
   ```

2. **RawContractCaller**: For direct contract calls when you want to specify ABI signatures manually
   ```csharp
   var caller = new RawContractCaller(endpoint);
   var context = new JsonRpcContext();
   await caller.CallAsync(context, contractAddress, "transfer(address,uint256)", args);
   ```

> **⚠️ Warning: This library is not audited or extensively tested in production environments.**
> 
> - Use this library at your own risk
> - Always test against your specific contracts to ensure values are not corrupted in transit
> - Verify gas estimates and transaction parameters before sending to mainnet
> - Consider using established libraries like Nethereum for production applications
> - This library is primarily intended for development and testing purposes

## Installation

```
dotnet add package Evoq.Ethereum
```

## Getting Started

Here's a minimal example to get you started with the library:

```csharp
using Evoq.Blockchain;
using Evoq.Ethereum;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

// Set up logging
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddSimpleConsole(options => options.SingleLine = true));

// Create a chain instance for Hardhat (local development)
var chain = Chain.CreateDefault(
    chainId: ulong.Parse(ChainNames.GetChainId(ChainNames.Hardhat)),
    rpcUrl: new Uri("http://localhost:8545"),
    loggerFactory: loggerFactory);

// Create an endpoint for contract interactions
var endpoint = new Endpoint("hardhat", "hardhat", "http://localhost:8545", loggerFactory);

// Create a sender account (using a test private key)
var senderAccount = new Account(Hex.Parse("0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
var sender = new Sender(senderAccount, new FileNonceStore());

// Create a transaction runner
var runner = new TransactionRunnerNative(sender, loggerFactory);

// Create a JSON-RPC context
var context = new JsonRpcContext();

// Example: Send 0.1 ETH to another address
var recipient = new EthereumAddress("0x1111111111111111111111111111111111111111");
var amount = EtherAmount.FromEther(0.1m);

// Estimate gas for the transaction
var estimate = await chain.EstimateTransactionFeeAsync(
    context,
    senderAccount.Address,
    recipient,
    amount,
    null);

// Create and send the transaction
var result = await runner.RunTransactionAsync(
    context,
    chain,
    recipient,
    estimate.ToSuggestedGasOptions(),
    amount,
    CancellationToken.None);

Console.WriteLine($"Transaction hash: {result.TransactionHash}");
```

This example demonstrates:
- Basic setup with logging
- Connecting to a local Hardhat node
- Creating a sender account
- Estimating gas for a transaction
- Sending ETH to another address

For more detailed examples, see the [Usage](#usage) section below.

## Features

- Ethereum-specific blockchain primitives
- Integration with Nethereum utilities
- Type-safe Ethereum data structures
- Simplified Web3 interactions

## Main Types and Usage

### Core Types

#### `EthereumAddress`
Represents an Ethereum address for an EOA or smart contract. Provides methods for address validation, checksum handling, and conversion between different formats.

```csharp
// Create an address from a hex string
var address = new EthereumAddress("0x742d35Cc6634C0532925a3b844Bc454e4438f44e");

// Check if address is zero address
bool isZero = address.IsZero;

// Get address in checksum format
string checksumAddress = address.ToString(); // Returns EIP-55 checksummed address
```

#### `EtherAmount`
Represents an amount of Ethereum currency with support for Wei and Ether units.

```csharp
// Create amounts in different units
var amountInWei = EtherAmount.FromWei(1000000000000000000); // 1 ETH
var amountInEther = EtherAmount.FromEther(1.5m); // 1.5 ETH

// Convert between units
decimal etherValue = amountInWei.ToEther();
BigInteger weiValue = amountInEther.ToWei();

// Format for display
string display = amountInEther.ToString(4); // "1.5000 ETH"
```

#### `Chain`
Represents a specific blockchain network and provides methods for interacting with it.

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

// Get current gas price
var gasPrice = await chain.GasPriceAsync(context);

// Get base fee (EIP-1559)
var baseFee = await chain.GetBaseFeeAsync(context);

// Estimate gas for ETH transfer
var transferGas = await chain.GetEthTransferGasAsync();
```

#### `Contract`
Represents a smart contract at a specific address on a chain.

```csharp
// Create a contract instance
var contract = new Contract(chainId, chainClient, contractClient, abiStream, contractAddress);

// Access contract address
var address = contract.Address;

// Get chain instance
var chain = contract.Chain;
```

### Transaction Types

#### `TransactionReceipt`
Contains details about a completed Ethereum transaction.

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

// Access transaction details
var receipt = await chain.GetTransactionReceiptAsync(context, txHash);

// Check transaction success
bool success = receipt.Success;

// Get gas used
var gasUsed = receipt.GasUsed;

// Get effective gas price (EIP-1559)
var effectiveGasPrice = receipt.EffectiveGasPrice;
```

### Message Signing

#### `PersonalSign`
Handles signing and verification of personal messages.

```csharp
// Create a personal sign instance
var personalSign = new PersonalSign("Hello Ethereum!", signer);

// Get signature
byte[] signature = personalSign.GetSignature();
```

### Constants and Utilities

#### `WeiAmounts`
Provides common Ethereum value constants and conversion utilities.

```csharp
// Common denominations
var oneEther = WeiAmounts.Ether;
var oneGwei = WeiAmounts.Gwei;

// Common gas limits
var transferGas = WeiAmounts.EthTransferGas;
var contractDeploymentGas = WeiAmounts.ContractDeploymentGas;

// Common gas prices
var lowPriorityFee = WeiAmounts.LowPriorityFee;
var urgentPriorityFee = WeiAmounts.UrgentPriorityFee;
```

### JSON-RPC Types

#### `JsonRpcClient`
Handles communication with Ethereum nodes using JSON-RPC.

```csharp
// Create a JSON-RPC client
var client = new JsonRpcClient(httpClient, logger);

// Create a JSON-RPC context
var context = new JsonRpcContext();

// Send requests
var response = await client.SendRequestAsync(context, method, parameters);
```

## Implementation Notes

### BigInteger Implementations

This library uses two different BigInteger implementations for different encoding schemes:

- **RLP Encoding**: Uses Bouncy Castle's `BigInteger` (`Org.BouncyCastle.Math.BigInteger`) which provides better handling of cryptographic values, particularly for variable-length encoding and handling of sign bits.

- **ABI Encoding**: Uses .NET's `System.Numerics.BigInteger` which works well with ABI's fixed-length 32-byte encoding format.

This design decision was made to optimize each encoding method for its specific requirements. In a future release, the library may be split into separate ABI and RLP components to better manage these dependencies.

## Target Frameworks

This package targets .NET Standard 2.1 for maximum compatibility across:
- .NET 6.0+
- .NET Framework 4.6.1+
- .NET Core 2.0+
- Xamarin
- Unity

## Dependencies

- Evoq.Blockchain (1.0.8)
- Evoq.Extensions (1.7.7)
- BouncyCastle.NetCore (2.2.1)
- System.Text.Json (8.0.5)
- Microsoft.Extensions.Http (8.0.0)

## Prerequisites and Configuration

### Required NuGet Packages

```bash
dotnet add package Evoq.Ethereum
dotnet add package Microsoft.Extensions.Http
```

Note: The following dependencies will be automatically included:
- Evoq.Blockchain
- Evoq.Extensions
- BouncyCastle.NetCore
- System.Text.Json

### Configuration Setup

Create an `appsettings.json` file in your project:

```json
{
  "Blockchain": {
    "Ethereum": {
      "JsonRPC": {
        "Hardhat": {
          "Url": "http://localhost:8545"
        }
      },
      "Addresses": {
        "Hardhat1Address": "0x1111111111111111111111111111111111111111",
        "Hardhat1PrivateKey": "0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
      }
    }
  }
}
```

### Logging Setup

Configure logging in your application:

```csharp
// Create logger factory with console output
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddSimpleConsole(options => options.SingleLine = true)
           .SetMinimumLevel(LogLevel.Debug));

// Optional: Add file logging
builder.AddFile("logs/evoq-ethereum-{Date}.txt");
```

### Network Configuration

The library supports various Ethereum networks. Here are the common configurations:

```csharp
// Hardhat (Local Development)
var hardhatConfig = new
{
    ChainId = ulong.Parse(ChainNames.GetChainId(ChainNames.Hardhat)),
    Url = "http://localhost:8545"
};

// Sepolia (Testnet)
var sepoliaConfig = new
{
    ChainId = ulong.Parse(ChainNames.GetChainId(ChainNames.Sepolia)),
    Url = "https://sepolia.infura.io/v3/YOUR-PROJECT-ID"
};

// Mainnet
var mainnetConfig = new
{
    ChainId = ulong.Parse(ChainNames.GetChainId(ChainNames.Mainnet)),
    Url = "https://mainnet.infura.io/v3/YOUR-PROJECT-ID"
};
```

### Environment Variables

For production environments, you can use environment variables instead of appsettings.json:

```bash
# Network Configuration
BLOCKCHAIN__ETHEREUM__JSONRPC__HARDHAT__URL=http://localhost:8545

# Account Configuration
BLOCKCHAIN__ETHEREUM__ADDRESSES__HARDHAT1ADDRESS=0x1111111111111111111111111111111111111111
BLOCKCHAIN__ETHEREUM__ADDRESSES__HARDHAT1PRIVATEKEY=0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
```

### Security Considerations

1. Never commit private keys to source control
2. Use environment variables or secure key management in production
3. Consider using Azure Key Vault or similar services for production environments
4. Use test accounts and test networks for development

## Examples

The library includes several example implementations in the `tests/Evoq.Ethereum.Tests/Ethereum.Examples` directory. These examples demonstrate common use cases and best practices.

### Running Examples Locally

To run the examples locally:

1. Start a local Hardhat node:
```bash
pnpm hardhat node
```

2. Deploy the required contracts (if any):
```bash
# For EAS examples
npx hardhat ignition deploy ./ignition/modules/eas.ts --network localhost

# For ERC-20 examples
# The examples use the DAI token contract on mainnet, but you can modify the address
# to use a local token contract for testing
```

3. Run the examples using the test runner:
```bash
dotnet test --filter "FullyQualifiedName~Evoq.Ethereum.Examples"
```

### Setting Up EAS for Local Development

The EAS examples require the Ethereum Attestation Service contracts to be deployed locally. Here's how to set it up:

1. Clone the EAS contracts repository:
```bash
git clone https://github.com/ethereum-attestation-service/eas-contracts.git
cd eas-contracts
pnpm install
```

2. Install Hardhat Ignition:
```bash
pnpm add --dev @nomicfoundation/hardhat-ignition
```

3. Create an Ignition deployment script:
```bash
mkdir -p ignition/modules
```

Create a file at `ignition/modules/eas.ts` with the following content:
```typescript
import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

export default buildModule("EASDeployment", (m) => {
    // Deploy SchemaRegistry first (no constructor args needed)
    const schemaRegistry = m.contract("SchemaRegistry");

    // Deploy EAS with SchemaRegistry address
    const eas = m.contract("EAS", [schemaRegistry]);

    // Return the contract instances
    return {
        schemaRegistry,
        eas,
    };
});
```

4. Update your `hardhat.config.ts` to include Ignition:
```typescript
import "@nomicfoundation/hardhat-ignition";
```

5. Deploy the contracts to your local Hardhat node:
```bash
npx hardhat ignition deploy ./ignition/modules/eas.ts --network localhost
```

After deployment, you'll see output with the deployed contract addresses. The SchemaRegistry will be deployed first, followed by the EAS contract that references it.

### Available Examples

#### ERC-20 Token Operations

The `ExampleERC20.cs` demonstrates common ERC-20 token operations:

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

// Simple token transfer
var transferAmount = EtherAmount.FromWei(1_000_000_000_000_000_000); // 1 DAI
var estimate = await contract.EstimateTransactionFeeAsync(
    context,
    "transfer",
    senderAddress,
    null,
    AbiKeyValues.Create("to", recipientAddress, "amount", transferAmount));

// Create transaction options
var options = new ContractInvocationOptions(estimate.ToSuggestedGasOptions(), EtherAmount.Zero);

// Send the transfer transaction
var result = await runner.RunTransactionAsync(
    context,
    contract,
    "transfer",
    options,
    AbiKeyValues.Create("to", recipientAddress, "amount", transferAmount),
    CancellationToken.None);

// Get the transaction receipt
var receipt = await chain.GetTransactionReceiptAsync(context, result.TransactionHash);

// Try to read the event from the receipt
if (receipt.TryReadEventLogs(contract, "Transfer", out var indexed, out var data))
{
    // Access the decoded event data
    var fromAddress = (EthereumAddress)indexed!["from"]!;
    var toAddress = (EthereumAddress)indexed!["to"]!;
    var value = (BigInteger)data!["value"]!;

    Console.WriteLine($"Transfer successful: {value} tokens from {fromAddress} to {toAddress}");
}

// Approve and transferFrom pattern
var approveAmount = EtherAmount.FromWei(1_000_000_000_000_000_000); // 1 DAI
await contract.InvokeMethodAsync(
    context,
    "approve",
    nonce,
    options,
    AbiKeyValues.Create("spender", spenderAddress, "amount", approveAmount));
```

Key features demonstrated:
- Direct token transfers
- Approve and transferFrom operations
- Event handling for Transfer and Approval events
- Balance checking
- Gas estimation for token operations

#### Ethereum Attestation Service (EAS)

The `ExampleEAS.cs` shows how to interact with the Ethereum Attestation Service:

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

// Register a schema
var registerArgs = AbiKeyValues.Create(
    ("schema", "bool"),
    ("resolver", EthereumAddress.Zero),
    ("revocable", true));

var registerReceipt = await runner.RunTransactionAsync(
    context,
    schemaRegistry,
    "register",
    registerOptions,
    registerArgs,
    CancellationToken.None);
```

Key features demonstrated:
- Schema registration
- Schema retrieval
- Event handling
- Gas estimation for contract operations

### Example Structure

Each example follows a consistent structure:
1. Setup of chain, endpoint, and logging
2. Account management
3. Contract initialization
4. Transaction preparation and execution
5. Event handling and verification
6. Error handling

## Usage

### Basic Setup

First, set up your chain and endpoint:

```csharp
// Create a chain instance (e.g., for Hardhat local network)
var chain = Chain.CreateDefault(chainId, new Uri("http://localhost:8545"), loggerFactory);

// Create an endpoint for contract interactions
var endpoint = new Endpoint("hardhat", "hardhat", "http://localhost:8545", loggerFactory);

// Create a JSON-RPC context
var context = new JsonRpcContext();
```

### Sending ETH

The library provides two main ways to send ETH:

1. Using `TransferRunnerNative` (Recommended):
```csharp
// Create a sender account
var sender = new Sender(senderAccount, nonceStore);

// Create a transfer runner
var runner = TransferRunnerNative.CreateDefault(endpoint, sender);

// Create a JSON-RPC context
var context = new JsonRpcContext();

// Configure gas options (EIP-1559)
var gasOptions = new EIP1559GasOptions(
    maxPriorityFeePerGas: WeiAmounts.LowPriorityFee,
    maxFeePerGas: WeiAmounts.UrgentPriorityFee,
    gasLimit: WeiAmounts.EthTransferGas
);

// Send ETH
var amount = EtherAmount.FromEther(0.1m); // 0.1 ETH
var receipt = await runner.RunTransferAsync(
    context,
    new TransferInvocationOptions(gasOptions, amount, recipientAddress),
    null);

// Verify transaction success
Assert.IsNotNull(receipt);
Assert.IsTrue(receipt.Success);
```

2. Using `TransactionRunnerNative` directly:
```csharp
// Create a sender account
var sender = new Sender(senderAccount, nonceStore);

// Create a transaction runner
var runner = new TransactionRunnerNative(sender, loggerFactory);

// Create a JSON-RPC context
var context = new JsonRpcContext();

// Estimate gas for the transaction
var estimate = await chain.EstimateTransactionFeeAsync(
    context,
    senderAccount.Address,
    recipientAddress,
    amount,
    null);

// Send ETH
var result = await runner.RunTransactionAsync(
    context,
    chain,
    recipientAddress,
    estimate.ToSuggestedGasOptions(),
    amount,
    CancellationToken.None);

// Get transaction receipt
var receipt = await chain.GetTransactionReceiptAsync(context, result.TransactionHash);
```

Key points about ETH transfers:
- The library supports both legacy and EIP-1559 transactions
- Gas estimation is handled automatically with `TransferRunnerNative`
- Nonce management is handled by the `INonceStore` implementation
- Transaction receipts provide confirmation of successful transfers
- The library handles retries for common failure scenarios (nonce issues, out of gas, etc.)

For more examples, see the test files in the `tests/Evoq.Ethereum.Tests/Ethereum.Examples` directory.

### Contract Interaction

There are two main ways to interact with contracts:

#### 1. Using the Contract Class (Recommended)

This approach provides type-safe contract interaction with ABI support:

```csharp
// Get the contract ABI
var abiStream = AbiFileHelper.GetAbiStream("YourContract.abi.json");
var contractAddress = new EthereumAddress("0x2222222222222222222222222222222222222222");

// Create a contract instance
var contract = chain.GetContract(contractAddress, endpoint, sender, abiStream);

// Create a JSON-RPC context
var context = new JsonRpcContext();

// Estimate gas for a transaction
var estimate = await contract.EstimateTransactionFeeAsync(
    context,
    "yourMethod",
    senderAddress,
    null,
    AbiKeyValues.Create("param1", "value1", "param2", "value2"));

// Create transaction options
var options = new ContractInvocationOptions(estimate.ToSuggestedGasOptions(), EtherAmount.Zero);

// Call the contract method
var result = await runner.RunTransactionAsync(
    context,
    contract,
    "yourMethod",
    options,
    AbiKeyValues.Create("param1", "value1", "param2", "value2"),
    CancellationToken.None);
```

For real-world examples, see the test files in the `tests/Evoq.Ethereum.Tests/Ethereum.Examples` directory:
- `ExampleERC20.cs` - Demonstrates ERC-20 token transfers and approvals
- `ExampleEAS.cs` - Shows interaction with the Ethereum Attestation Service

The ERC-20 example includes:
- Simple token transfers
- Approve and transferFrom operations
- Event handling for Transfer and Approval events
- Balance checking
- Gas estimation for token operations

#### 2. Using RawContractCaller

For simpler cases or when you don't have the ABI, use the RawContractCaller. It supports both named and positional parameters:

```csharp
// Create a raw contract caller
var caller = new RawContractCaller(endpoint);

// Create a JSON-RPC context
var context = new JsonRpcContext();

// Example with named parameters in the signature
var resultNamed = await caller.CallAsync(
    context,
    contractAddress,
    "transfer(address to,uint256 amount)",  // Named parameters in signature
    ("to", recipientAddress),
    ("amount", amountInWei));

// Example with positional parameters (no names in signature)
var resultPositional = await caller.CallAsync(
    context,
    contractAddress,
    "transfer(address,uint256)",  // No parameter names in signature
    ("0", recipientAddress),      // Use "0" for first parameter
    ("1", amountInWei));         // Use "1" for second parameter
```

When using RawContractCaller:
- If the function signature includes parameter names (e.g., `transfer(address to,uint256 amount)`), use those names when providing values
- If the function signature doesn't include parameter names (e.g., `transfer(address,uint256)`), use "0", "1", etc. as parameter names to indicate position
- The function signature should follow standard Solidity format

#### 3. Handling Complex Return Types

When working with contract methods that return complex types like tuples (Solidity structs), special care is needed in decoding the results, especially when using `RawContractCaller`. Here's an example using EAS's `getSchema` function:

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

// 1. Make the call to get the raw bytes
var caller = new RawContractCaller(endpoint);
var returnedHex = await caller.CallAsync(
    context,
    schemaRegistry.Address, 
    "getSchema(bytes32 uid)", 
    ("uid", schemaUid)
);

// 2. Define the return type signature
// Note: We're defining a single tuple named "record" that matches the SchemaRecord struct
var returnParams = AbiParameters.Parse(
    "((bytes32 uid, address resolver, bool revocable, string schema) record)"
);

// 3. Decode the result
var decoder = new AbiDecoder();
var decodedResult = decoder.DecodeParameters(returnParams, returnedHex);

// 4. Access the decoded data
// The result contains a single parameter named "record" containing the tuple
if (decodedResult.Parameters.TryFirst(out var first))
{
    // The tuple is represented as a dictionary
    var record = first.Value as IDictionary<string, object?>;
    
    // Access individual fields
    var uid = record["uid"];
    var schema = record["schema"];
    var resolver = record["resolver"];
    var revocable = record["revocable"];
}
```

Key points about decoding tuples:
1. When a function returns a single tuple, it's still treated as a parameter (we name it "record" in this case)
2. The return signature uses double parentheses: outer ones for the parameter list, inner ones for the tuple structure
3. Field names in the tuple can be chosen arbitrarily, but it's best to match the contract's struct field names
4. The decoded tuple is represented as a dictionary with string keys matching the field names
5. Each field in the dictionary will have the appropriate type based on its Solidity type

This pattern is particularly useful when:
- Working with contract methods that return structs
- Using `RawContractCaller` without full ABI information
- Need to decode complex return types manually

#### 4. Decoding Events from Transaction Receipts

When working with contract events, you can decode them from transaction receipts using the `Contract` class. Here's how to decode events:

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

// Get a transaction receipt
var receipt = await chain.GetTransactionReceiptAsync(context, txHash);

// Try to read the event from the receipt
if (receipt.TryReadEventLogs(contract, "Transfer", out var indexed, out var data))
{
    // Access the decoded event data
    var fromAddress = (EthereumAddress)indexed!["from"]!;
    var toAddress = (EthereumAddress)indexed!["to"]!;
    var value = (BigInteger)data!["value"]!;

    Console.WriteLine($"Transfer: {value} tokens from {fromAddress} to {toAddress}");
}
```

Key points about event decoding:
1. Events can have both indexed and non-indexed parameters
2. Indexed parameters are stored in the `indexed` dictionary
3. Non-indexed parameters are stored in the `data` dictionary
4. The event name must match exactly with the contract's event definition
5. `TryReadEventLogs` returns false if no matching event is found in the receipt

Common event names:
```csharp
// ERC-20 events
"Transfer"    // Transfer(address indexed from,address indexed to,uint256 value)
"Approval"    // Approval(address indexed owner,address indexed spender,uint256 value)

// ERC-721 events
"Transfer"    // Transfer(address indexed from,address indexed to,uint256 indexed tokenId)
"Approval"    // Approval(address indexed owner,address indexed approved,uint256 indexed tokenId)
"ApprovalForAll" // ApprovalForAll(address indexed owner,address indexed operator,bool approved)
```

When using RawContractCaller:
- If the function signature includes parameter names (e.g., `transfer(address to,uint256 amount)`), use those names when providing values
- If the function signature doesn't include parameter names (e.g., `transfer(address,uint256)`), use "0", "1", etc. as parameter names to indicate position
- The function signature should follow standard Solidity format

### Working with EIP-165 Interfaces

The library includes support for EIP-165 interface detection:

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

// Create an EIP-165 checker
var eip165 = new EIP165Native(endpoint);

// Check if a contract supports an interface
var supportsInterface = await eip165.SupportsInterface(
    context,
    contractAddress,
    Hex.Parse("0x...") // Interface ID
);
```

### Transaction Management

For transactions that require signing:

```csharp
// Set up a sender account
var sender = new Sender(senderAccount, nonceStore);

// Create a transaction runner
var runner = new TransactionRunnerNative(sender, loggerFactory);

// Create a JSON-RPC context
var context = new JsonRpcContext();

// Run a transaction
var result = await runner.RunTransactionAsync(
    contract,
    "methodName",
    options,
    arguments,
    CancellationToken.None);
```

#### Nonce Management

The `TransactionRunnerNative` works in collaboration with the `INonceStore` to manage transaction nonces. This is crucial for:

- Preventing transaction replay attacks
- Ensuring transactions are processed in the correct order
- Handling transaction failures and retries
- Detecting and handling gaps in the nonce sequence

The nonce store provides several key operations:

```csharp
public interface INonceStore
{
    // Reserve the next nonce for immediate use
    Task<uint> BeforeSubmissionAsync();

    // Handle various failure scenarios
    Task<NonceRollbackResponse> AfterSubmissionFailureAsync(uint nonce);
    Task<NonceRollbackResponse> AfterTransactionRevertedAsync(uint nonce);
    Task<NonceRollbackResponse> AfterTransactionOutOfGas(uint nonce);
    
    // Handle success
    Task AfterSubmissionSuccessAsync(uint nonce);
    
    // Handle nonce too low errors
    Task<uint> AfterNonceTooLowAsync(uint nonce);
}
```

The `NonceRollbackResponse` enum indicates what action should be taken:

```csharp
public enum NonceRollbackResponse
{
    NonceNotFound,        // Nonce record was not found
    RemovedOkay,          // Nonce removed, no gap detected
    RemovedGapDetected,   // Nonce removed, gap detected
    NotRemovedShouldRetry, // Keep nonce and retry transaction
    NotRemovedDueToError,  // Nonce removal failed due to error
    NotRemovedGasSpent     // Nonce kept because gas was spent
}
```

The library provides two built-in implementations:

1. `InMemoryNonceStore`: A simple in-memory store using a HashSet. Suitable for testing and single-instance applications.
   - Uses a HashSet to track nonces
   - Handles retries with a 10-second cooldown
   - Detects gaps in the nonce sequence
   - Thread-safe with lock-based synchronization

2. `FileSystemNonceStore`: A file-based store that persists nonces to disk. Better for development and testing.
   - Stores each nonce as a file in a specified directory
   - Handles file system concurrency
   - Supports external nonce synchronization
   - Includes failure tracking with timestamps
   - Detects and handles gaps in the nonce sequence

For production environments, consider implementing a more robust solution that:
- Persists nonces across application restarts
- Handles concurrent access safely
- Provides transaction rollback capabilities
- Maintains an audit trail of nonce usage
- Properly handles gaps in the nonce sequence

### Gas Estimation

The library provides detailed gas estimation with EIP-1559 support:

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

var estimate = await contract.EstimateTransactionFeeAsync(
    context,
    "methodName",
    senderAddress,
    null,
    arguments);

// Access fee components
Console.WriteLine($"Gas Limit: {estimate.EstimatedGasLimit}");
Console.WriteLine($"Max Fee: {estimate.SuggestedMaxFeePerGas}");
Console.WriteLine($"Priority Fee: {estimate.SuggestedMaxPriorityFeePerGas}");
Console.WriteLine($"Base Fee: {estimate.CurrentBaseFeePerGas}");
```

### Error Handling

The library provides comprehensive error handling:

```csharp
// Create a JSON-RPC context
var context = new JsonRpcContext();

try
{
    var result = await contract.InvokeMethodAsync(context, "methodName", nonce, options, arguments);
}
catch (Exception ex)
{
    // Handle specific contract errors
    if (ex is RevertedTransactionException revertEx)
    {
        // Handle contract revert with custom error
        Console.WriteLine($"Contract reverted: {revertEx.Message}");
        // Example output:
        // Contract reverted: JSON-RPC error: -32603 - Error: VM Exception while processing transaction: reverted with custom error 'AlreadyExists()'
    }
    else if (ex is ContractRevertException contractEx)
    {
        // Handle contract revert with custom error
        Console.WriteLine($"Contract reverted: {contractEx.Message}");
    }
    else
    {
        // Handle other errors
        Console.WriteLine($"Error: {ex.Message}");
    }
}
```

The library provides detailed error information to help you:
1. Identify the exact reason for transaction failures
2. Handle specific contract error cases in your code
3. Debug contract interactions more effectively

Here's an example of what a contract revert error looks like in practice:

```text
[Failed] ExampleEAS_Send
    Message:
        Test method Evoq.Ethereum.Examples.ExampleEAS.ExampleEAS_Send threw exception: 
        Evoq.Ethereum.Transactions.RevertedTransactionException: JSON-RPC error: -32603 - Error: VM Exception while processing transaction: reverted with custom error 'AlreadyExists()'
    Stack Trace:
            at Evoq.Ethereum.Contracts.ContractClient.EstimateGasAsync(Contract contract, String methodName, EthereumAddress senderAddress, Nullable`1 value, IDictionary`2 arguments, CancellationToken cancellationToken) in /Users/lukepuplett/Git/Hub/evoq-ethereum/src/Evoq.Ethereum/Ethereum.Contracts/ContractClient.cs:line 207
           at Evoq.Ethereum.Contracts.Contract.EstimateGasAsync(String methodName, EthereumAddress senderAddress, Nullable`1 value, IDictionary`2 arguments, CancellationToken cancellationToken) in /Users/lukepuplett/Git/Hub/evoq-ethereum/src/Evoq.Ethereum/Ethereum.Contracts/Contract.cs:line 149
           at Evoq.Ethereum.Contracts.Contract.EstimateTransactionFeeAsync(String methodName, EthereumAddress senderAddress, Nullable`1 value, IDictionary`2 arguments, CancellationToken cancellationToken) in /Users/lukepuplett/Git/Hub/evoq-ethereum/src/Evoq.Ethereum/Ethereum.Contracts/Contract.cs:line 173
           at Evoq.Ethereum.Examples.ExampleEAS.ExampleEAS_Send() in /Users/lukepuplett/Git/Hub/evoq-ethereum/tests/Evoq.Ethereum.Tests/Ethereum.Examples/ExampleEAS.cs:line 56
    Standard Output Messages:
        dbug: JsonRpcProviderCaller[0] Extracted request ID: 637243567
        dbug: JsonRpcProviderCaller[0] Using timeout of 90 seconds
        dbug: JsonRpcProviderCaller[0] Attempt 1 for JSON-RPC method eth_estimateGas (ID: 637243567)
        dbug: JsonRpcProviderCaller[0] Added compression headers to request
        dbug: JsonRpcProviderCaller[0] Sending request to http://localhost:8545/
        warn: JsonRpcProviderCaller[0] JSON-RPC Error | Method: eth_estimateGas | Code: -32603 | Message: Error: VM Exception while processing transaction: reverted with custom error 'AlreadyExists()'
        fail: JsonRpcProviderCaller[0] JSON-RPC Error | Method: eth_estimateGas | Code: -32603 | Message: Error: VM Exception while processing transaction: reverted with custom error 'AlreadyExists()'

```

For more examples, see the test files in the `tests/Evoq.Ethereum.Tests/Ethereum.Examples` directory.

## To Do List

### API Validation and Examples
- [x] Create sample wallet application (create/sign/submit transactions)
  - Implemented with full EIP-1559 support
  - Includes gas estimation and fee calculation
  - Supports both legacy and modern transaction types
- [x] Create smart contract interaction example (deploy and call methods)
  - Implemented with EAS Schema Registry example
  - Shows contract method calls and parameter handling
  - Includes gas estimation for contract calls
- [x] Create ERC-20 token transfer example
  - Implemented in ExampleERC20.cs
  - Shows direct transfers and approve/transferFrom pattern
  - Includes event handling and balance checking
  - Demonstrates gas estimation for token operations
- [ ] Create NFT minting example (ERC-721)
  - Infrastructure exists (gas limits defined)
  - Need to add example implementation
- [x] Develop integration tests with test networks (Sepolia)
  - Implemented with Hardhat test network
  - Configuration for different networks exists
- [ ] Create CLI tool for common Ethereum operations
  - Need to implement command-line interface
  - Should support common operations like transfers and contract interactions

### API Design Improvements
- [x] Review and refine public API surface
  - Well-structured interfaces and classes
  - Clear separation of concerns
- [x] Mark appropriate implementation classes as internal
  - Good encapsulation of implementation details
  - Clear public API boundaries
- [x] Create helper methods for common workflows
  - Transaction creation and signing helpers
  - Gas estimation and fee calculation utilities
- [x] Improve error handling and messages
  - Comprehensive transaction validation
  - Detailed contract interaction errors
- [x] Add comprehensive XML documentation
  - Extensive parameter descriptions
  - Usage examples in code

### Core Functionality Testing
- [x] Transaction creation, signing, and submission
  - Support for legacy and EIP-1559 transactions
  - Comprehensive transaction handling
- [x] Gas estimation
  - Detailed fee calculation with base fee and priority fee
  - Support for EIP-1559 fee market
- [x] Contract deployment
  - Support for contract creation transactions
  - Appropriate gas limits
- [x] Contract method calls (read and write)
  - Type-safe parameter handling
  - Support for both read and write operations
- [x] Event log parsing
  - Transaction receipt handling
  - Log and bloom filter support
- [x] ABI encoding/decoding
  - Type-safe parameter handling
  - Comprehensive encoding/decoding support
- [x] RLP encoding/decoding
  - Support for all transaction types
  - Comprehensive test cases
- [x] Address validation and formatting
  - Proper validation and formatting
  - Type-safe address handling
- [x] Private key management
  - Secure key handling
  - Account management
- [x] Nonce management
  - Implemented InMemoryNonceStore for testing
  - Implemented FileSystemNonceStore for development
  - Comprehensive gap detection and handling
  - Retry mechanism with cooldown
  - Thread-safe implementations
- [x] Fee estimation (EIP-1559)
  - Base fee and priority fee calculations
  - Support for fee market

### Documentation
- [x] Add usage examples to README
  - Added comprehensive examples for basic setup
  - Added contract interaction examples
  - Added nonce management documentation
  - Added gas estimation examples
- [ ] Create documentation site
  - Need to set up documentation hosting
  - Should include API reference and guides
- [ ] Add code samples for common tasks
  - Need to add examples for ERC-20 and ERC-721 operations
  - Include more contract interaction examples
- [ ] Document architecture decisions
  - Need to document design choices
  - Explain transaction type implementations
  - Document fee calculation approach
  - Document nonce management strategy

## Building

Testing depends on Golang executables which are included in the repository along with their source code:

1. **gabi** - Used to generate ABI encoding using go-ethereum and serves to test the .NET ABI encoding against the go-ethereum ABI encoding.

2. **grlp** - Used to generate RLP encoding using go-ethereum and serves to test the .NET RLP encoding against the go-ethereum RLP encoding.

Both executables were compiled for macOS on an M chip. They may need to be recompiled for other platforms.

**Note** - The tests do not call these executables directly. Instead, they are used to generate the expected output which is embedded into the test code as the expected output string.

From the folder containing the solution file:

```bash
dotnet build
dotnet test
```

### Test Rigs

The repository includes Go-based test rigs that generate reference encodings from the go-ethereum implementation:

- **gabi** - For ABI encoding reference outputs
- **grlp** - For RLP encoding reference outputs

For detailed information about the grlp test rig, including usage instructions and test cases, see the [Go RLP Encoder Test Rig README](path/to/grlp/README.md).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Local Development with Hardhat and EAS

### Setting Up Ethereum Attestation Service (EAS)

For local testing and development, this project can be integrated with the [Ethereum Attestation Service (EAS)](https://attest.sh/). The following steps outline how to set up a local environment with EAS contracts deployed to a Hardhat node:

1. Clone the EAS repository from GitHub
2. Install the Hardhat Ignition extension (EAS uses Hardhat, not Foundry)
3. Add a custom deployment script
4. Run a local Hardhat node
5. Deploy the EAS contracts to your local node

#### Prerequisites

- Node.js (v16+)
- pnpm (EAS uses pnpm as its package manager)
- Git

#### Step 1: Clone the EAS Repository

```bash
git clone https://github.com/ethereum-attestation-service/eas-contracts.git
cd eas-contracts
pnpm install
```

#### Step 2: Install Hardhat Ignition

```bash
pnpm add --dev @nomicfoundation/hardhat-ignition
```

Update your `hardhat.config.js` or `hardhat.config.ts` to include the Ignition plugin.

#### Step 3: Create Ignition Deployment Script

Create a directory for your Ignition modules and add a deployment script:

```bash
mkdir -p ignition/modules
```

Create a file at `ignition/modules/eas.ts` with the following content:
```typescript
import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

export default buildModule("EASDeployment", (m) => {
    // Deploy SchemaRegistry first (no constructor args needed)
    const schemaRegistry = m.contract("SchemaRegistry");

    // Deploy EAS with SchemaRegistry address
    const eas = m.contract("EAS", [schemaRegistry]);

    // Return the contract instances
    return {
        schemaRegistry,
        eas,
    };
});
```

#### Step 4: Run a Local Hardhat Node

In a separate terminal window, start a local Hardhat node:

```bash
pnpm hardhat node
```

This will spin up a local Ethereum node with several pre-funded accounts for testing.

#### Step 5: Deploy EAS Contracts

With your local node running, deploy the EAS contracts:

```bash
npx hardhat ignition deploy ./ignition/modules/eas.ts --network localhost
```

After successful deployment, you'll see output similar to:

## Compatibility Note: BouncyCastle BigInteger.Equals

This library uses a custom internal helper method (`Signing.Equals`) for comparing `Org.BouncyCastle.Math.BigInteger` instances instead of the built-in `BigInteger.Equals(BigInteger)` method. This is a deliberate workaround to address potential runtime issues caused by conflicting BouncyCastle versions.

**The Problem:**

- This library depends on a newer BouncyCastle package (e.g., `BouncyCastle.NetCore` 2.2.1) that includes the `BigInteger.Equals(BigInteger)` overload.
- Consuming applications might transitively depend on older BouncyCastle versions (e.g., 1.8.5.0 via `Portable.BouncyCastle` or `BouncyCastle.Crypto`) which *lack* this specific overload.
- Standard .NET assembly binding redirects often fail to force the runtime to load the newer version or correctly redirect the call in these scenarios, leading to `System.MissingMethodException` at runtime.

**The Solution:**

To ensure broader compatibility across different application environments, the library avoids the problematic `Equals(BigInteger)` call and instead uses `CompareTo(other) == 0`, which is available in both old and new BouncyCastle versions. The `Signing.Equals` method encapsulates this logic.

This workaround prevents runtime crashes in environments affected by this dependency conflict.