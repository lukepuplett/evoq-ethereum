#!/bin/bash

echo "Running all RLP encoding test cases..."
echo ""
echo "// RLP Encoding Test Cases"
echo "// Generated using go-ethereum's RLP implementation"
echo ""

# Check if grlp binary exists, if not try to build it
if [ ! -f "./grlp" ] || [ ! -x "./grlp" ]; then
    echo "grlp binary not found or not executable. Attempting to build it..."
    go build -o grlp main.go
    
    if [ $? -ne 0 ]; then
        echo "Failed to build grlp binary. Falling back to 'go run'..."
        USE_GO_RUN=true
    else
        chmod +x ./grlp
        echo "Successfully built grlp binary."
        USE_GO_RUN=false
    fi
else
    USE_GO_RUN=false
fi

for i in {1..25}
do
    if [ "$USE_GO_RUN" = true ]; then
        result=$(go run main.go --test $i)
    else
        result=$(./grlp --test $i)
    fi
    
    echo "// Test Case $i"
    echo "// $(grep -A 2 "case $i:" main.go | tail -n +2 | head -n 1 | sed 's/\/\/ //')"
    echo "var testCase${i}Output = \"$result\";"
    echo ""
done

echo "All test cases completed." 