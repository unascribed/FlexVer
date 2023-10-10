# FlexVer Python
A Python implementation of FlexVer

## Installation
FlexVer is hosted on PyPI under the `flexver` name, so it can be installed using any compliant package manager such as `pip`.
Alternatively, you can directly copy [flexver.py](./flexver.py) into your project.

## Usage
This implementation provides the `FlexVer` class, which implements total ordering; and a `compare(str, str): int` function, for more direct comparisons.

```python
FlexVer("1.0.0") < FlexVer("1.0.1")
# or
compare("1.0.0", "1.0.1") == -1
```