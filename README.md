<img src="logo.svg" width="180px" align="right"/>

# FlexVer
FlexVer is a SemVer-compatible intuitive comparator for free-form versioning strings as seen in the
wild. It's designed to sort versions like people do, rather than attempting to force conformance to
a rigid and limited standard. This repo collects meta information and discussion on FlexVer, as well
as implementations of it in various languages. Notably, it houses a [specification](SPEC.md).

It works by splitting a string into "numeric" and "non-numeric" parts, and then sorting those
lexically, with a few special cases for SemVer compatibility. This mode of operation is similar to
how `sort -V` behaves in GNU coreutils. As such, when used solely for simple comparisons and not
group-aware things (such as ~ matches), it is also a stand-in for a SemVer parser (with one
exception — read on)

The initial Java implementation contains a test harness used to initially verify the logic, seen
below:

![image](https://user-images.githubusercontent.com/6185037/200154644-b94b61bf-e430-4dbd-bd2e-caddab86c9f2.png)

Cyan is numeric, pink is non-numeric, red is SemVer pre-releases, and gray is ignored SemVer-style
appendices. As the potential problem space of version number comparisons is infinite, I cannot
devise a good way to truly verify this is a completely sound implementation; so, I simply
cherry-picked some random examples of versions that I felt would trip up a version comparator, which
both do and do not follow SemVer. As such, there are no unit tests; I feel a system like this only
benefits from regression tests, after basic sanity tests done during development.

It is a non-goal for comparisons between versions of entirely different structures to "make sense" —
but nonetheless, it does try its best and will normalize structural differences and fall back to
lexical comparison when all else fails. Fundamentally, ***FlexVer does not impose any form of
restrictions*** — it will always do its best to compare two strings and *never* throw an exception.
If you do pass two completely different versions to it, well....

![image](https://user-images.githubusercontent.com/6185037/200155199-a80a03cf-9820-4075-9763-efff800e2507.png)

...the results won't necessarily make sense. Garbage in, garbage out.

There is **one known case where FlexVer and SemVer disagree**. It is related to pre-releases, as
they are an exception to the intuitive version flow. There is a special case that makes the most
common forms of them work, such as `-pre1`, `-rc2`, `-beta.2`, etc. However, this works by looking
for a non-numeric component that starts with `-` and is longer than one character. As such, if you
have a pre-release starting with a numeral, then FlexVer's normal parsing kicks in, which sorts
`1.0.0` and `1.0.0-2` *in the opposite order that SemVer would*. However, I have never seen fully
numeric pre-releases like this in the wild; a hyphenated numeric like that is generally meant to
represent another "group" of versions — in which case, FlexVer does indeed sort it as expected.
