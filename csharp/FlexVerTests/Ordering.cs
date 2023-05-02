using System.Runtime.Serialization;

namespace FlexVerTests;

#pragma warning disable CS8524 // Exhaustive switch (e.g. underlying type cast to enum). Disabling to enable SwitchExpressionException.

public enum Ordering
{
    [EnumMember(Value = "<")]
    Less = -1,
    [EnumMember(Value = "=")]
    Equal = 0,
    [EnumMember(Value = ">")]
    Greater = 1,
}

public static class OrderingExtensions
{
    public static Ordering FromChar(string ordering)
    => ordering switch
    {
         "<" => Ordering.Less,
         "=" => Ordering.Equal,
         ">" => Ordering.Greater,
         _ => throw new ArgumentException("Invalid char for Ordering")
    };

    public static string ToChar(this Ordering ordering)
        => ordering switch
        {
            Ordering.Less => "<",
            Ordering.Equal => "=",
            Ordering.Greater => ">",
        };

    public static Ordering Invert(this Ordering ordering)
    => ordering switch
    {
        Ordering.Less => Ordering.Greater,
        Ordering.Equal => Ordering.Equal,
        Ordering.Greater => Ordering.Less,
    };

    public static Ordering FromComparison(int comparisonResult)
    => comparisonResult switch
    {
        < 0 => Ordering.Less,
        0 => Ordering.Equal,
        > 0 => Ordering.Greater,
    };
}
