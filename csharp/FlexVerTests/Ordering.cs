namespace FlexVerTests;


public enum Ordering
{
	Less = -1,
	Equal = 0,
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
