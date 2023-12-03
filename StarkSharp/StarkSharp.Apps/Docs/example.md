### Example call dex:

```csharp
string privateKey = "private_key";
string walletAddress = "your_wallet_address";
Settings.apiurl = "your_api_url";
Token weth = new Token
{
    ContractAddress = "0x49d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
    Name = "weth",
    Network = "starknet",
    Abi = null
};
Token usdc = new Token()
{
    ContractAddress = "0x5a643907b9a4bc6a55e9069c4fd5fd1f5c79a22470690f75556c4736e34426",
    Name = "usdc",
    Network = "starknet",
    Abi = null
};

MySwap mySwap = new MySwap(privateKey, walletAddress);
var status =  await mySwap.Swap(weth, usdc, 0.0002m);
```