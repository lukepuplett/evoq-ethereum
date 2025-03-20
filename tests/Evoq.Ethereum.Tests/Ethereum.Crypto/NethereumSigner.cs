// using Evoq.Blockchain;
// using Nethereum.Signer;

// namespace Evoq.Ethereum.Crypto;

// public static class NethereumSigner
// {
//     public static (Hex R, Hex S, Hex V) Sign(Hex privateKey, Hex txHash, ulong chainId)
//     {
//         var ethEcKey = new EthECKey(privateKey.ToByteArray(), isPrivate: true);

//         var signature = ethEcKey.SignAndCalculateV(txHash.ToByteArray(), chainId);

//         var r = signature.R.ToHexStruct();
//         var s = signature.S.ToHexStruct();
//         var v = signature.V.ToHexStruct();

//         return (r, s, v);
//     }
// }