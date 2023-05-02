using FlexVer;

namespace FlexVerTests;

public class Tests
{
    private const string TestDir = "";
    private static readonly string[] EnabledTests = { "test_vectors.txt", "large.txt" };

    [Test]
    [TestCaseSource(nameof(GetEqualityTests))]
    public void TestEquality(string a, string b, Ordering expectedOrdering)
    {
        Ordering c = OrderingExtensions.FromComparison(FlexVerComparator.Compare(a, b));
        Ordering c2 = OrderingExtensions.FromComparison(FlexVerComparator.Compare(b, a));

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
