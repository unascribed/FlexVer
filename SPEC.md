# FlexVer Specification 1.1.1

This document describes the FlexVer algorithm at a high level. The concept behind FlexVer is to
offer a standardized and SemVer-compatible intuitive version comparator. Its behavior is designed
to match how a person would compare two version numbers, providing intuitive and "unsurprising"
results.

> **Note**
> The following document provides a high-level logical overview of the algorithm. It does *not*
> describe the most computationally efficient way to do it — or even, in fact, the way it is done
> in the reference implementations. Please check the reference code for these, once this document
> has introduced the concepts to you.

## Unicode Behavior
FlexVer works entirely in terms of Unicode scalar values, also known as Unicode codepoints. Many
languages have strings in UTF-8 or UTF-16, which will require conversion to codepoints to properly
implement FlexVer. Doing this is important to ensure equivalent behavior between languages.

Supplementary plane Unicode codepoints are generally rare in version numbers, but it is nonetheless
important to specify this behavior. Built-in language string comparisons often ignore Unicode and
simply compare the literal code unit values.

Despite usage of Unicode codepoints, for less surprising behavior and ease of implementation in
languages without a full Unicode database, only the ASCII digits (`0123456789`) are considered to be
*digits* in FlexVer (rather than the full Unicode definition), and that is how the term *digit* is
used in the rest of this document.

## Decomposition
The core of a FlexVer comparison is the *decomposition* of the input string into a series of
*components*. A component is a run of codepoints that are either all digits, or all are not digits.
Additionally, for compatibility with SemVer, ASCII hyphen-minus `-` and ASCII plus `+` are considered
"separator" characters, that will also split components themselves.

Decompositions in this document will be represented with spaces separating all components, like so:

`1 . 0 . 1 _ 01 a -pre 1 +exp 2`

Given this fundamental decomposition, the components can be split into four types, based on their
contents:

1. **Textual** - the run is entirely *non-digit* codepoints, and does not contain a *separator* (unless of length 1)
2. **Numeric** - the run is entirely *digit* codepoints
3. **Pre-release** - the run is entirely *non-digit* codepoints, its first codepoint is ASCII hyphen-minus (`-`), **and it is longer than one codepoint**
4. **Appendix** - the run is entirely *non-digit* codepoints, and its first codepoint is ASCII plus (`+`)

Appendices are a special case. If an appendix component is encountered, that component and all of
those following it are disregarded for comparison. This is one of the two SemVer compatibility special
cases in FlexVer.

Given this information, we can annotate the above decomposition with its component types,
represented as `t`, `n`, `p`, and `a`:

<code>n<b>1</b> t<b>.</b> n<b>0</b> t<b>.</b> n<b>1</b> t<b>_</b> n<b>01</b> t<b>a</b> p<b>-pre</b> n<b>1</b> a<b>+exp</b> n<b>2</b></code>

Appendices are discarded, leaving us with:

<code>n<b>1</b> t<b>.</b> n<b>0</b> t<b>.</b> n<b>1</b> t<b>_</b> n<b>01</b> t<b>a</b> p<b>-pre</b> n<b>1</b></code>

> **Note**
> This annotated form will not be used again, and is presented here for illustration. The type of a
> component is trivial to determine once the rules are known.

## Comparison
When comparing two versions, an additional "null" component is introduced if the versions are of
differing length. The shorter version is padded with nulls at the end, until it matches the length
of the longer version. For instance, when comparing `1.0` and `1.0.1`, the following decompositions
are those that are final, where `/` is a null component:

`1 . 0 / /`

`1 . 0 . 1`

Given the typed decomposition, the components can be compared as described below. If two components
at the same index are of differing types and are both not null, then they are compared as if they
are both Textual components based on their original text.

### Textual
For each codepoint in each of the two components (componentA and componentB), their numeric Unicode
values are compared, starting from the first codepoint. The iteration continues until the shortest
component ends. If any codepoint differs, that difference is the result. If all codepoints are
equal, then the result is the difference between the length of the components. In psuedocode:

