# flexver

A Crystal implementation of [FlexVer](https://github.com/unascribed/FlexVer).

## Installation

Due to the structure of [Shards dependencies](https://github.com/crystal-lang/shards/blob/master/docs/shard.yml.adoc#dependency-attributes)
and the FlexVer repository, it's currently impossible to natively use `flexver` as a GitHub dependency in a `shard.yml`.
However, you may instead use submodules:

1. Add the repository as a submodule:
    ```sh
    git submodule add https://github.com/unascribed/FlexVer flexver
    ```
2. Use a local path for `shards.yml`:
   ```yaml
   dependencies:
     flexver:
      path: ./flexver/crystal
   ```
3. Run `shards install`.

In the event that you need to update, run `git submodule update flexver`.

## Usage

```crystal
require "flexver"

FlexVer.new "b1.7.3" > FlexVer.new "a1.2.6" # => true
FlexVer.new "0.17.1-beta.1" > FlexVer.new "0.17.1-beta.2" # => false
# ...
```

## Contributors

- [Jill "oatmealine" Monoids](https://github.com/your-github-user) - creator and maintainer
