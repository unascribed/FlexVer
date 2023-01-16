# FlexVer Python

A Python implementation of FlexVer

## Getting It

FlexVer can be obtained from PyPI using `pip` under the name `flexver`.
Alternatively, copy [flexver.py](./flexver/flexver.py) wholesale into your project.

## Usage

This implementation provides the `FlexVer` class, which implements total ordering.

```python
FlexVer("1.0.0") < FlexVer("1.0.1")
```
