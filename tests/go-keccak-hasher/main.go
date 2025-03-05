package main

import (
	"encoding/hex"
	"flag"
	"fmt"
	"os"
	"strings"

	"github.com/ethereum/go-ethereum/crypto"
)

func main() {
	// Define command-line flags
	var input string
	var raw bool
	var prefix bool
	var isHex bool

	flag.StringVar(&input, "input", "", "The string to hash")
	flag.BoolVar(&raw, "raw", false, "Output raw bytes in hex without 0x prefix")
	flag.BoolVar(&prefix, "prefix", true, "Add 0x prefix to the output (default: true)")
	flag.BoolVar(&isHex, "hex", false, "Treat input as hex string (0x prefix will be removed if present)")
	flag.Parse()

	// Check if input is provided
	if input == "" {
		// If no flag is provided, check for positional arguments
		args := flag.Args()
		if len(args) > 0 {
			input = args[0]
		} else {
			fmt.Println("Error: No input string provided")
			fmt.Println("Usage: keccak-hasher [options] <string>")
			fmt.Println("Options:")
			flag.PrintDefaults()
			os.Exit(1)
		}
	}

	var inputBytes []byte
	var err error

	// Process input based on whether it's hex or not
	if isHex {
		// Remove 0x prefix if present
		hexInput := input
		if strings.HasPrefix(hexInput, "0x") {
			hexInput = hexInput[2:]
		}

		// Convert hex string to bytes
		inputBytes, err = hex.DecodeString(hexInput)
		if err != nil {
			fmt.Printf("Error decoding hex input: %v\n", err)
			os.Exit(1)
		}
	} else {
		// Use input as raw string
		inputBytes = []byte(input)
	}

	// Calculate Keccak-256 hash
	hash := crypto.Keccak256(inputBytes)

	// Convert to hex string
	hexHash := hex.EncodeToString(hash)

	// Format output based on flags
	if !raw && prefix {
		hexHash = "0x" + hexHash
	}

	fmt.Println(hexHash)
}
