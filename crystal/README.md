# flexver

A Crystal implementation of [FlexVer](https://github.com/unascribed/FlexVer).

## Installation

1. Update your `shards.yml`:
   ```yaml
   dependencies:
     flexver:
       github: unascribed/FlexVer
       version: 0.1.0
   ```
2. Run `shards install`.

## Usage

```crystal
require "flexver/crystal/src/flexver.cr"

FlexVer.new "b1.7.3" > FlexVer.new "a1.2.6" # => true
FlexVer.new "0.17.1-beta.1" > FlexVer.new "0.17.1-beta.2" # => false
# ...
```

## Contributors

- [Jill "oatmealine" Monoids](https://github.com/oatmealine) - creator and maintainer
