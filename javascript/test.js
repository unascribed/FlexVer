var flexVerCompare = require("./index.js");

function signum(i) {
	return i < 0 ? -1 : i > 0 ? 1 : 0;
}

function test(a, b, expect) {
	var c = signum(flexVerCompare(a, b));
	var c2 = signum(flexVerCompare(b, a));
	if (-c2 != c) {
		throw new Error("Comparison method violates its general contract! ("+a+" <=> "+b+" is not commutative)");
	}
	var res = "";
	if (c < 0) res = "<";
	if (c == 0) res = "=";
	if (c > 0) res = ">";
	console.log(a+" "+res+" "+b+(c != expect ? " DOES NOT MATCH" : ""));
}

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
