using FlexVer;

namespace FlexVerTests;

public class Tests
{
	private const string TestDir = "";
	private static readonly string[] EnabledTests = { "test_vectors.txt", "large.txt" };

	[Test]
	[TestCaseSource(nameof(GetEqualityTests))]
	public void TestEquality_StaticCompare(string a, string b, Ordering expectedOrdering)
	{
		Ordering c = OrderingExtensions.FromComparison(FlexVerComparer.Compare(a, b));
		Ordering c2 = OrderingExtensions.FromComparison(FlexVerComparer.Compare(b, a));

		Assert.That(c, Is.EqualTo(c2.Invert()), $"Comparison method violates its general contract! ({a} <=> {b} is not commutative)");
		Assert.That(c, Is.EqualTo(expectedOrdering), $"OrderingExtensions.FromComparison produced {a} {c} {b}");
	}

	[Test]
	[TestCaseSource(nameof(GetEqualityTests))]
	public void TestEquality_DefaultComparerInstance(string a, string b, Ordering expectedOrdering)
	{
		Ordering c = OrderingExtensions.FromComparison(FlexVerComparer.Default.Compare(a, b));
		Ordering c2 = OrderingExtensions.FromComparison(FlexVerComparer.Default.Compare(b, a));

		Assert.That(c, Is.EqualTo(c2.Invert()), $"Comparison method violates its general contract! ({a} <=> {b} is not commutative)");
		Assert.That(c, Is.EqualTo(expectedOrdering), $"OrderingExtensions.FromComparison produced {a} {c} {b}");
	}

	[Test]
	[TestCase(null, null, Ordering.Equal)]
	[TestCase(null, "1.0.0", Ordering.Less)]
	[TestCase("1.0.0", null, Ordering.Greater)]
	[TestCase("1.0.0", "1.0.0", Ordering.Equal)]
	public void TestEquality_DefaultComparerInstance_HandlesClrNulls(string? a, string? b, Ordering expectedOrdering)
	{
		Ordering c = OrderingExtensions.FromComparison(FlexVerComparer.Default.Compare(a, b));
		Ordering c2 = OrderingExtensions.FromComparison(FlexVerComparer.Default.Compare(b, a));

		Assert.That(c, Is.EqualTo(c2.Invert()), $"Comparison method violates its general contract! ({a} <=> {b} is not commutative)");
		Assert.That(c, Is.EqualTo(expectedOrdering), $"OrderingExtensions.FromComparison produced {a} {c} {b}");
	}


	internal static IEnumerable<object?[]> GetEqualityTests()
	=> EnabledTests
		.SelectMany(testFileName => File.ReadAllLines(Path.Join(TestDir, testFileName)))
		.Where(line => !line.StartsWith('#'))
		.Where(line => !string.IsNullOrWhiteSpace(line))
		.Select(line =>
		{
			var split = line.Split(' ');
			if (split.Length != 3) throw new ArgumentException($"Line formatted incorrectly, expected 2 spaces: {line}");
			return new object?[] { split[0], split[2], OrderingExtensions.FromChar(split[1]) };
		});
}
