/*
 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 *
 * See <http://creativecommons.org/publicdomain/zero/1.0/>
 */

package com.unascribed;

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
	
	/*
	 * Break apart a string into "logical" version components, by splitting it where a string
	 * of characters changes from numeric to non-numeric.
	 */
	private static List<Comparable<?>> decompose(String str, boolean debug) {
		if (str.isEmpty()) return Collections.emptyList();
		boolean lastWasNumber = Character.isDigit(str.codePointAt(0));
		StringBuilder accum = new StringBuilder();
		List<Comparable<?>> out = new ArrayList<>();
		// remove appendices
		int plus = str.indexOf('+');
		if (plus != -1) str = str.substring(0, plus);
		for (int i = 0; i < str.length(); i++) {
			if (Character.isLowSurrogate(str.charAt(i))) continue;
			int cp = str.codePointAt(i);
			boolean number = Character.isDigit(cp);
			if (number != lastWasNumber) {
				complete(lastWasNumber, accum, out, debug);
				lastWasNumber = number;
			}
			accum.appendCodePoint(cp);
		}
		complete(lastWasNumber, accum, out, debug);
		return out;
	}

	private static void complete(boolean number, StringBuilder accum, List<Comparable<?>> out, boolean debug) {
		if (debug) {
			out.add("\u001B["+(number?"96":isSemverPrerelease(accum.toString())?"91":"95")+"m"+accum);
		} else {
			String s = accum.toString();
			if (number) {
				// just in case someone uses a pointlessly long version string...
				out.add(Long.parseLong(s));
			} else {
				out.add(s);
			}
		}
		accum.setLength(0);
	}
	
	// it's difficult to generically deal with comparables - these operations are safe
	@SuppressWarnings({ "rawtypes", "unchecked" })
	public static int compare(String a, String b) {
		// Arrays.compare gives wrong precedence to array length, causing 1.0.1 to sort as older than 1.0.0_01
		// so, implement it ourselves from scratch
		List<Comparable<?>> ad = decompose(a, false);
		List<Comparable<?>> bd = decompose(b, false);
		for (int i = 0; i < Math.max(ad.size(), bd.size()); i++) {
			Comparable ac = i >= ad.size() ? null : ad.get(i);
			Comparable bc = i >= bd.size() ? null : bd.get(i);
			if (typeof(ac) != typeof(bc)) {
				// Comparables assume the input object is the same type as the object, so we need to
				// ensure that's true; this will happen in the case of two critically mismatched
				// versions that contain a symbol where a number is expected - in this case, lexical
				// is the best we can do
				ac = ac == null ? null : ac.toString();
				bc = bc == null ? null : bc.toString();
			}
			int c;
			if (bc == null && isSemverPrerelease(ac)) {
				// special case: compatibility with semver, which sorts "pre-releases" differently
				c = -1;
			} else if (ac == null && isSemverPrerelease(bc)) {
				c = 1;
			} else if (ac == null) {
				// special case: nulls are *always* lesser
				// bc cannot be null here, don't need to check
				c = -1;
			} else {
				c = bc == null ? 1 : ac.compareTo(bc);
			}
			if (c != 0) return c;
		}
		return 0;
	}
	
	private static boolean isSemverPrerelease(Object o) {
		if (o instanceof String) {
			String s = (String)o;
			return s.length() > 1 && s.charAt(0) == '-';
		}
		return false;
	}

	private static Class<?> typeof(Object o) {
		return o == null ? null : o.getClass();
	}

	private static void test(String a, String b) {
		int c = signum(compare(a, b));
		int c2 = signum(compare(b, a));
		if (-c2 != c) {
			throw new IllegalArgumentException("Comparison method violates its general contract! ("+a+" <=> "+b+" is not commutative)");
		}
		String res = "";
		if (c < 0) res = "<";
		if (c == 0) res = "=";
		if (c > 0) res = ">";
		System.out.println(represent(a)+"\u001B[0m "+res+" "+represent(b));
	}
	
	private static int signum(int i) {
		return i < 0 ? -1 : i > 0 ? 1 : 0;
	}

	private static String represent(String str) {
		List<Comparable<?>> d = decompose(str, true);
		boolean odd = true;
		StringBuilder out = new StringBuilder();
		for (Object o : d) {
			int color = o instanceof Number ? 96 : 95;
			out.append("\u001B[").append(color).append("m");
			out.append(o);
			odd = !odd;
		}
		if (str.contains("+")) {
			out.append("\u001B[90m");
			out.append(str.substring(str.indexOf('+')));
		}
		return out.toString();
	}

	public static void main(String[] args) {
		test("b1.7.3", "a1.2.6");
		test("b1.2.6", "a1.7.3");
		test("a1.1.2", "a1.1.2_01");
		test("1.16.5-0.00.5", "1.14.2-1.3.7");
		test("1.0.0", "1.0.0_01");
		test("1.0.1", "1.0.0_01");
		test("1.0.0_01", "1.0.1");
		test("0.17.1-beta.1", "0.17.1");
		test("0.17.1-beta.1", "0.17.1-beta.2");
		test("1.4.5_01", "1.4.5_01+fabric-1.17");
		test("1.4.5_01", "1.4.5_01+fabric-1.17+ohgod");
		test("14w16a", "18w40b");
		test("18w40a", "18w40b");
		test("1.4.5_01+fabric-1.17", "18w40b");
		test("13w02a", "c0.3.0_01");
		test("0.6.0-1.18.x", "0.9.beta-1.18.x");
	}

}
