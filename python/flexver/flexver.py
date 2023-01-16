"""FlexVer Python
A parser for the FlexVer (<https://github.com/unascribed/FlexVer>) version standard

LICENSE:
To the extent possible under law, the author has dedicated all copyright
and related and neighboring rights to this software to the public domain
worldwide. This software is distributed without any warranty.

See <http://creativecommons.org/publicdomain/zero/1.0/>
"""

from dataclasses import dataclass
from functools import total_ordering
from itertools import zip_longest
from typing import Union, Optional, TypeVar


@dataclass
class _Numerical:
    number: int
    string: str

    def __init__(self, str_in: str):
        self.number = int(str_in)
        self.string = str_in


@dataclass
class _Lexical:
    string: str


@dataclass
class _SemverPrerelease:
    string: str


_AllSortingTypes = Union[_Numerical, _Lexical, _SemverPrerelease]


def _is_ascii_digit(s: str) -> bool:
    return s.isdigit() and s.isascii()


def _decompose(s: str) -> list[_AllSortingTypes]:
    if not s:
        return []

    currently_numeric: bool = _is_ascii_digit(s[0])
    out: list[_AllSortingTypes] = []

    if "+" in s:
        s = s.split("+")[0]

    last_index = 0
    for (i, c) in enumerate(s):
        if currently_numeric:
            if not _is_ascii_digit(c):
                currently_numeric = False
                out.append(_Numerical(s[last_index:i]))
                last_index = i
            continue

        if i == 0 or not _is_ascii_digit(c) and c != "-":
            continue

        if _is_ascii_digit(c):
            currently_numeric = True

        if s[last_index] == "-" and i > (last_index + 1):
            out.append(_SemverPrerelease(s[last_index:i]))
        else:
            out.append(_Lexical(s[last_index:i]))

        last_index = i

    if currently_numeric:
        out.append(_Numerical(s[last_index:]))
    elif s[last_index] == "-" and last_index < len(s) - 1:
        out.append(_SemverPrerelease(s[last_index:]))
    else:
        out.append(_Lexical(s[last_index:]))

    return out


_LESS = -1
_EQUAL = 0
_GREATER = 1


def _cmp(left: Optional[_AllSortingTypes], right: Optional[_AllSortingTypes]):

    A = TypeVar("A", int, str)

    def _inner_cmp(left: A, right: A) -> int:
        if left == right:
            return _EQUAL
        elif left < right:
            return _LESS
        else:  # left > right
            return _GREATER

    if isinstance(right, _SemverPrerelease):
        if left is None:
            return _GREATER
        else:
            return _LESS

    elif isinstance(left, _SemverPrerelease):
        if right is None:
            return _LESS
        else:
            return _GREATER

    elif isinstance(left, _Numerical) and isinstance(right, _Numerical):
        return _inner_cmp(left.number, right.number)

    elif left is None:
        return _LESS

    elif right is None:
        return _GREATER

    elif left is not None and right is not None:
        return _inner_cmp(left.string, right.string)

    else:  # Theoretically this should never happen ¯\_(ツ)_/¯
        raise NotImplementedError


def _compare(left: list[_AllSortingTypes], right: list[_AllSortingTypes]) -> int:
    zipped = zip_longest(left, right)

    for l, r in zipped:
        if (cmp := _cmp(l, r)) != _EQUAL:
            return cmp

    return _EQUAL


@total_ordering
class FlexVer:
    _string: str
    _decomposition: list[_AllSortingTypes]

    def __init__(self, inp: str):
        self._string = inp
        self._decomposition = _decompose(inp)

    def __str__(self) -> str:
        return self._string

    def __repr__(self) -> str:
        return f"FlexVer('{self}')"

    def __eq__(self, other: "FlexVer") -> bool:
        return _compare(self._decomposition, other._decomposition) == _EQUAL

    def __lt__(self, other: "FlexVer") -> bool:
        return _compare(self._decomposition, other._decomposition) == _LESS

    def __gt__(self, other: "FlexVer") -> bool:
        return _compare(self._decomposition, other._decomposition) == _GREATER