```raku
for i from 0 to min(componentA.length, componentB.length) exclusive:
	let a = componentA[i]
	let b = componentB[i]
	if (a != b) return a <=> b
return componentA.length <=> componentB.length
```

### Numeric
This may either be implemented by parsing the component as an integer and comparing the integers,
or as a codepoint-wise comparison following the same rules an integer parser would. Either is
considered reasonable — the advantage of the codepoint-wise version being there is no limit to
the length of a component, while the advantage of the integer-parse version being it is easier to
implement. Both are described below.

#### Integer parse
Parse the component as an integer type, and return the difference between them. In psuedocode:

```raku
int(componentA) <=> int(componentB)
```

You should prefer the largest fast integer type available in your language. This is usually a 64-bit
integer. Parsing the string as an arbitrary-precision integer is wasteful for only doing a
comparison.

> **Warning**
> Care must be taken to keep the original text around in the event of a fallback to textual
> comparison, as mentioned above for when two components at the same index differ in type. If this
> is not done, and instead a number is converted back into a string for such a comparison, *it will
> be incorrect in the case of components with leading zeroes*.

#### Codepoint-wise
Remove zeroes from the beginnings of both components until doing so would leave them as length
zero. If their lengths differ, that difference is the result. Otherwise, iterate through each
codepoint, and compare their digit values. If those differ, that difference is the result. If all
the codepoints' digit values match, the result is equal. In psuedocode:

```raku
componentA = removeLeadingZeroes(componentA)
componentB = removeLeadingZeroes(componentB)
if (componentA.length != componentB.length) return componentA.length <=> componentB.length
for i from 0 to componentA.length exclusive:
	let a = digit(componentA[i])
	let b = digit(componentB[i])
	if (a != b) return a <=> b
return 0
```

As digits may only be ASCII, `digit` can be trivially implemented by subtracting the ASCII value of
`0` (0x30, 48) from the codepoints. Many languages offer a utility method for this already.
`removeLeadingZeroes` can be implemented similar to the following psuedocode:

```raku
if (component.length == 1) return component
let i = 0
while (i < a.length - 1 && digit(a[i]) == 0):
	i++
return component.slice(i, component.length)
```

### Pre-release
Pre-release components are compared identically to textual components, *except when being compared
to null*. See the Null section for more.

### Appendix
Appendix components must be removed before comparison is done.

### Null
Null components always compare as less than other components, *except pre-release components*,
compared to which, *null is greater*. This implements the SemVer rule that `1.0-pre1` is *less* than
`1.0`. Two null components are of course equal, but a comparison of two nulls should never occur.

## Sample Decompositions

* b1.7.3 - `b 1 . 7 . 3`
* b1.2.6 - `b 1 . 2 . 6`
* a1.1.2 - `a 1 . 1 . 2`
* 1.16.5-0.00.5 - `1 . 16 . 5 - 0 . 00 . 5`
* 1.0.0 - `1 . 0 . 0`
* 1.0.1 - `1 . 0 . 1`
* 1.0.0_01 - `1 . 0 . 0 _ 01`
* 0.17.1-beta.1 - `0 . 17 . 1 -beta. 1`
* 1.4.5_01 - `1 . 4 . 5 _ 01`
* 14w16a - `14 w 16 a`
* 1.4.5_01+exp-1.17 - `1 . 4 . 5 _ 01 +exp- 1 . 17`
* 13w02a - `13 w 02 a`
* 0.6.0-1.18.x - `0 . 6 . 0 - 1 . 18 .x`
* 1.0 - `1 . 0`
* a-a - `a -a`

## Sample Comparisons

Please see the [test vectors](https://github.com/unascribed/FlexVer/blob/trunk/test/test_vectors.txt)
used by the reference implementations.
