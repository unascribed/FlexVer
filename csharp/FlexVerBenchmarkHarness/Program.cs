using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FlexVer;

BenchmarkRunner.Run<Benchmarker>();

[MemoryDiagnoser]
public class Benchmarker
{
	private List<(string, string)> _versionsToCompare = null!;

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
		_versionsToCompare = File.ReadAllLines("test_vectors.txt")
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
#endregion TestData
}
