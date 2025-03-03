package main

import (
	"flag"
	"fmt"
	"math/big"
	"os"

	"github.com/ethereum/go-ethereum/rlp"
)

func main() {
	testNum := flag.Int("test", 0, "Test case number (1-25)")
	flag.Parse()

	if *testNum < 1 || *testNum > 25 {
		fmt.Println("Please specify a test case with --test (1-25)")
		os.Exit(1)
	}

	var data interface{}

	// Test cases
	switch *testNum {
	case 1:
		// Empty string - tests the RLP encoding of an empty string
		// Expected: 0x80 (single byte representing an empty string)
		data = ""

	case 2:
		// Single byte (< 0x80) - tests the RLP encoding of a single byte in the [0x00, 0x7f] range
		// Expected: The byte itself (0x7f)
		data = []byte{0x7f}

	case 3:
		// Short string (< 56 bytes) - tests the RLP encoding of a short string
		// Expected: 0x80 + length followed by the string bytes
		data = "hello world"

	case 4:
		// Long string (>= 56 bytes) - tests the RLP encoding of a long string
		// Expected: 0xb7 + length of length followed by length followed by the string bytes
		longStr := make([]byte, 100)
		for i := 0; i < 100; i++ {
			longStr[i] = byte(i % 256)
		}
		data = longStr

	case 5:
		// Zero - tests the RLP encoding of zero
		// Expected: 0x80 (empty string, as 0 is encoded as empty byte array)
		data = uint(0)

	case 6:
		// Small integer - tests the RLP encoding of a small integer
		// Expected: Single byte if < 128, otherwise 0x80 + length followed by the minimal byte representation
		data = uint(42)

	case 7:
		// Medium integer - tests the RLP encoding of a medium-sized integer
		// Expected: 0x80 + length followed by the minimal byte representation
		data = uint(1024)

	case 8:
		// Large integer - tests the RLP encoding of a large integer using big.Int
		// Expected: 0x80 + length followed by the minimal byte representation
		data = big.NewInt(1000000000000000)

	case 9:
		// Negative integer - RLP cannot encode negative integers directly
		// When using big.Int.Bytes(), negative numbers lose their sign:
		// 1. big.Int stores numbers as sign + magnitude internally
		// 2. Bytes() only returns the absolute value (magnitude) as a byte slice
		// 3. The sign information is discarded
		// 4. This means -1000000 and 1000000 would produce identical byte slices
		// 5. For proper handling of negatives, applications must track sign separately
		n := big.NewInt(-1000000)
		fmt.Printf("// Note: Original value is %v, but Bytes() returns absolute value\n", n)
		data = n.Bytes() // This will only encode the absolute value (0xF4240)

	case 10:
		// Empty list - tests the RLP encoding of an empty list
		// Expected: 0xc0 (single byte representing an empty list)
		data = []interface{}{}

	case 11:
		// List with a single element - tests the RLP encoding of a list with one item
		// Expected: 0xc0 + length followed by the RLP encoding of the item
		data = []interface{}{uint(1)}

	case 12:
		// List with multiple elements of the same type - tests the RLP encoding of a homogeneous list
		// Expected: 0xc0 + length followed by the concatenation of the RLP encodings of the items
		data = []interface{}{uint(1), uint(2), uint(3)}

	case 13:
		// List with mixed types - tests the RLP encoding of a heterogeneous list
		// Expected: 0xc0 + length followed by the concatenation of the RLP encodings of the items
		data = []interface{}{uint(1), "hello", []byte{0x42}}

	case 14:
		// Nested list - tests the RLP encoding of a list containing another list
		// Expected: 0xc0 + length followed by the concatenation of the RLP encodings of the items,
		// where one item is itself an RLP-encoded list
		data = []interface{}{
			uint(1),
			[]interface{}{uint(2), uint(3)},
			"hello",
		}

	case 15:
		// Deeply nested list - tests the RLP encoding of a list with multiple levels of nesting
		// Expected: Recursive application of the list encoding rules
		data = []interface{}{
			uint(1),
			[]interface{}{
				uint(2),
				[]interface{}{uint(3), "nested"},
			},
			"hello",
		}

	case 16:
		// Simple struct - tests the RLP encoding of a Go struct
		// RLP encodes structs as lists of their exported fields in order
		// Expected: List encoding of the struct fields
		type Person struct {
			Name string
			Age  uint
		}
		data = Person{Name: "Alice", Age: 30}

	case 17:
		// Struct with nested struct - tests the RLP encoding of a struct containing another struct
		// Expected: List encoding where one element is itself a list encoding of the nested struct
		type Address struct {
			Street  string
			City    string
			ZipCode uint
		}
		type Person struct {
			Name    string
			Age     uint
			Address Address
		}
		data = Person{
			Name: "Bob",
			Age:  25,
			Address: Address{
				Street:  "123 Main St",
				City:    "Anytown",
				ZipCode: 12345,
			},
		}

	case 18:
		// Struct with slice - tests the RLP encoding of a struct containing a slice
		// Expected: List encoding where one element is itself a list encoding of the slice
		type Group struct {
			Name    string
			Members []string
		}
		data = Group{
			Name:    "Team A",
			Members: []string{"Alice", "Bob", "Charlie"},
		}

	case 19:
		// Byte arrays of different sizes - tests the RLP encoding of fixed-size byte arrays
		// Expected: Each array encoded according to its length and content
		data = []interface{}{
			[1]byte{0x01},
			[2]byte{0x02, 0x03},
			[3]byte{0x04, 0x05, 0x06},
			[4]byte{0x07, 0x08, 0x09, 0x0a},
		}

	case 20:
		// Basic Ethereum transaction (legacy format)
		// Contains: nonce, gasPrice, gasLimit, to, value, data, v, r, s
		type LegacyTransaction struct {
			Nonce    uint64
			GasPrice *big.Int
			GasLimit uint64
			To       [20]byte
			Value    *big.Int
			Data     []byte
			V        *big.Int
			R        *big.Int
			S        *big.Int
		}

		to := [20]byte{}
		for i := 0; i < 20; i++ {
			to[i] = byte(i + 1)
		}

		r1, _ := new(big.Int).SetString("1234567890abcdef", 16)
		s1, _ := new(big.Int).SetString("fedcba9876543210", 16)
		data = LegacyTransaction{
			Nonce:    42,
			GasPrice: big.NewInt(30000000000), // 30 Gwei
			GasLimit: 21000,
			To:       to,
			Value:    big.NewInt(1000000000000000000), // 1 ETH in wei
			Data:     []byte{},
			V:        big.NewInt(27),
			R:        r1,
			S:        s1,
		}

	case 21:
		// EIP-1559 transaction
		// Contains: chainId, nonce, maxPriorityFeePerGas, maxFeePerGas, gasLimit, to, value, data, accessList, v, r, s
		type AccessTuple struct {
			Address     [20]byte
			StorageKeys [][32]byte
		}

		type EIP1559Transaction struct {
			ChainID              *big.Int
			Nonce                uint64
			MaxPriorityFeePerGas *big.Int
			MaxFeePerGas         *big.Int
			GasLimit             uint64
			To                   [20]byte
			Value                *big.Int
			Data                 []byte
			AccessList           []AccessTuple
			V                    *big.Int
			R                    *big.Int
			S                    *big.Int
		}

		to := [20]byte{}
		for i := 0; i < 20; i++ {
			to[i] = byte(i + 1)
		}

		// Create a simple access list
		accessList := []AccessTuple{
			{
				Address: [20]byte{0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14},
				StorageKeys: [][32]byte{
					{0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20},
				},
			},
		}

		r2, _ := new(big.Int).SetString("1234567890abcdef", 16)
		s2, _ := new(big.Int).SetString("fedcba9876543210", 16)
		data = EIP1559Transaction{
			ChainID:              big.NewInt(1), // Ethereum mainnet
			Nonce:                123,
			MaxPriorityFeePerGas: big.NewInt(2000000000),  // 2 Gwei
			MaxFeePerGas:         big.NewInt(50000000000), // 50 Gwei
			GasLimit:             21000,
			To:                   to,
			Value:                big.NewInt(1000000000000000000), // 1 ETH in wei
			Data:                 []byte{0xca, 0xfe, 0xba, 0xbe},
			AccessList:           accessList,
			V:                    big.NewInt(1),
			R:                    r2,
			S:                    s2,
		}

	case 22:
		// Simple Ethereum transaction with just the core fields
		// This is a simplified transaction format focusing on the most essential fields
		type SimpleTransaction struct {
			Nonce    uint64
			GasPrice *big.Int
			GasLimit uint64
			To       [20]byte
			Value    *big.Int
			Data     []byte
		}

		to := [20]byte{}
		for i := 0; i < 20; i++ {
			to[i] = byte(i)
		}

		data = SimpleTransaction{
			Nonce:    1,
			GasPrice: big.NewInt(20000000000), // 20 Gwei
			GasLimit: 21000,
			To:       to,
			Value:    big.NewInt(500000000000000000), // 0.5 ETH
			Data:     []byte{},
		}

	case 23:
		// Contract creation transaction (no 'to' address)
		type ContractCreationTx struct {
			Nonce    uint64
			GasPrice *big.Int
			GasLimit uint64
			Value    *big.Int
			Data     []byte // Contract bytecode
			V        *big.Int
			R        *big.Int
			S        *big.Int
		}

		// Simple contract bytecode (just an example)
		contractCode := []byte{
			0x60, 0x80, 0x60, 0x40, 0x52, // PUSH1 80 PUSH1 40 MSTORE
			0x60, 0x0a, 0x60, 0x00, 0x55, // PUSH1 0a PUSH1 00 SSTORE (stores 10 at storage slot 0)
			0x60, 0x00, 0x80, 0xfd, // PUSH1 00 DUP1 REVERT
		}

		r3, _ := new(big.Int).SetString("9876543210abcdef", 16)
		s3, _ := new(big.Int).SetString("fedcba0987654321", 16)
		data = ContractCreationTx{
			Nonce:    0,
			GasPrice: big.NewInt(50000000000), // 50 Gwei
			GasLimit: 500000,                  // Contract creation needs more gas
			Value:    big.NewInt(0),           // Usually 0 for contract creation
			Data:     contractCode,
			V:        big.NewInt(28),
			R:        r3,
			S:        s3,
		}

	case 24:
		// Ethereum block header
		type BlockHeader struct {
			ParentHash  [32]byte
			UncleHash   [32]byte
			Coinbase    [20]byte
			Root        [32]byte
			TxHash      [32]byte
			ReceiptHash [32]byte
			Bloom       [256]byte
			Difficulty  *big.Int
			Number      *big.Int
			GasLimit    uint64
			GasUsed     uint64
			Time        uint64
			Extra       []byte
			MixDigest   [32]byte
			Nonce       [8]byte
		}

		// Create a sample block header
		parentHash := [32]byte{}
		uncleHash := [32]byte{}
		coinbase := [20]byte{}
		root := [32]byte{}
		txHash := [32]byte{}
		receiptHash := [32]byte{}
		bloom := [256]byte{}
		mixDigest := [32]byte{}
		nonce := [8]byte{}

		// Fill with some sample data
		for i := 0; i < 32; i++ {
			if i < 8 {
				nonce[i] = byte(i + 1)
			}
			if i < 20 {
				coinbase[i] = byte(i + 1)
			}
			parentHash[i] = byte(i + 1)
			uncleHash[i] = byte(i + 2)
			root[i] = byte(i + 3)
			txHash[i] = byte(i + 4)
			receiptHash[i] = byte(i + 5)
			mixDigest[i] = byte(i + 6)
		}

		data = BlockHeader{
			ParentHash:  parentHash,
			UncleHash:   uncleHash,
			Coinbase:    coinbase,
			Root:        root,
			TxHash:      txHash,
			ReceiptHash: receiptHash,
			Bloom:       bloom,
			Difficulty:  big.NewInt(2000000),
			Number:      big.NewInt(12345),
			GasLimit:    15000000,
			GasUsed:     12500000,
			Time:        1618203344,
			Extra:       []byte("Ethereum"),
			MixDigest:   mixDigest,
			Nonce:       nonce,
		}

	case 25:
		// Transaction receipt
		type Log struct {
			Address [20]byte
			Topics  [][32]byte
			Data    []byte
		}

		type Receipt struct {
			PostStateOrStatus []byte
			CumulativeGasUsed uint64
			Bloom             [256]byte
			Logs              []Log
		}

		// Create a sample receipt
		address := [20]byte{}
		for i := 0; i < 20; i++ {
			address[i] = byte(i + 1)
		}

		topic1 := [32]byte{}
		topic2 := [32]byte{}
		for i := 0; i < 32; i++ {
			topic1[i] = byte(i + 1)
			topic2[i] = byte(i + 2)
		}

		logs := []Log{
			{
				Address: address,
				Topics:  [][32]byte{topic1, topic2},
				Data:    []byte{0x01, 0x02, 0x03, 0x04},
			},
		}

		bloom := [256]byte{}

		data = Receipt{
			PostStateOrStatus: []byte{0x01}, // success
			CumulativeGasUsed: 21000,
			Bloom:             bloom,
			Logs:              logs,
		}
	}

	// Encode the data
	encoded, err := rlp.EncodeToBytes(data)
	if err != nil {
		fmt.Printf("Encoding error: %v\n", err)
		os.Exit(1)
	}

	// Output as hex
	fmt.Printf("0x%x\n", encoded)
}
