
/*
 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 *
 * See <http://creativecommons.org/publicdomain/zero/1.0/>
 */

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

[assembly:InternalsVisibleTo("FlexVerTests")]

namespace FlexVer;

/**
 * Implements FlexVer, a SemVer-compatible intuitive comparator for free-form versioning strings as
 * seen in the wild. It's designed to sort versions like people do, rather than attempting to force
 * conformance to a rigid and limited standard. As such, it imposes no restrictions. Comparing two
 * versions with differing formats will likely produce nonsensical results (garbage in, garbage out),
 * but best effort is made to correct for basic structural changes, and versions of differing length
 * will be parsed in a logical fashion.
 */
public static class FlexVerComparer
{
	private static readonly Rune AppendixStartCp = new('+');
	private static readonly Rune PreReleaseStartCp = new('-');
	private static readonly Rune ZeroCp = new('0');

	public static IComparer<string> Default { get; } = new FlexVerComparerImpl();

	private sealed class FlexVerComparerImpl : IComparer<string>
	{
		// ReSharper disable once MemberHidesStaticFromOuterClass
		public int Compare(string? x, string? y)
		{
			if (x is null) return y is null ? 0 : -1;
			if (y is null) return 1;
			return FlexVerComparer.Compare(x, y);
		}
	}

	/// <summary>
	/// Parse the given strings as freeform version strings, and compare them according to FlexVer.
	/// </summary>
	/// <param name="a">the first version string</param>
	/// <param name="b">the second version string</param>
	/// <returns><c>0</c> if the two versions are equal, a negative number if <c>a &lt; b</c>, or a positive number if <c>a &gt; b</c></returns>
	public static int Compare(string a, string b)
	{
		if (a is null) throw new ArgumentNullException(nameof(a));
		if (b is null) throw new ArgumentNullException(nameof(b));

		var offsetA = 0;
		var offSetB = 0;
		Span<Rune> codepointsA = stackalloc Rune[32]; // 32 arbitrarily chosen
		Span<Rune> codepointsB = stackalloc Rune[32];
		bool aHitAppendix = false;
		bool bHitAppendix = false;
		while (true) {
			var ac = GetNextVersionComponent(a, ref offsetA, ref aHitAppendix, codepointsA);
			var bc = GetNextVersionComponent(b, ref offSetB, ref bHitAppendix, codepointsB);

			if (ac.ComponentType is VersionComponentType.Null && bc.ComponentType is VersionComponentType.Null) {
				return 0;
			}

			int c = VersionComponent.CompareTo(ac, bc);
			if (c != 0) return c;
			codepointsA.Clear();
			codepointsB.Clear();
		}
	}

	internal enum VersionComponentType
	{
		Default,
		SemVerPrerelease,
		Numeric,
		Null,
	}

	[DebuggerDisplay("{ComponentType} | '{this.ToString()}'")]
	internal ref struct VersionComponent
	{
		public ReadOnlySpan<Rune> Codepoints { get; }
		public VersionComponentType ComponentType { get; }

		public VersionComponent(ReadOnlySpan<Rune> codepoints, VersionComponentType componentType)
		{
			Codepoints = codepoints;
			ComponentType = componentType;
		}

		public static int CompareTo(VersionComponent cur, VersionComponent other)
		{
			return cur.ComponentType switch
			{
				VersionComponentType.Default => CompareToBase(cur, other),
				VersionComponentType.Null when other.ComponentType == VersionComponentType.Null => 0,
				VersionComponentType.Null when other.ComponentType == VersionComponentType.SemVerPrerelease => 1,
				VersionComponentType.Null => -CompareToBase(other, cur),
				VersionComponentType.Numeric => CompareToNumeric(cur, other),
				VersionComponentType.SemVerPrerelease => CompareToSemVerPrerelease(cur, other)
			};
		}

		public static int CompareToBase(VersionComponent cur, VersionComponent other)
		{
			if (other.ComponentType == VersionComponentType.Null) return 1;

			ReadOnlySpan<Rune> a = cur.Codepoints;
			ReadOnlySpan<Rune> b = other.Codepoints;

			for (int i = 0; i < Math.Min(a.Length, b.Length); i++) {
				Rune c1 = a[i];
				Rune c2 = b[i];
				if (c1 != c2) return c1.Value - c2.Value;
			}

			return a.Length - b.Length;
		}

		public static int CompareToNumeric(VersionComponent cur, VersionComponent that)
		{
			if (that.ComponentType == VersionComponentType.Null) return 1;
			if (that.ComponentType == VersionComponentType.Numeric) {
				ReadOnlySpan<Rune> a = RemoveLeadingZeroes(cur.Codepoints);
				ReadOnlySpan<Rune> b = RemoveLeadingZeroes(that.Codepoints);
				if (a.Length != b.Length) return a.Length-b.Length;
				for (int i = 0; i < a.Length; i++) {
					Rune ad = a[i];
					Rune bd = b[i];
					if (ad != bd) return ad.Value - bd.Value;
				}
				return 0;
			}
			return CompareToBase(cur, that);
		}

		public static int CompareToSemVerPrerelease(VersionComponent left, VersionComponent right)
		{
			if (right.ComponentType == VersionComponentType.Null) return -1; // opposite order
			return CompareToBase(left, right);
		}

		private static ReadOnlySpan<Rune> RemoveLeadingZeroes(ReadOnlySpan<Rune> a)
		{
			if (a.Length == 1) return a;
			int i = 0;
			int stopIdx = a.Length - 1;
			while (i < stopIdx && a[i] == ZeroCp) {
				i++;
			}
			return a[i..];
		}

		public override string ToString() => string.Join("", Codepoints.ToArray().Select(rune => rune.ToString()));
	}

	internal static VersionComponent GetNextVersionComponent(
		ReadOnlySpan<char> str,
		ref int i,
		ref bool hitAppendix,
		Span<Rune> writableComponentCodepoints)
	{
		if (str.Length == i || hitAppendix) {
			return new VersionComponent(ReadOnlySpan<Rune>.Empty, VersionComponentType.Null);
		}

		bool lastWasNumber = char.IsAsciiDigit(str[i]);

		ValueListBuilder<Rune> builder = new ValueListBuilder<Rune>(writableComponentCodepoints);

		while (i < str.Length) {
			Rune.DecodeFromUtf16(str[i..], out Rune cp, out int charsConsumed);

			if (cp == AppendixStartCp) {
				hitAppendix = true;
				break;
			}

			bool isNumber = cp.IsAscii && Rune.IsDigit(cp);
			if (// Ending a Number component
				isNumber != lastWasNumber
				// Starting a new PreRelease component
			    || (cp == PreReleaseStartCp && builder.Length > 0 && builder[0] != PreReleaseStartCp)
			) {
				return CreateComponent(lastWasNumber, builder.AsSpan());
			}
			builder.Append(cp);
			i += charsConsumed;
		}
		return CreateComponent(lastWasNumber, builder.AsSpan());
	}

	private static VersionComponent CreateComponent(bool number, ReadOnlySpan<Rune> s)
	{
		if (number) {
			return new VersionComponent(s, VersionComponentType.Numeric);
		}

		if (s.Length > 1 && s[0] == PreReleaseStartCp) {
			return new VersionComponent(s, VersionComponentType.SemVerPrerelease);
		}

		return new VersionComponent(s, VersionComponentType.Default);
	}

}
