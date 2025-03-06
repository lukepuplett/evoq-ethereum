namespace Evoq.Ethereum.Contracts;

// A hypothetical Contract class would be able to produce function
// signatures for the methods in the ABI, and hold the address of
// the contract.
//
// contract.CallAsync("GetSchema", schemaId);
//
// contractCaller.CallAsync(contract, "GetSchema", schemaId);
//
// ContractCaller is a class that can be used to call methods on a
// contract. Is is configured with a IEthereumJsonRpc, IAbiEncoder,
// IAbiDecode, and a ITransactionSigner, and a INonceStore.
//
// !! We need something to compute the gas price.
//