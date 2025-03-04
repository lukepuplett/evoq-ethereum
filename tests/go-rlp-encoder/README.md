# Go RLP Encoder Test Rig

This is a simple tool that produces sample RLP encoding outputs using the go-ethereum implementation. It's designed to generate reference outputs for testing RLP encoder implementations in other languages.

## Building the Binary

You'll need Go installed to build the binary, but once built, the binary can be distributed and used without Go:

```bash
# Build the binary
go build -o grlp main.go
```

## Installation

To build the binary, you need the go-ethereum dependency:

```bash
go get github.com/ethereum/go-ethereum
```

## Usage

### Using the Binary

Once built, you can use the binary directly:

```bash
./grlp --test <number>
```

Or run the test script to generate all test cases:

```bash
./run_all_tests.sh
```

### Using Go Directly (Development)

During development, you can also run without building:

```bash
go run main.go --test <number>
```

Where `<number>` is a test case number from 1 to 25.

## Test Cases

1. Empty string
2. Single byte (< 0x80)
3. Short string (< 56 bytes)
4. Long string (>= 56 bytes)
5. Zero
6. Small integer
7. Medium integer
8. Large integer
9. Negative integer (encoded as bytes)
10. Empty list
11. List with a single element
12. List with multiple elements of the same type
13. List with mixed types
14. Nested list
15. Deeply nested list
16. Simple struct
17. Struct with nested struct
18. Struct with slice
19. Byte arrays of different sizes
20. Legacy Ethereum transaction
21. EIP-1559 transaction
22. Simple Ethereum transaction
23. Contract creation transaction
24. Ethereum block header
25. Transaction receipt

## Example

```bash
$ ./grlp --test 3
0x8b68656c6c6f20776f726c64
```

## RLP Encoding Rules

RLP (Recursive Length Prefix) encoding follows these rules:

1. For a single byte whose value is in the [0x00, 0x7f] range, that byte is its own RLP encoding.
2. If a string is 0-55 bytes long, the RLP encoding consists of a single byte with value 0x80 plus the length of the string followed by the string. The range of the first byte is [0x80, 0xb7].
3. If a string is more than 55 bytes long, the RLP encoding consists of a single byte with value 0xb7 plus the length in bytes of the length of the string in binary form, followed by the length of the string, followed by the string. For example, a 1024-byte string would be encoded as \xb9\x04\x00 followed by the string. The range of the first byte is [0xb8, 0xbf].
4. If the total payload of a list (i.e. the combined length of all its items being RLP encoded) is 0-55 bytes long, the RLP encoding consists of a single byte with value 0xc0 plus the length of the list followed by the concatenation of the RLP encodings of the items. The range of the first byte is [0xc0, 0xf7].
5. If the total payload of a list is more than 55 bytes long, the RLP encoding consists of a single byte with value 0xf7 plus the length in bytes of the length of the payload in binary form, followed by the length of the payload, followed by the concatenation of the RLP encodings of the items. The range of the first byte is [0xf8, 0xff]. 