/*
 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 *
 * See <http://creativecommons.org/publicdomain/zero/1.0/>
 */

package com.unascribed.flexver;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * Implements FlexVer, a SemVer-compatible intuitive comparator for free-form versioning strings as
 * seen in the wild. It's designed to sort versions like people do, rather than attempting to force
 * conformance to a rigid and limited standard. As such, it imposes no restrictions. Comparing two
 * versions with differing formats will likely produce nonsensical results (garbage in, garbage out),
 * but best effort is made to correct for basic structural changes, and versions of differing length
 * will be parsed in a logical fashion.
 */
public class FlexVerComparator {
	
	/**
	 * Parse the given strings as freeform version strings, and compare them according to FlexVer.
	 * @param a the first version string
	 * @param b the second version string
	 * @return {@code 0} if the two versions are equal, a negative number if {@code a < b}, or a positive number if {@code a > b}
	 */
	public static int compare(String a, String b) {
		List<VersionComponent> ad = decompose(a);
		List<VersionComponent> bd = decompose(b);
		for (int i = 0; i < Math.max(ad.size(), bd.size()); i++) {
			VersionComponent ac = get(ad, i);
			VersionComponent bc = get(bd, i);
			int c = ac.compareTo(bc);
			if (c != 0) return c;
		}
		return 0;
	}
	

	private static final VersionComponent NULL = new VersionComponent() {
		@Override
		public int compareTo(VersionComponent other) { return other == NULL ? 0 : -other.compareTo(this); }
		@Override
		public String toString() { return ""; }
	};
	
	// @VisibleForTesting
	interface VersionComponent {
		int compareTo(VersionComponent other);
	}
	
	// @VisibleForTesting
	static class LiteralVersionComponent implements VersionComponent {
		private final String str;
		
		public LiteralVersionComponent(String str) { this.str = str; }
		
		@Override
		public int compareTo(VersionComponent other) {
			if (other == NULL) return 1;
			return toString().compareTo(other.toString());
		}
		
		@Override
		public String toString() {
			return str;
		}
	}
	
	// @VisibleForTesting
	static class NumericVersionComponent implements VersionComponent {
		private final String strValue;
		private final long value;
		
		public NumericVersionComponent(String value) {
			this.strValue = value;
			// just in case someone uses a pointlessly long version string...
			this.value = Long.parseLong(value);
		}
		
		public long value() {
			return value;
		}
		
		@Override
		public int compareTo(VersionComponent other) {
			if (other == NULL) return 1;
			if (other instanceof NumericVersionComponent)
				return Long.compare(value(), ((NumericVersionComponent)other).value());
			return toString().compareTo(other.toString());
		}
		
		@Override
		public String toString() {
			return strValue;
		}
	}
	
	// @VisibleForTesting
	static class SemVerPrereleaseVersionComponent implements VersionComponent {
		private final String str;
		
		public SemVerPrereleaseVersionComponent(String str) { this.str = str; }
		
		@Override
		public int compareTo(VersionComponent other) {
			if (other == NULL) return -1; // opposite order
			return toString().compareTo(other.toString());
		}
		
		@Override
		public String toString() {
			return str;
		}
	}
	
	/*
	 * Break apart a string into intuitive version components, by splitting it where a run of
	 * characters changes from numeric to non-numeric.
	 */
	// @VisibleForTesting
	static List<VersionComponent> decompose(String str) {
		if (str.isEmpty()) return Collections.emptyList();
		boolean lastWasNumber = Character.isDigit(str.codePointAt(0));
		StringBuilder accum = new StringBuilder();
		List<VersionComponent> out = new ArrayList<>();
		// remove appendices
		int plus = str.indexOf('+');
		if (plus != -1) str = str.substring(0, plus);
		for (int i = 0; i < str.length(); i++) {
			if (i > 0 && Character.isHighSurrogate(str.charAt(i-1)) && Character.isLowSurrogate(str.charAt(i))) continue;
			int cp = str.codePointAt(i);
			boolean number = Character.isDigit(cp);
			if (number != lastWasNumber) {
				out.add(createComponent(lastWasNumber, accum.toString()));
				accum.setLength(0);
				lastWasNumber = number;
			}
			accum.appendCodePoint(cp);
		}
		out.add(createComponent(lastWasNumber, accum.toString()));
		return out;
	}

	private static VersionComponent createComponent(boolean number, String s) {
		if (number) {
			return new NumericVersionComponent(s);
		} else if (s.length() > 1 && s.charAt(0) == '-') {
			return new SemVerPrereleaseVersionComponent(s);
		} else {
			return new LiteralVersionComponent(s);
		}
	}

	private static VersionComponent get(List<VersionComponent> li, int i) {
		return i >= li.size() ? NULL : li.get(i);
	}

}
