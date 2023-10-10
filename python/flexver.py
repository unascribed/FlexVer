"""FlexVer Python
A parser for the FlexVer (<https://github.com/unascribed/FlexVer>) version standard

LICENSED UNDER CC0 1.0:
	To the extent possible under law, the author has dedicated all copyright
	and related and neighboring rights to this software to the public domain
	worldwide. This software is distributed without any warranty.
	See <https://creativecommons.org/publicdomain/zero/1.0/>
"""

import functools
import typing
import dataclasses

__author__ = 'ENDERZOMBI102 <enderzombi102.end@gmail.com>'
__license__ = 'CC0-1.0'
__version__ = '1.1.1'
__all__ = [ 'FlexVer', 'compare' ]


@functools.total_ordering
class FlexVer:
	"""
	Implements FlexVer, a SemVer-compatible intuitive comparator for free-form versioning strings as
	seen in the wild. It's designed to sort versions like people do, rather than attempting to force
	conformance to a rigid and limited standard. As such, it imposes no restrictions.

	Comparing two versions with differing formats will likely produce nonsensical results (garbage in, garbage out),
	but best effort is made to correct for basic structural changes, and versions of differing length
	will be parsed in a logical fashion.
	"""
	__slots__ = [ '_components', '_original' ]
	_components: typing.Final[ typing.List['_VersionComponent'] ]
	_original: typing.Final[ str ]

	def __init__( self, version: str ) -> None:
		self._original = version
		self._components = _decompose( version )

	def compare( self, other: 'FlexVer' ) -> int:
		"""
		Compares this `FlexVer` object with another.
		\t
		:param other: The other `FlexVer` object.
		:returns: `0`:code: if the two are equal, a negative number if `self < other`:code:, or a positive number if `self > other`:code:.
		"""
		for i in range( max( len( self._components ), len( other._components ) ) ):
			c: int = _get( self._components, i ).compare_to( _get( other._components, i ) )
			if c != 0:
				return c
		return 0

	def __lt__( self, other: 'FlexVer' ) -> bool:
		return self.compare( other ) < 0

	def __eq__( self, other: 'FlexVer' ) -> bool:
		return self.compare( other ) == 0

	def __str__( self ) -> str:
		return self._original

	def __repr__( self ) -> str:
		return f"FlexVer( '{self}' )"


def compare( a: str, b: str ) -> int:
	"""
	Parse the given strings as freeform version strings, and compare them according to FlexVer.
	\t
	:param a: The first version string.
	:param b: The second version string.
	:returns: `0`:code: if the two versions are equal, a negative number if `a < b`:code:, or a positive number if `a > b`:code:.
	"""
	ad: typing.List[ _VersionComponent ] = _decompose(a)
	bd: typing.List[ _VersionComponent ] = _decompose(b)
	for i in range( max( len( ad ), len( bd ) ) ):
		c = _get( ad, i ).compare_to( _get( bd, i ) )
		if c != 0:
			return c
	return 0


@dataclasses.dataclass( frozen=True, eq=False, repr=False )
class _VersionComponent:
	_component: typing.Final[ str ] = ''

	def compare_to( self, that: '_VersionComponent' ) -> int:
		if that == _null:
			return 1

		a: str = self._component
		b: str = that._component

		for i in range( min( len( a ), len( b ) ) ):
			c1: str = a[ i ]
			c2: str = b[ i ]
			if c1 != c2:
				return ord( c1 ) - ord( c2 )

		return len( a ) - len( b )

	def __str__( self ) -> str:
		return self._component

	def __repr__( self ) -> str:
		return f't{self._component}'


class _NullVersionComponent(_VersionComponent):
	def compare_to( self, other: _VersionComponent ) -> int:
		return 0 if other is _null else -other.compare_to( self )

	def __str__( self ) -> str:
		return '/'

	def __repr__( self ) -> str:
		return '/'


_null: typing.Final[ _VersionComponent ] = _NullVersionComponent()


class _SemVerPrereleaseVersionComponent(_VersionComponent):
	def compare_to( self, that: _VersionComponent ) -> int:
		if that == _null:
			return -1  # opposite order
		return super().compare_to( that )

	def __repr__( self ) -> str:
		return f'p{self._component}'


class _NumericVersionComponent(_VersionComponent):
	def compare_to( self, that: _VersionComponent ) -> int:
		if that == _null:
			return 1

		if isinstance( that, _NumericVersionComponent ):
			a: str = self._component.lstrip( '0' )
			b: str = that._component.lstrip( '0' )

			if len( a ) != len( b ):
				return len( a ) - len( b )

			for i in range( len( a ) ):
				ad: str = a[ i ]
				bd: str = b[ i ]
				if ad != bd:
					return ord( ad ) - ord( bd )
			return 0

		return super().compare_to( that )

	def __repr__( self ) -> str:
		return f'n{self._component}'


def _decompose( string: str ) -> typing.List[_VersionComponent]:
	"""
	Break apart a string into intuitive version components, by splitting it where a run of
	characters changes from numeric to non-numeric.
	"""
	if not string:
		return [ ]

	def createComponent( number: bool, string: str ) -> _VersionComponent:
		if number:
			return _NumericVersionComponent( string )
		elif len( string ) > 1 and string[ 0 ] == '-':
			return _SemVerPrereleaseVersionComponent( string )
		return _VersionComponent( string )

	lastWasNumber: bool = 48 <= ord( string[0] ) <= 57
	accum: typing.List[ str ] = [ ]
	out: typing.List[ _VersionComponent ] = [ ]
	for cp in string:
		if cp == '+':
			break  # remove appendices
		number: bool = 48 <= ord( cp ) <= 57
		if number != lastWasNumber or (cp == '-' and accum and accum[ 0 ] != '-'):
			out.append( createComponent( lastWasNumber, ''.join( accum ) ) )
			accum = [ ]
			lastWasNumber = number
		accum.append( cp )

	out.append( createComponent( lastWasNumber, ''.join( accum ) ) )
	return out


def _get( li: typing.List[ _VersionComponent ], i: int ) -> _VersionComponent:
	"""
	When comparing two versions, an additional "null" component is introduced if the versions are of differing length.
	The shorter version is padded with nulls at the end, until it matches the length of the longer version.
	"""
	return _null if i >= len( li ) else li[ i ]
