
/*
 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 *
 * See <http://creativecommons.org/publicdomain/zero/1.0/>
 */

using System.Globalization;
using System.Runtime.CompilerServices;

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
public class FlexVerComparer {


	/// <summary>
	/// Parse the given strings as freeform version strings, and compare them according to FlexVer.
	/// </summary>
	/// <param name="a">the first version string</param>
	/// <param name="b">the second version string</param>
	/// <returns><c>0</c> if the two versions are equal, a negative number if <c>a &lt; b</c>, or a positive number if <c>a &gt; b</c></returns>
	public static int Compare(string a, string b) {
		List<VersionComponent> ad = Decompose(a);
		List<VersionComponent> bd = Decompose(b);
		int highestCount = Math.Max(ad.Count, bd.Count);
		for (int i = 0; i < highestCount; i++) {
			int c = Get(ad, i).CompareTo(Get(bd, i));
			if (c != 0) return c;
		}
		return 0;
	}

	private static readonly VersionComponent Null = NullVersionComponent.Instance;

	internal class NullVersionComponent : VersionComponent
	{
		public static NullVersionComponent Instance { get; } = new();

		public override int CompareTo(VersionComponent other)
		=> ReferenceEquals(other, Null) ? 0 : -other.CompareTo(this);

		private NullVersionComponent() : base(Array.Empty<int>())
		{ }
	}

	internal class VersionComponent
	{
		public int[] Codepoints { get; }

		public VersionComponent(int[] codepoints)
		{
			Codepoints = codepoints;
		}

		public virtual int CompareTo(VersionComponent that)
		{
			if (ReferenceEquals(that, Null)) return 1;

			int[] a = this.Codepoints;
			int[] b = that.Codepoints;

			for (int i = 0; i < Math.Min(a.Length, b.Length); i++) {
				int c1 = a[i];
				int c2 = b[i];
				if (c1 != c2) return c1 - c2;
			}

			return a.Length - b.Length;
		}

		public override string ToString() => new string(Codepoints.Select(el => (char)el).ToArray());
	}

	internal sealed class SemVerPrereleaseVersionComponent : VersionComponent
	{
		public SemVerPrereleaseVersionComponent(int[] codepoints) : base (codepoints) { }

		public override int CompareTo(VersionComponent that)
		{
			if (ReferenceEquals(that, Null)) return -1; // opposite order
			return base.CompareTo(that);
		}
	}

	internal sealed class NumericVersionComponent : VersionComponent
	{
		public NumericVersionComponent(int[] codepoints) : base(codepoints) { }

		public override int CompareTo(VersionComponent that) {
			if (ReferenceEquals(that, Null)) return 1;
			if (that is NumericVersionComponent) {
				int[] a = RemoveLeadingZeroes(this.Codepoints);
				int[] b = RemoveLeadingZeroes(that.Codepoints);
				if (a.Length != b.Length) return a.Length-b.Length;
				for (int i = 0; i < a.Length; i++) {
					int ad = a[i];
					int bd = b[i];
					if (ad != bd) return ad-bd;
				}
				return 0;
			}
			return base.CompareTo(that);
		}

		private static int[] RemoveLeadingZeroes(int[] a) {
			if (a.Length == 1) return a;
			int i = 0;
			while (i < a.Length && a[i] == '0') {
				i++;
			}
			return a[i..];
		}

	}

	/*
	 * Break apart a string into intuitive version components, by splitting it where a run of
	 * characters changes from numeric to non-numeric.
	 */
	internal static List<VersionComponent> Decompose(string str) {
		// TODO: should this NRE, like in the java version, or treat `null` as `""`?
		if (string.Empty == str) return new List<VersionComponent>();
		bool lastWasNumber = char.IsAsciiDigit(str[0]);
		var stringInfo = new StringInfo(str);
		int totalCodepoints = stringInfo.LengthInTextElements;
		int[] accum = new int[totalCodepoints];
		List<VersionComponent> outComponents = new();
		int j = 0;

		for (int i = 0; i < str.Length; i++) {
			char cp = str[i];
			if (char.IsHighSurrogate(cp)) i++;
			if (cp == '+') break; // remove appendices
			bool isNumber = char.IsAsciiDigit(cp);
			if (isNumber != lastWasNumber || (cp == '-' && j > 0 && accum[0] != '-')) {
				outComponents.Add(CreateComponent(lastWasNumber, accum, j));
				j = 0;
				lastWasNumber = isNumber;
			}
			accum[j] = cp;
			j++;
		}
		outComponents.Add(CreateComponent(lastWasNumber, accum, j));
		return outComponents;
	}

	private static VersionComponent CreateComponent(bool number, int[] s, int j)
	{
		s = s[..j];

		if (number) {
			return new NumericVersionComponent(s);
		}

		if (s.Length > 1 && s[0] == '-') {
			return new SemVerPrereleaseVersionComponent(s);
		}

		return new VersionComponent(s);
	}

	private static VersionComponent Get(List<VersionComponent> li, int i)
	=> i >= li.Count ? Null : li[i];
}
