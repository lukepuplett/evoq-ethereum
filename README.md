# Evoq.Ethereum

A lightweight .NET library providing Ethereum-specific utilities and extensions. This package builds upon Evoq.Blockchain and Nethereum to provide a simplified interface for Ethereum blockchain operations.

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

// Example: Send 0.1 ETH to another address
var recipient = new EthereumAddress("0x1111111111111111111111111111111111111111");
var amount = EtherAmount.FromEther(0.1m);

// Estimate gas for the transaction
var estimate = await chain.EstimateTransactionFeeAsync(
    senderAccount.Address,
    recipient,
    amount,
    null);

// Create and send the transaction
var result = await runner.RunTransactionAsync(
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

## Usage

### Basic Setup

First, set up your chain and endpoint:

```csharp
// Create a chain instance (e.g., for Hardhat local network)
var chain = Chain.CreateDefault(chainId, new Uri("http://localhost:8545"), loggerFactory);

// Create an endpoint for contract interactions
var endpoint = new Endpoint("hardhat", "hardhat", "http://localhost:8545", loggerFactory);
```

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

// Estimate gas for a transaction
var estimate = await contract.EstimateTransactionFeeAsync(
    "yourMethod",
    senderAddress,
    null,
    AbiKeyValues.Create("param1", "value1", "param2", "value2"));

// Create transaction options
var options = new ContractInvocationOptions(estimate.ToSuggestedGasOptions(), EtherAmount.Zero);

// Call the contract method
var result = await runner.RunTransactionAsync(
    contract,
    "yourMethod",
    options,
    AbiKeyValues.Create("param1", "value1", "param2", "value2"),
    CancellationToken.None);
```

#### 2. Using RawContractCaller

For simpler cases or when you don't have the ABI, use the RawContractCaller:

```csharp
// Create a raw contract caller
var caller = new RawContractCaller(endpoint);

// Example recipient address
var recipientAddress = new EthereumAddress("0x3333333333333333333333333333333333333333");

// Call a contract method using its signature
var result = await caller.SimpleCall(
    contractAddress,
    "transfer(address,uint256)",
    ("to", recipientAddress),
    ("amount", amountInWei));
```

### Working with EIP-165 Interfaces

The library includes support for EIP-165 interface detection:

```csharp
// Create an EIP-165 checker
var eip165 = new EIP165Native(endpoint);

// Check if a contract supports an interface
var supportsInterface = await eip165.SupportsInterface(
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
var estimate = await contract.EstimateTransactionFeeAsync(
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
try
{
    var result = await contract.InvokeMethodAsync("methodName", nonce, options, arguments);
}
catch (Exception ex)
{
    // Handle specific contract errors
    if (ex is ContractRevertException revertEx)
    {
        // Handle contract revert with custom error
        Console.WriteLine($"Contract reverted: {revertEx.Message}");
    }
    else
    {
        // Handle other errors
        Console.WriteLine($"Error: {ex.Message}");
    }
}
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
- [ ] Create ERC-20 token transfer example
  - Infrastructure exists (gas limits defined)
  - Need to add example implementation
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

```