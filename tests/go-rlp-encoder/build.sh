#!/bin/bash

echo "Building grlp binary..."
go build -o grlp main.go

if [ $? -eq 0 ]; then
    echo "Build successful! You can now run:"
    echo "./grlp --test <number>"
    echo "or"
    echo "./run_all_tests.sh"
    chmod +x run_all_tests.sh
    chmod +x grlp
else
    echo "Build failed."
    exit 1
fi 