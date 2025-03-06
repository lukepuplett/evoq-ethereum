using System;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Represents an item in an EIP-2930 access list.
/// </summary>
public struct AccessListItem
{
    /// <summary>
    /// The address to access.
    /// </summary>
    public byte[] Address; // 20-byte address

    /// <summary>
    /// The storage keys to access.
    /// </summary>
    public byte[][] StorageKeys; // Each key is 32 bytes

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessListItem"/> struct.
    /// </summary>
    /// <param name="address">The address to access.</param>
    /// <param name="storageKeys">The storage keys to access.</param>
    public AccessListItem(byte[] address, byte[][] storageKeys)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        StorageKeys = storageKeys ?? Array.Empty<byte[]>();
    }
}