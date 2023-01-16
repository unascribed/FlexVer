const flexVerCompare = require("./dist/node.js");
const fs = require('fs');

const ENABLED_TESTS = [ "test_vectors.txt", "large.txt" ];

function signum(i) {
	return i < 0 ? -1 : i > 0 ? 1 : 0;
}

function runTest(a, b, expect) {
	var c = signum(flexVerCompare(a, b));
	var c2 = signum(flexVerCompare(b, a));
	if (-c2 != c) {
		console.log(`FAIL: Comparison method violates its general contract! (${a} <=> ${b} is not commutative)`);
	}
	var res = "";
	if (c < 0) res = "<";
	if (c == 0) res = "=";
	if (c > 0) res = ">";

	if (res != expect) {
		console.log(`FAIL: expected ${a} ${expect} ${b} but got '${res}'`);
	}
}

ENABLED_TESTS.forEach(test => {
	fs.readFile(`../test/${test}`, "utf-8", (err, data) => {
		if (err) throw err;
		data.split("\n")
			.filter(line => !line.startsWith("#"))
			.filter(line => line.length != 0)
			.map(line => {
				var split = line.split(" ")
				if (split.length != 3) throw test+" Line formatted incorrectly, expected 2 spaces: "+line;
				
				var lefthand = split[0];
				var righthand = split[2];
				var ordening = split[1];

				return runTest(lefthand, righthand, ordening)
			})
	})
})
