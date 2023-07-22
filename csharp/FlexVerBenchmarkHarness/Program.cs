using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FlexVer;

BenchmarkRunner.Run<Benchmarker>();

[MemoryDiagnoser]
public class Benchmarker
{
	private List<(string, string)> _versionsToCompare = null!;

	[Benchmark]
	public void CompareAll_New()
	{
		foreach (var (a, b) in _versionsToCompare) {
			// FlexVerComparerV2.Compare(a, b);
		}
	}

	[Benchmark]
	public void CompareAll_Existing()
	{
		foreach (var (a, b) in _versionsToCompare) {
			FlexVerComparer.Compare(a, b);
		}
	}

#region TestData
	[GlobalSetup]
	public void Setup()
	{
		_versionsToCompare = VersionsToTest.Split('\n')
			.Where(line => !line.StartsWith('#'))
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.Select(line =>
			{
				var split = line.Split(' ');
				if (split.Length != 3) throw new ArgumentException($"Line formatted incorrectly, expected 2 spaces: '{line}'");
				return (split[0], split[2]);
			})
			.ToList();
	}

	private const string VersionsToTest = """
		# This test file is formatted as "<lefthand> <operator> <righthand>", seperated by the space character
		# Implementations should ignore lines starting with "#" and lines that have a length of 0

		# Basic numeric ordering (lexical string sort fails these)
		10 > 2
		100 > 10

		# Trivial common numerics
		1.0 < 1.1
		1.0 < 1.0.1
		1.1 > 1.0.1

		# SemVer compatibility
		1.5 > 1.5-pre1
		1.5 = 1.5+foobar

		# SemVer incompatibility
		1.5 < 1.5-2
		1.5-pre10 > 1.5-pre2

		# Check boundary between textual and prerelease
		a-a < a

		# Check boundary between textual and appendix
		a+a = a

		# Dash is included in prerelease comparison (if stripped it will be a smaller component)
		# Note that a-a < a=a regardless since the prerelease splits the component creating a smaller first component; 0 is added to force splitting regardless
		a0-a < a0=a

		# Pre-releases must contain only non-digit
		1.16.5-10 > 1.16.5

		# Pre-releases can have multiple dashes (should not be split)
		# Reasoning for test data: "p-a!" > "p-a-" (correct); "p-a!" < "p-a t-" (what happens if every dash creates a new component)
		-a- > -a!

		# Misc
		b1.7.3 > a1.2.6
		b1.2.6 > a1.7.3
		a1.1.2 < a1.1.2_01
		1.16.5-0.00.5 > 1.14.2-1.3.7
		1.0.0 < 1.0.0_01
		1.0.1 > 1.0.0_01
		1.0.0_01 < 1.0.1
		0.17.1-beta.1 < 0.17.1
		0.17.1-beta.1 < 0.17.1-beta.2
		1.4.5_01 = 1.4.5_01+fabric-1.17
		1.4.5_01 = 1.4.5_01+fabric-1.17+ohgod
		14w16a < 18w40b
		18w40a < 18w40b
		1.4.5_01+fabric-1.17 < 18w40b
		13w02a < c0.3.0_01
		0.6.0-1.18.x < 0.9.beta-1.18.x
		19283091283091283091283901283901289031289031283917280371230912730917290371290371209731209371209312.skfjslakdfjklasjdfklasjdklfjasdlkfjasdlkjflaskdjflkasdjflksadjfklasdjfklsadjfklasdjkfljsakldfjalskjdflas.akdaljskda > 1029301293019231.12312.21312312
		""";
#endregion TestData
}
