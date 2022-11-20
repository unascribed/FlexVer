package com.unascribed.flexver;

import java.util.List;

import com.unascribed.flexver.FlexVerComparator.NumericVersionComponent;
import com.unascribed.flexver.FlexVerComparator.SemVerPrereleaseVersionComponent;
import com.unascribed.flexver.FlexVerComparator.VersionComponent;

public class FlexVerBasicTest {

	private static final boolean ANSI = false;
	
	private static void test(String a, String b, int expect) {
		int c = signum(FlexVerComparator.compare(a, b));
		int c2 = signum(FlexVerComparator.compare(b, a));
		if (-c2 != c) {
			throw new IllegalArgumentException("Comparison method violates its general contract! ("+a+" <=> "+b+" is not commutative)");
		}
		if (c != expect) throw new IllegalArgumentException("Expected "+expect+", got "+c);
		String res = "";
		if (c < 0) res = "<";
		if (c == 0) res = "=";
		if (c > 0) res = ">";
		System.out.println(represent(a)+(ANSI?"\u001B[0m ":" ")+res+" "+represent(b));
	}
	
	private static int signum(int i) {
		return i < 0 ? -1 : i > 0 ? 1 : 0;
	}

	private static String represent(String str) {
		List<VersionComponent> d = FlexVerComparator.decompose(str);
		StringBuilder out = new StringBuilder(ANSI ? "" : "`");
		for (VersionComponent vc : d) {
			if (ANSI) {
				int color = 90;
				if (vc instanceof NumericVersionComponent) {
					color = 96;
				} else if (vc instanceof SemVerPrereleaseVersionComponent) {
					color = 91;
				} else {
					color = 95;
				}
				out.append("\u001B[").append(color).append("m");
			}
			out.append(vc);
			if (!ANSI) {
				out.append(" ");
			}
		}
		if (str.contains("+")) {
			if (ANSI) {
				out.append("\u001B[90m");
			}
			out.append(str.substring(str.indexOf('+')));
			out.append(" ");
		}
		if (!ANSI) {
			out.setLength(out.length()-1);
			out.append("`");
		}
		return out.toString();
	}

	public static void main(String[] args) {
		test("b1.7.3", "a1.2.6", 1);
		test("b1.2.6", "a1.7.3", 1);
		test("a1.1.2", "a1.1.2_01", -1);
		test("1.16.5-0.00.5", "1.14.2-1.3.7", 1);
		test("1.0.0", "1.0.0_01", -1);
		test("1.0.1", "1.0.0_01", 1);
		test("1.0.0_01", "1.0.1", -1);
		test("0.17.1-beta.1", "0.17.1", -1);
		test("0.17.1-beta.1", "0.17.1-beta.2", -1);
		test("1.4.5_01", "1.4.5_01+fabric-1.17", 0);
		test("1.4.5_01", "1.4.5_01+fabric-1.17+ohgod", 0);
		test("14w16a", "18w40b", -1);
		test("18w40a", "18w40b", -1);
		test("1.4.5_01+fabric-1.17", "18w40b", -1);
		test("13w02a", "c0.3.0_01", -1);
		test("0.6.0-1.18.x", "0.9.beta-1.18.x", -1);
		// 2^65. Too large for a 64-bit integer or a double
		test("36893488147419103232", "36893488147419103233", -1);
		test("37", "12", 1);
		test("12", "13", -1);
		test("12", "21", -1);
		test("43", "103", -1);

		test("1.0", "1.1", -1);
	}

}
