# flexver-rs

A Rust implementation of FlexVer.

## Getting It

You can either copy (and rename) [lib.rs](src/lib.rs) wholesale into your project, or retrieve it from [crates.io](https://crates.io/crates/flexver-rs) like so in `Cargo.toml`:

```toml
[dependencies]
flexver-rs = "0.1.2"
```

## Usage

The crate provides both a `compare` function and the `FlexVer` struct. The `FlexVer` struct implements `Ord`, and thus supports all of the comparison operations.

```rust
fn compare(left: &str, right: &str) -> std::cmp::Ordering; // Type signature

assert_eq!(compare("1.0.0", "1.1.0"), Ordering::Less);

```

```rust
struct FlexVer(&str); // Type signature

assert!(FlexVer("1.0.0") < FlexVer("1.1.0"));
```

You can find additional examples in the tests section at the bottom of [lib.rs](src/lib.rs).
