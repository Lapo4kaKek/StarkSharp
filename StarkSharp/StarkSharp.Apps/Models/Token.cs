using System.ComponentModel;
using System.Reflection;

namespace StarkSharpApp.StarkSharp.StarkSharp.StarkSharp.Apps.Models;

public class Token
{
    public required string Name { get; init; }
    public required string ContractAddress { get; init; }
    public required string? Abi { get; init; }
    public required string Network { get; init; }
}
public enum ChainId
{
    [Description("0x534e5f4d41494e")]
    Mainnet,

    [Description("0x534e5f474f45524c49")]
    Goerli
}

public static class ChainIdExtensions
{
    public static string GetStringValue(this ChainId chainId)
    {
        Type type = chainId.GetType();
        string name = Enum.GetName(type, chainId);
        if (name != null)
        {
            FieldInfo field = type.GetField(name);
            if (field != null && Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
            {
                return attr.Description;
            }
        }
        return null;
    }
}
