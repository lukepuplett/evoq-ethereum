# Go Keccak Hasher

A simple CLI tool for hashing strings using the Keccak-256 hash function as used by Ethereum.

## Installation

```bash
# Clone the repository
git clone https://github.com/your-username/go-keccak-hasher.git
cd go-keccak-hasher

# Build the binary
go build -o keccak-hasher
```

## Usage

```bash
# Basic usage - hash a string
./keccak-hasher "Hello, world!"

# Using the input flag
./keccak-hasher --input="Hello, world!"

# Output raw bytes without 0x prefix
./keccak-hasher --raw "Hello, world!"

# Explicitly disable 0x prefix
./keccak-hasher --prefix=false "Hello, world!"

# Hash hex input (with or without 0x prefix)
./keccak-hasher --hex "0x48656c6c6f2c20776f726c6421"
./keccak-hasher --hex "48656c6c6f2c20776f726c6421"
```

## Examples

```bash
# Hashing a string
$ ./keccak-hasher "Hello, world!"
0x47173285a8d7341e5e972fc677286384f802f8ef42a5ec5f03bbfa254cb01fad

# Hashing a string without 0x prefix
$ ./keccak-hasher --raw "Hello, world!"
47173285a8d7341e5e972fc677286384f802f8ef42a5ec5f03bbfa254cb01fad

# Hashing hex input (the hex encoding of "Hello, world!")
$ ./keccak-hasher --hex "0x48656c6c6f2c20776f726c6421"
0x47173285a8d7341e5e972fc677286384f802f8ef42a5ec5f03bbfa254cb01fad
```

## Dependencies

This tool uses the [go-ethereum](https://github.com/ethereum/go-ethereum) package for Keccak-256 hashing. 