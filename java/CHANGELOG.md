
# 1.0.1
- Now fully Unicode-aware, removing edge-cases involving UTF-16 surrogates and bringing full parity with the new Rust impl
- Numeric components can now be arbitrarily long, no 64-bit limit (other implementations may not share this feature)
- Non-ASCII digits are no longer considered numerical as it's surprising behavior and difficult to implement in other languages (including Rust)

# 1.0
- Initial release
