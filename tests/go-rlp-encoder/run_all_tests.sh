#!/bin/bash

echo "Running all RLP encoding test cases..."
echo ""
echo "// RLP Encoding Test Cases"
echo "// Generated using go-ethereum's RLP implementation"
echo ""

for i in {1..25}
do
    result=$(go run main.go --test $i)
    echo "// Test Case $i"
    echo "// $(grep -A 2 "case $i:" main.go | tail -n +2 | head -n 1 | sed 's/\/\/ //')"
    echo "var testCase${i}Output = \"$result\";"
    echo ""
done

echo "All test cases completed." 