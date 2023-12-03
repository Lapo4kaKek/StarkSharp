using System.Numerics;
using Newtonsoft.Json;
using StarkSharp.Accounts;
using StarkSharp.Connectors;
using StarkSharp.Connectors.Components;
using StarkSharp.Platforms;
using StarkSharp.Platforms.Dotnet;
using StarkSharp.Rpc;
using StarkSharpApp.StarkSharp.StarkSharp.StarkSharp.Apps.Models;

namespace StarkSharpApp.StarkSharp.StarkSharp.StarkSharp.Apps.Dex;

public class MySwap
{
    private string privateKey; // argentX
    private string walletAddress;

    
    private Platform platform;
    private Connector connector;
  
    private static readonly Dictionary<string, string> list = new Dictionary<string, string>
    {
        {"ETH-USDC", "1" },
        {"DAI-ETH", "2"},
        {"WBTC-USDC", "3"},
        {"ETH-USDT", "4"},
        {"USDC-USDT","5"},
        {"DAI-USDC","6"},
        {"tETH-ETH", "7"},
        {"ORDS-ETH", "8"}
    };

    public MySwap(string privateKey, string walletAddress)
    {
        platform = DotnetPlatform.New(Platform.PlatformConnectorType.RPC);
        connector = new Connector(platform);

        this.privateKey = privateKey;
        this.walletAddress = walletAddress;
    }
    
    public async Task<bool> Swap(Token tokenFrom, Token tokenTo, decimal amountIn, decimal priceImpact = 0.005m,
        decimal slippage = 0.995m)
    {
        var contractAddress = "0x10884171baf1914edc28d7afb619b40a4051cfae78a094a55d230f19e944a28";
        ApproveTransaction(tokenFrom, contractAddress, amountIn);
        
        Thread.Sleep(10000);
        
        string functionName = "swap";
        
        string poolId = "0x" + GetPoolId(tokenFrom, tokenTo);
        
        double decimalsFrom = tokenFrom.Name == "weth" ? 18 : 6;
        
        string amount_in = "0x" + new BigInteger(amountIn * (decimal)Math.Pow(10, decimalsFrom)).ToString("X");
        
        BigInteger amountInBig = new BigInteger(amountIn * (decimal)Math.Pow(10, decimalsFrom));
        
        BigInteger[] reserves = await GetReserves(tokenFrom, tokenTo);

        double decimalsTo = tokenTo.Name == "weth" ? 18 : 6;
        
        var amountToMin = await GetAmountOutMin(tokenTo, amountInBig, reserves[0], reserves[1]);
        var amount_to_min = "0x" + new BigInteger(amountToMin * (decimal)Math.Pow(10, decimalsTo)).ToString("X");
        
        string[] functionArgsSwap = new[] { poolId, tokenFrom.ContractAddress, amount_in, "0x0", amount_to_min, "0x0" };
        
        string version = "0x1";
     
        var maxFee = GetSwapFeeString();
        
        TransactionInteraction transactionInteractionSwap
            = new TransactionInteraction(
                walletAddress, 
                contractAddress, 
                functionName, 
                functionArgsSwap, 
                CairoVersion.Version1,
                maxFee, 
                ChainId.Mainnet.GetStringValue(), 
                privateKey, 
                version);
        
        connector.SendTransaction(transactionInteractionSwap, response => OnSendTransactionSuccess(response), errorMessage => OnSendTransactionError(errorMessage));
        return true;
    }
    
    private string GetSwapFeeString()
    {
        //string maxFee = "0x3e3faca6d10";
        string maxFee = "0x1126321C20D9A";
        return maxFee;
    }
    private string GetPoolId(Token tokenFrom, Token tokenTo)
    {
        string tokenFromName = CheckWeth(tokenFrom) ? "ETH" : tokenFrom.Name.ToUpperInvariant();
        string tokenToName = CheckWeth(tokenTo) ? "ETH" : tokenTo.Name.ToUpperInvariant();

        string key = $"{tokenFromName}-{tokenToName}";
        string reverseKey = $"{tokenToName}-{tokenFromName}";

        if (list.TryGetValue(key, out string poolId) || list.TryGetValue(reverseKey, out poolId))
        {
            return poolId;
        }

        return null;
    }
    private async Task<BigInteger[]> GetReserves(Token tokenFrom, Token tokenTo)
    {
        string contractAddress = "0x010884171baf1914edc28d7afb619b40a4051cfae78a094a55d230f19e944a28";
        string entrypoint = "get_pool";
        string pool_id = "0x" + GetPoolId(tokenFrom, tokenTo);
        
        ContractInteraction interaction = new ContractInteraction
        (
            contractAddress,
            entrypoint,
            new object[] {pool_id}
        );
        TaskCompletionSource<BigInteger[]> taskCompletionSource = new TaskCompletionSource<BigInteger[]>();
        platform.CallContract(interaction, 
            successCallback: (response) =>
            {
                string[] result = JsonConvert.DeserializeObject<string[]>(response);
                BigInteger[] reserves = new BigInteger[]
                {
                    BigInteger.Parse(result[2].Substring(2), System.Globalization.NumberStyles.HexNumber), 
                    BigInteger.Parse(result[5].Substring(2), System.Globalization.NumberStyles.HexNumber)
                };
                taskCompletionSource.SetResult(reserves);
            },
            errorCallback: (error) =>
            {
                Console.WriteLine($"Error: {error}");
                taskCompletionSource.SetException(new Exception(error));
            });
        var result = taskCompletionSource.Task.Result;
        return result;
    }
    private async Task<decimal> GetAmountOutMin(Token tokenTo, BigInteger amountIn, BigInteger reserveIn, BigInteger reserveOut)
    {
        BigInteger amountOutMin = (reserveOut * amountIn) / (reserveIn + amountIn);
        double decimalsTo = tokenTo.Name == "weth" ? 18 : 6;
        decimal result = (decimal)amountOutMin / (decimal)Math.Pow(10,  decimalsTo);
        return result * 0.95m;
    }

    private bool CheckWeth(Token token)
    {
        return token.Name == "weth";
    }

    private void ApproveTransaction(Token token, string spenderAddress, decimal amountIn)
    {
        string functionName = "approve";
        
        double decimalsTo = token.Name == "weth" ? 18 : 6;
        
        var amountInBiginteger = new BigInteger(amountIn * (decimal)Math.Pow(10, decimalsTo));
        
        string[] functionArgsApprove  = { spenderAddress, "0x" + amountInBiginteger.ToString("X"), "0x0" };

        CairoVersion cairoVersion = CairoVersion.Version1;
        
        string maxFee = "0x2e3faca6d30";
        
        string version = "0x1";
        TransactionInteraction transactionInteractionApprove 
            = new TransactionInteraction(walletAddress, token.ContractAddress, functionName, functionArgsApprove, cairoVersion,
                maxFee, ChainId.Mainnet.GetStringValue(), privateKey, version);
        connector.SendTransaction(transactionInteractionApprove, response => OnSendTransactionSuccess(response),
            errorMessage => OnSendTransactionError(errorMessage));
    }
    private static void OnSendTransactionError(JsonRpcResponse errorMessage)
    {
        Console.WriteLine("Error: ");
        Console.WriteLine(errorMessage.result);
    }
    private static void OnSendTransactionSuccess(JsonRpcResponse response)
    {
        Console.WriteLine("Success: ");
        Console.WriteLine(response.result);
    }
    private static string OnSendCallContractError(string response)
    {
        return response;
    }

    private static string OnSendCallContractSuccess(string response)
    {
        return response;
    }
    
}