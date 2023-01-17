/*
 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 *
 * See <http://creativecommons.org/publicdomain/zero/1.0/>
 */

package flexver

import (
	"bufio"
	"fmt"
	"os"
	"reflect"
	"strings"
	"testing"
)

var ENABLED_TESTS = []string{
	"test_vectors.txt",
	"large.txt",
}

const (
	opLT = -1
	opEQ = 0
	opGT = 1
)

func RunCompare(t *testing.T, lefthand string, righthand string, ordering int) {
	res := Compare(lefthand, righthand)
	if (ordering == opLT && !(res < 0)) ||
		(ordering == opEQ && !(res == 0)) ||
		(ordering == opGT && !(res > 0)) {
		t.Errorf("Compare returned %v", res)
	}

	res2, err := CompareError(lefthand, righthand)
	if err != nil {
		t.Fatalf("CompareError returned an unexpected error: %v", err)
	}

	if res != res2 {
		t.Error("CompareError did not give the same result as Compare")
	}
}

func RunLess(t *testing.T, lefthand string, righthand string, ordering int) {
	res := Less(lefthand, righthand)
	if ordering == opLT {
		if !res {
			t.Error("Less incorrectly returned false")
		}
	} else {
		if res {
			t.Error("Less incorrectly returned true")
		}
	}

	res2, err := LessError(lefthand, righthand)
	if err != nil {
		t.Fatalf("LessError returned an unexpected error: %v", err)
	}

	if res != res2 {
		t.Error("LessError did not give the same result as Less")
	}
}

func RunEqual(t *testing.T, lefthand string, righthand string, ordering int) {
	res := Equal(lefthand, righthand)
	if ordering == opEQ {
		if !res {
			t.Error("Equal incorrectly returned false")
		}
	} else {
		if res {
			t.Error("Equal incorrectly returned true")
		}
	}

	res2, err := EqualError(lefthand, righthand)
	if err != nil {
		t.Fatalf("EqualError returned an unexpected error: %v", err)
	}

	if res != res2 {
		t.Error("EqualError did not give the same result as Equal")
	}
}

func TestStandardized(t *testing.T) {
	for _, test := range ENABLED_TESTS {
		ProcessTestfile(t, test)
	}
}

func ProcessTestfile(t *testing.T, test string) {
	file, err := os.Open("../../test/" + test)
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()

		if strings.HasPrefix(line, "#") {
			continue
		}
		if len(line) == 0 {
			continue
		}

		split := strings.Split(line, " ")
		if len(split) != 3 {
			t.Fatal("Line formatted incorrectly, expected 2 spaces: " + line)
		}

		ord := 0
		switch split[1] {
		case "<":
			ord = opLT
		case "=":
			ord = opEQ
		case ">":
			ord = opGT
		}

		lefthand := split[0]
		righthand := split[2]

		t.Run(line, func(t *testing.T) {
			t.Run("Compare", func(t *testing.T) {
				RunCompare(t, lefthand, righthand, ord)
			})

			t.Run("Less", func(t *testing.T) {
				RunLess(t, lefthand, righthand, ord)
			})

			t.Run("Equal", func(t *testing.T) {
				RunEqual(t, lefthand, righthand, ord)
			})
		})
	}

	if err := scanner.Err(); err != nil {
		t.Fatal(err)
	}
}

func TestInvalid(t *testing.T) {
	// Run through some invalid inputs and fail the test if it didn't return an error
	_, err := CompareError("\xc3\x28", "")
	if err == nil {
		t.Fatal()
	}
	_, err = LessError("\xc3\x28", "")
	if err == nil {
		t.Fatal()
	}
	_, err = EqualError("\xc3\x28", "")
	if err == nil {
		t.Fatal()
	}
	_, err = CompareError("", "\xc3\x28")
	if err == nil {
		t.Fatal()
	}
	_, err = LessError("", "\xc3\x28")
	if err == nil {
		t.Fatal()
	}
	_, err = EqualError("", "\xc3\x28")
	if err == nil {
		t.Fatal()
	}
}

func TestInvalidPanic(t *testing.T) {
	// Ensures that the AssertPanic function is working, as Go doesn't have any built-in way to assert a function panics
	if !DetectPanic(func() { panic("Test") }) || DetectPanic(func() {}) {
		t.Fatal()
	}

	// Run through some invalid inputs and fail the test if it doesn't panic
	if !DetectPanic(func() { Compare("\xc3\x28", "") }) {
		t.Fatal()
	}

	if !DetectPanic(func() { Less("\xc3\x28", "") }) {
		t.Fatal()
	}

	if !DetectPanic(func() { Equal("\xc3\x28", "") }) {
		t.Fatal()
	}

	if !DetectPanic(func() { Compare("", "\xc3\x28") }) {
		t.Fatal()
	}

	if !DetectPanic(func() { Less("", "\xc3\x28") }) {
		t.Fatal()
	}

	if !DetectPanic(func() { Equal("", "\xc3\x28") }) {
		t.Fatal()
	}

}

// Will return true if the given function panics and false if it returned correctly
func DetectPanic(f func()) (ret bool) {
	defer func() {
		if r := recover(); r != nil {
			ret = true
		}
	}()

	f()

	return false // If we reached this statement, we didn't panic
}

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
