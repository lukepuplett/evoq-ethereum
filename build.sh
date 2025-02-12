#!/bin/bash

# Exit on error
set -e

# Configuration
PROJECT_PATH="src/Evoq.Ethereum/Evoq.Ethereum.csproj"

# Clean previous artifacts
rm -rf ./artifacts

# Build and test
echo "Building project..."
dotnet build -c Release

echo "Running tests..."
dotnet test -c Release

# Pack
echo "Creating NuGet package..."
dotnet pack -c Release "$PROJECT_PATH" -o ./artifacts

echo "Build completed successfully! Package created in ./artifacts"