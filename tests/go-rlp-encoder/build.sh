#!/bin/bash

echo "Building grlp binary..."

# Check if Go is installed
if ! command -v go &> /dev/null; then
    echo "Error: Go is not installed or not in PATH."
    echo "Please install Go before building the binary."
    exit 1
fi

# Check for go-ethereum dependency
if ! go list github.com/ethereum/go-ethereum/rlp &> /dev/null; then
    echo "Installing required dependency: github.com/ethereum/go-ethereum..."
    go get github.com/ethereum/go-ethereum
    
    if [ $? -ne 0 ]; then
        echo "Failed to install dependency. Please run:"
        echo "go get github.com/ethereum/go-ethereum"
        exit 1
    fi
fi

# Build the binary
go build -o grlp main.go

if [ $? -eq 0 ]; then
    echo "Build successful! You can now run:"
    echo "./grlp --test <number>"
    echo "or"
    echo "./run_all_tests.sh"
    chmod +x run_all_tests.sh
    chmod +x grlp
    echo "Binary size: $(du -h grlp | cut -f1)"
else
    echo "Build failed."
    exit 1
fi 