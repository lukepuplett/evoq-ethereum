# Evoq.Ethereum

A lightweight .NET library providing Ethereum-specific utilities and extensions. This package builds upon Evoq.Blockchain and Nethereum to provide a simplified interface for Ethereum blockchain operations.

## Installation

```
dotnet add package Evoq.Ethereum
```

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

- Evoq.Blockchain (1.0.0)
- Nethereum.Util (4.29.0)
- Nethereum.Signer (4.29.0)
- Nethereum.Web3 (4.29.0)

## Usage

```csharp
// Example usage will be added as features are implemented
```

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
  - File-based nonce tracking
  - Transaction sequence handling
- [x] Fee estimation (EIP-1559)
  - Base fee and priority fee calculations
  - Support for fee market

### Documentation
- [ ] Add usage examples to README
  - Need to add code samples for common operations
  - Include transaction and contract interaction examples
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
Hardhat Ignition 🚀

Deploying [ EASDeployment ]

Batch #1
  Executed EASDeployment#SchemaRegistry

Batch #2
  Executed EASDeployment#EAS

[ EASDeployment ] successfully deployed 🚀

Deployed Addresses

EASDeployment#SchemaRegistry - 0x5FbDB2315678afecb367f032d93F642f64180aa3
EASDeployment#EAS - 0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512
```

These contract addresses can be used in your application to interact with the EAS system on your local development environment.

### Integration Testing

With the local Hardhat node running and EAS contracts deployed, you can now test your Ethereum applications against a fully functional local blockchain environment. This setup is ideal for:

- Testing smart contract interactions
- Developing and testing dApps
- Validating EAS attestation functionality
- Simulating various blockchain scenarios without spending real ETH

## Author

Luke Puplett

## Project Links

- [GitHub Repository](https://github.com/lukepuplett/evoq-ethereum)
- [NuGet Package](https://www.nuget.org/packages/Evoq.Ethereum)