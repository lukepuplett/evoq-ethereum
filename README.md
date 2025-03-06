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
- [ ] Create sample wallet application (create/sign/submit transactions)
- [ ] Create smart contract interaction example (deploy and call methods)
- [ ] Create ERC-20 token transfer example
- [ ] Create NFT minting example (ERC-721)
- [ ] Develop integration tests with test networks (Sepolia)
- [ ] Create CLI tool for common Ethereum operations

### API Design Improvements
- [ ] Review and refine public API surface
- [ ] Mark appropriate implementation classes as internal
- [ ] Create helper methods for common workflows
- [ ] Improve error handling and messages
- [ ] Add comprehensive XML documentation

### Core Functionality Testing
- [ ] Transaction creation, signing, and submission
- [ ] Gas estimation
- [ ] Contract deployment
- [ ] Contract method calls (read and write)
- [ ] Event log parsing
- [ ] ABI encoding/decoding
- [ ] RLP encoding/decoding
- [ ] Address validation and formatting
- [ ] Private key management
- [ ] Nonce management
- [ ] Fee estimation (EIP-1559)

### Documentation
- [ ] Add usage examples to README
- [ ] Create documentation site
- [ ] Add code samples for common tasks
- [ ] Document architecture decisions

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

## Author

Luke Puplett

## Project Links

- [GitHub Repository](https://github.com/lukepuplett/evoq-ethereum)
- [NuGet Package](https://www.nuget.org/packages/Evoq.Ethereum)