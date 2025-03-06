namespace Evoq.Ethereum.Examples;

[TestClass]
public class ExampleEAS
{
    [TestMethod]
    public void ExampleEAS_CreateWallet()
    {
        // Call the GetSchema method on Ethereum Attestation Service

        // What do we need?
        //
        // ABI of EAS contract in order to call the GetSchema method
        // Address of EAS contract
        //
        // How do we get this?
        //
        // Use the ContractAbiReader to read the ABI from the EAS contract

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


        // Then what?
        //
        // Get the function signature of the GetSchema method
        //
        // Call the GetSchema method with a value for the schemaId.
        //
        // This means ABI encoding the function signature and the schemaId.
        //
        // We don't need a signed transaction because we are not mutating the state.
        //
        // We do need to select a HTTP provider, and a JSON-RPC method.


    }
}
