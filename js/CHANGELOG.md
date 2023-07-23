# 1.1.1
- Fixed incorrect handling of leading zeroes when compared to a single-zero component. (e.g. '00' considered != '0')

# 1.1.0
- Fixed inconsistent handling of an ambiguity in the spec related to prereleases

# 1.0.2
- Provide separate browser, Node.js, and ES module artifacts
- I tried to do this with a module build system but they all suck so I just used Handlebars

# 1.0.1
- Throw an error if the module system can't be discovered instead of trying a weird trick that doesn't work

# 1.0.0
- Initial release
