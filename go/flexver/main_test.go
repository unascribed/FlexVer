/*
 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 *
 * See <http://creativecommons.org/publicdomain/zero/1.0/>
 */

package flexver

import (
	"fmt"
	"reflect"
	"testing"
	"unicode/utf8"
)

func TestBasicSort(t *testing.T) {
	var input = []string{
		"0.17.2", "0.1.0", "1.0.0", "1000", "0.17.2-pre.1", "1.16.5+pre",
	}
	var expect = []string{
		"0.1.0", "0.17.2-pre.1", "0.17.2", "1.0.0", "1.16.5+pre", "1000",
	}
	VersionSlice(input).Sort()
	if !reflect.DeepEqual(input, expect) {
		t.Fatalf("Failed to sort strings: got %v (expected %v)", input, expect)
	}
}

const (
	opLT        = -1
	opEQ        = 0
	opGT        = 1
	shouldError = 10
)

func opChar(op int) string {
	if op < 0 {
		return "<"
	} else if op == opEQ {
		return "="
	} else if op == opGT {
		return ">"
	} else {
		return "!"
	}
}

type testInstance struct {
	a  string
	op int
	b  string
}

func t(a string, op int, b string) testInstance {
	return testInstance{a: a, op: op, b: b}
}

func (i testInstance) name() string {
	a := i.a
	b := i.b
	if !utf8.ValidString(a) {
		a = "badutf8"
	}
	if !utf8.ValidString(b) {
		b = "badutf8"
	}
	return a + " " + opChar(i.op) + " " + b
}

var specTests = []testInstance{
	t("b1.7.3", opGT, "a1.2.6"),
	t("a1.1.2", opLT, "a1.1.2_01"),
	t("1.16.5-0.00.5", opGT, "1.14.2-1.3.7"),
	t("1.0.0", opLT, "1.0.0_01"),
	t("1.0.1", opGT, "1.0.0_01"),
	t("0.17.1-beta.1", opLT, "0.17.1"),
	t("0.17.1-beta.1", opLT, "0.17.1-beta.2"),
	t("1.4.5_01", opEQ, "1.4.5_01+exp-1.17"),
	t("1.4.5_01", opEQ, "1.4.5_01+exp-1.17-moretext"),
	t("14w16a", opLT, "18w40b"),
	t("18w40a", opLT, "18w40b"),
	t("1.4.5_01+exp-1.17", opLT, "18w40b"),
	t("13w02a", opLT, "c0.3.0_01"),
	t("0.6.0-1.18.x", opLT, "0.9.beta-1.18.x"),
	t("36893488147419103232", opLT, "36893488147419103233"),
	t("1.0", opLT, "1.1"),
	t("1.0", opLT, "1.0.1"),
	t("10", opGT, "2"),
}

var coverageTests = []testInstance{
	// Empty strings
	t("", opEQ, ""),
	t("1", opGT, ""),
	t("", opLT, "1"),
	// Invalid UTF8
	t("\xc3\x28", shouldError, ""),
	t("", shouldError, "\xc3\x28"),
}

var allTests = append(append([]testInstance{}, specTests...), coverageTests...)

func TestCompare(t *testing.T) {
	for _, v := range allTests {
		t.Run(v.name(), func(t *testing.T) {
			if v.op == shouldError {
				// Catch panic
				defer func() { _ = recover() }()
			}

			res := Compare(v.a, v.b)
			if (v.op == opLT && res >= 0) ||
				(v.op == opEQ && res != 0) ||
				(v.op == opGT && res <= 0) ||
				(v.op == shouldError) {
				t.Fatalf("Unexpected result %v", res)
			}
		})
	}
}

func TestLess(t *testing.T) {
	for _, v := range allTests {
		t.Run(v.name(), func(t *testing.T) {
			if v.op == shouldError {
				// Catch panic
				defer func() { _ = recover() }()
			}

			res := Less(v.a, v.b)
			if (v.op == opLT && res == false) ||
				(v.op == opEQ && res == true) ||
				(v.op == opGT && res == true) ||
				(v.op == shouldError) {
				t.Fatalf("Unexpected result %v", res)
			}
		})
	}
}

func TestEqual(t *testing.T) {
	for _, v := range allTests {
		t.Run(v.name(), func(t *testing.T) {
			if v.op == shouldError {
				// Catch panic
				defer func() { _ = recover() }()
			}

			res := Equal(v.a, v.b)
			if (v.op == opLT && res == true) ||
				(v.op == opEQ && res == false) ||
				(v.op == opGT && res == true) ||
				(v.op == shouldError) {
				t.Fatalf("Unexpected result %v", res)
			}
		})
	}
}

func TestCompareError(t *testing.T) {
	for _, v := range allTests {
		t.Run(v.name(), func(t *testing.T) {
			res, err := CompareError(v.a, v.b)
			if err == nil {
				if (v.op == opLT && res >= 0) ||
					(v.op == opEQ && res != 0) ||
					(v.op == opGT && res <= 0) ||
					(v.op == shouldError) {
					t.Fatalf("Unexpected result %v", res)
				}
			} else if v.op != shouldError {
				t.Fatalf("Unexpected error %v", err)
			}
		})
	}
}

func TestLessError(t *testing.T) {
	for _, v := range allTests {
		t.Run(v.name(), func(t *testing.T) {
			res, err := LessError(v.a, v.b)
			if err == nil {
				if (v.op == opLT && res == false) ||
					(v.op == opEQ && res == true) ||
					(v.op == opGT && res == true) ||
					(v.op == shouldError) {
					t.Fatalf("Unexpected result %v", res)
				}
			} else if v.op != shouldError {
				t.Fatalf("Unexpected error %v", err)
			}
		})
	}
}

func TestEqualError(t *testing.T) {
	for _, v := range allTests {
		t.Run(v.name(), func(t *testing.T) {
			res, err := EqualError(v.a, v.b)
			if err == nil {
				if (v.op == opLT && res == true) ||
					(v.op == opEQ && res == false) ||
					(v.op == opGT && res == true) ||
					(v.op == shouldError) {
					t.Fatalf("Unexpected result %v", res)
				}
			} else if v.op != shouldError {
				t.Fatalf("Unexpected error %v", err)
			}
		})
	}
}

func ExampleCompare() {
	fmt.Println(Compare("1.0.1", "1.0.3"), Compare("10.0.0", "1.0.1"))
	// Output: -2 1
}

func ExampleLess() {
	fmt.Println(Less("10.0.0", "1.0.1"), Less("10.0.0", "10.1.0.2"))
	// Output: false true
}

func ExampleEqual() {
	fmt.Println(Equal("1.0.0+fluffy", "1.0.0"), Equal("1.0.0", "1.0.0-pre.1"))
	// Output: true false
}

func ExampleVersionSlice_Sort() {
	// Type alias, works identically to []string
	versions := VersionSlice{"100", "1.0.2", "0.1.2", "0.3.4-pre"}
	// or VersionSlice([]string{ ... })
	versions.Sort()
	fmt.Println(versions)
	// Output: [0.1.2 0.3.4-pre 1.0.2 100]
}
