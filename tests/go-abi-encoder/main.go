package main

import (
	"flag"
	"fmt"
	"math/big"
	"os"

	"github.com/ethereum/go-ethereum/accounts/abi"
)

func main() {
	testNum := flag.Int("test", 0, "Test case number (1-20)")
	flag.Parse()

	if *testNum < 1 || *testNum > 20 {
		fmt.Println("Please specify a test case with --test (1-20)")
		os.Exit(1)
	}

	var types []abi.Type
	var values []interface{}

	// Define ABI types
	evmUint256, _ := abi.NewType("uint256", "", nil)
	// abi_uint32, _ := abi.NewType("uint32", "", nil)
	evmUint8, _ := abi.NewType("uint8", "", nil)
	evmBoolType, _ := abi.NewType("bool", "", nil)
	evmBytes, _ := abi.NewType("bytes", "", nil)
	evmBytes10, _ := abi.NewType("bytes10", "", nil)
	// bytes3, _ := abi.NewType("bytes3", "", nil)
	// stringType, _ := abi.NewType("string", "", nil)

	// Test cases
	switch *testNum {
	case 1: // foo(uint256) - 1
		types = []abi.Type{evmUint256}
		values = []interface{}{big.NewInt(1)}

	case 2: // foo(bool) - true
		types = []abi.Type{evmBoolType}
		values = []interface{}{true}

	case 3: // foo(uint8, uint256) - (1, 1)
		types = []abi.Type{evmUint8, evmUint256}
		values = []interface{}{uint8(1), big.NewInt(1)}

	case 4: // foo(uint8[2]) - [1, 2]
		uint8_2, _ := abi.NewType("uint8[2]", "", nil)
		types = []abi.Type{uint8_2}
		values = []interface{}{[2]uint8{1, 2}}

	case 5: // foo(uint8[4][2]) - [[10, 20, 30, 40], [1, 2, 3, 4]]
		uint8_4_2, _ := abi.NewType("uint8[4][2]", "", nil)
		types = []abi.Type{uint8_4_2}
		values = []interface{}{[2][4]uint8{{10, 20, 30, 40}, {1, 2, 3, 4}}}

	case 6: // foo(uint8[3][2][1]) - [[[1, 2, 3], [1, 2, 3]]]
		uint8_3_2_1, _ := abi.NewType("uint8[3][2][1]", "", nil)
		types = []abi.Type{uint8_3_2_1}
		values = []interface{}{[1][2][3]uint8{{{1, 2, 3}, {1, 2, 3}}}}

	case 7: // foo((uint256 id, uint256 balance) account) - (3, 10)
		tuple, _ := abi.NewType("tuple", "", []abi.ArgumentMarshaling{
			{Name: "id", Type: "uint256"},
			{Name: "balance", Type: "uint256"},
		})
		types = []abi.Type{tuple}
		values = []interface{}{struct {
			ID      *big.Int `abi:"id"`
			Balance *big.Int `abi:"balance"`
		}{big.NewInt(3), big.NewInt(10)}}

	case 8: // foo(bool isActive, (uint256 id, uint256 balance) account) - (true, (3, 10))
		tuple, _ := abi.NewType("tuple", "", []abi.ArgumentMarshaling{
			{Name: "id", Type: "uint256"},
			{Name: "balance", Type: "uint256"},
		})
		types = []abi.Type{evmBoolType, tuple}
		values = []interface{}{
			true,
			struct {
				ID      *big.Int `abi:"id"`
				Balance *big.Int `abi:"balance"`
			}{big.NewInt(3), big.NewInt(10)},
		}

	case 9: // foo((bool isActive, uint256 seenUnix) prof, (uint256 id, uint256 balance) account) - ((true, 20), (3, 10))
		profTuple, _ := abi.NewType("tuple", "", []abi.ArgumentMarshaling{
			{Name: "isActive", Type: "bool"},
			{Name: "seenUnix", Type: "uint256"},
		})
		acctTuple, _ := abi.NewType("tuple", "", []abi.ArgumentMarshaling{
			{Name: "id", Type: "uint256"},
			{Name: "balance", Type: "uint256"},
		})
		types = []abi.Type{profTuple, acctTuple}
		values = []interface{}{
			struct {
				IsActive bool     `abi:"isActive"`
				SeenUnix *big.Int `abi:"seenUnix"`
			}{true, big.NewInt(20)},
			struct {
				ID      *big.Int `abi:"id"`
				Balance *big.Int `abi:"balance"`
			}{big.NewInt(3), big.NewInt(10)},
		}

	case 10: // foo(((bool isActive, uint256 seenUnix) prof, uint256 id, uint256 balance) account) - ((true, 20), 3, 10)
		nestedTuple, _ := abi.NewType("tuple", "", []abi.ArgumentMarshaling{
			{Name: "prof", Type: "tuple", Components: []abi.ArgumentMarshaling{
				{Name: "isActive", Type: "bool"},
				{Name: "seenUnix", Type: "uint256"},
			}},
			{Name: "id", Type: "uint256"},
			{Name: "balance", Type: "uint256"},
		})
		types = []abi.Type{nestedTuple}
		values = []interface{}{
			struct {
				Prof struct {
					IsActive bool     `abi:"isActive"`
					SeenUnix *big.Int `abi:"seenUnix"`
				} `abi:"prof"`
				ID      *big.Int `abi:"id"`
				Balance *big.Int `abi:"balance"`
			}{
				Prof: struct {
					IsActive bool     `abi:"isActive"`
					SeenUnix *big.Int `abi:"seenUnix"`
				}{true, big.NewInt(20)},
				ID:      big.NewInt(3),
				Balance: big.NewInt(10),
			},
		}

	case 11: // foo(bytes) - [1]
		types = []abi.Type{evmBytes}
		values = []interface{}{[]byte{1}}

	case 12: // foo(uint8[]) - [1, 2]
		uint8Dyn, _ := abi.NewType("uint8[]", "", nil)
		types = []abi.Type{uint8Dyn}
		values = []interface{}{[]uint8{1, 2}}

	case 13: // foo(uint8[2][]) - [[1, 2], [3, 4]]
		uint8_2Dyn, _ := abi.NewType("uint8[2][]", "", nil)
		types = []abi.Type{uint8_2Dyn}
		values = []interface{}{[][2]uint8{{1, 2}, {3, 4}}}

	case 14: // foo(uint8[][]) - [[1, 2], [3, 4]]
		uint8DynDyn, _ := abi.NewType("uint8[][]", "", nil)
		types = []abi.Type{uint8DynDyn}
		values = []interface{}{[][]uint8{{1, 2}, {3, 4}}}

	case 15: // foo(bool isActive, (string id, uint256 balance) account) - (true, ("abc", 9))
		tuple, _ := abi.NewType("tuple", "", []abi.ArgumentMarshaling{
			{Name: "id", Type: "string"},
			{Name: "balance", Type: "uint256"},
		})
		types = []abi.Type{evmBoolType, tuple}
		values = []interface{}{
			true,
			struct {
				ID      string   `abi:"id"`
				Balance *big.Int `abi:"balance"`
			}{"abc", big.NewInt(9)},
		}

	case 16: // foo(bool isActive, ((string id, string name) user, uint256 balance) account) - (true, (("a", "abc"), 9))
		nestedTuple, _ := abi.NewType("tuple", "", []abi.ArgumentMarshaling{
			{Name: "user", Type: "tuple", Components: []abi.ArgumentMarshaling{
				{Name: "id", Type: "string"},
				{Name: "name", Type: "string"},
			}},
			{Name: "balance", Type: "uint256"},
		})
		types = []abi.Type{evmBoolType, nestedTuple}
		values = []interface{}{
			true,
			struct {
				User struct {
					ID   string `abi:"id"`
					Name string `abi:"name"`
				} `abi:"user"`
				Balance *big.Int `abi:"balance"`
			}{
				User: struct {
					ID   string `abi:"id"`
					Name string `abi:"name"`
				}{"a", "abc"},
				Balance: big.NewInt(9),
			},
		}

	case 17: // bar(bytes3[2]) - ["abc", "def"]
		bytes3_2, _ := abi.NewType("bytes3[2]", "", nil)
		types = []abi.Type{bytes3_2}
		values = []interface{}{[2][3]byte{{'a', 'b', 'c'}, {'d', 'e', 'f'}}}

	case 18: // baz(uint256 x, bool y) - (69, true)
		types = []abi.Type{evmUint256, evmBoolType}
		values = []interface{}{big.NewInt(69), true}

	case 19: // sam(bytes, bool, uint[]) - ("dave", true, [1, 2, 3])
		uintDyn, err := abi.NewType("uint256[]", "", nil)
		if err != nil {
			fmt.Printf("Failed to create uint256[] type: %v\n", err)
			os.Exit(1)
		}
		types = []abi.Type{evmBytes, evmBoolType, uintDyn}
		uintValues := []*big.Int{
			big.NewInt(1),
			big.NewInt(2),
			big.NewInt(3),
		}
		values = []interface{}{
			[]byte("dave"),
			true,
			uintValues,
		}

	case 20: // foo(uint256, uint32[], bytes10, bytes) - (0x123, [0x456, 0x789], "1234567890", "Hello, world!")
		uint32Dyn, _ := abi.NewType("uint32[]", "", nil)
		types = []abi.Type{evmUint256, uint32Dyn, evmBytes10, evmBytes}
		values = []interface{}{
			big.NewInt(0x123),
			[]uint32{0x456, 0x789},
			[10]byte{'1', '2', '3', '4', '5', '6', '7', '8', '9', '0'},
			[]byte("Hello, world!"),
		}
	}

	// Create ABI arguments
	args := abi.Arguments{}
	for _, t := range types {
		args = append(args, abi.Argument{Type: t})
	}

	// Encode
	encoded, err := args.Pack(values...)
	if err != nil {
		fmt.Printf("Encoding error: %v\n", err)
		os.Exit(1)
	}

	// Output as hex
	fmt.Printf("0x%x\n", encoded)
}
