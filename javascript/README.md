# FlexVer-JS

A basic JavaScript implementation of FlexVer. Does not use "modern" JavaScript syntax, and so should
work anywhere, as long as String.codePointAt is defined.

## Getting it

You can either copy and rename [index.js](index.js) wholesale into your project, or retrieve it from
NPM, where the package name is `flexver`.

## Usage

In the browser, a `flexVerCompare` function is added to the `window` object. It accepts two strings,
and returns a negative number for `a < b`, 0 for `a == b`, and a positive number for `a > b`.

In Node, a function is exported from the module that works the same as described above.
