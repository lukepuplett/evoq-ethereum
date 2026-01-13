# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.3.0] - 2026-01-13

### Added
- Added optional `IHttpClientFactory` support to `Chain` and `ChainClient` for better HTTP client management
  - Allows consumers to provide their own `IHttpClientFactory` instance for custom HTTP client configuration
  - All `CreateDefault` methods now accept an optional `httpClientFactory` parameter
  - Backward compatible - existing code continues to work without changes
- Added comprehensive tests for HttpClientFactory functionality in `ChainHttpClientFactoryTests.cs`

### Updated
- Updated `Microsoft.Extensions.Http` dependency from 8.0.0 to 10.0.1
- Updated test project dependencies to 10.0.1

### Changed
- Improved comments in `AbiDecoder.cs` for clarity
- Removed unused using statement in `JsonRpcClient.cs`

## [3.2.2] - 2025-11-06

### Fixed
- Fixed duplicate key exception when decoding arrays of tuples in `AbiDecoder.checkTypeAndSet()`
  - Handles empty `SafeName` values (falls back to `Position.ToString()`)
  - Handles duplicate keys by appending suffixes (`_1`, `_2`, etc.)
  - Prevents `ArgumentException: An item with the same key has already been added` when decoding `((address,uint256,bytes)[])` and similar types

## [3.2.1] - 2025-11-06

### Fixed
- Fixed array of tuples decoding bug

## [3.2.0] - Previous

### Added
- Improved MessageSigner API with instance methods and better encapsulation
- ETH transfer functionality

### Changed
- Improved logging
- Surfaced signer addresses
