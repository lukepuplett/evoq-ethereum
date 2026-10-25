# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.0.0] - 2026-10-25

### Breaking
- `TransactionDataDto.AccessList` is now `AccessListEntryDto[]?` instead of `AccessListDto?`. Nodes return `accessList` as a plain array, so the old wrapper type could not deserialize a real response. `AccessListDto` is no longer used by the library.

### Fixed
- `Chain.SuggestEip1559FeesAsync` always returned the hard-coded 2 gwei tip, because `ChainClient.GetFeeHistoryAsync` discarded the `eth_feeHistory` response and returned an empty `FeeHistory`. It now returns the real data.
- `Chain.SuggestEip1559FeesAsync` read the tip from the oldest block in the fee history (`Reward[0][0]`). It now uses the newest block.
- `FeeHistoryDto`, `BlockDataDto`, `WithdrawalDto`, `TransactionDataDto` and `AccessListEntryDto` had no `JsonPropertyName` attributes, so camelCase node responses were never populated. This left block numbers, gas values, timestamps, withdrawals and transactions empty or zero, and made `Chain.GetBaseFeeAsync` always return 0. The max fee from `SuggestEip1559FeesAsync` was therefore the tip alone.
- `FeeHistory`, `BlockData` and `TransactionData` now parse minimal-length hex quantities (e.g. `0x1`) and empty values (`0x`), which nodes return.

### Added
- `ChainSuggestFeesTests` and `BlockDataDeserializationTests`, which reproduce the above using node-shaped JSON.

### Upgrade notes
- Fee suggestions now reflect the network. On low-fee chains (e.g. Base Sepolia) the tip and max fee will be much lower than the previous ~2 gwei, and the base fee is now included in the max fee.
- Code that read `TransactionDataDto.AccessList.AccessList` should read `TransactionDataDto.AccessList` directly.

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
