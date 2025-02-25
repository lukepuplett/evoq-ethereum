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

## Building

Testing depends on a Golang executable which is included in the repository along with its source code. The Golang executable is used to generate ABI encoding using go-ethereum and serves to test the .NET ABI encoding against the go-ethereum ABI encoding.

The Golang executable is called **gabi** and was compiled for macOS on an M chip. It may need to be recompiled for other platforms.

**Note** - The tests do not call the gabi executable, instead gabi is used to generate the expected output which is embedded into the test code as the expected output string.

From the folder containing the solution file:

```bash
dotnet build
dotnet test
```

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