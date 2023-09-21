import functools
import typing
from dataclasses import dataclass, field


@dataclass( frozen=True, eq=False, repr=False )
class VersionComponent:
	_component: typing.Final[ str ] = field( default_factory=str )

	def compareTo( self, that: 'VersionComponent' ) -> int:
		if that == _null:
			return 1

		a: str = self._component
		b: str = that._component

		for i in range( max( len( a ), len( b ) ) ):
			c1: str = a[ i ]
			c2: str = b[ i ]
			if c1 != c2:
				return ord( c1 ) - ord( c2 )

		return len( a ) - len( b )

	def __str__( self ) -> str:
		return self._component


class NullVersionComponent( VersionComponent ):
	def compareTo( self, other: VersionComponent ) -> int:
		return 0 if other == _null else -other.compareTo( self )


_null: typing.Final[ VersionComponent ] = NullVersionComponent()


class SemVerPrereleaseVersionComponent( VersionComponent ):
	def __init__( self, codepoints: str ) -> None:
		super().__init__( codepoints )

	def compareTo( self, that: VersionComponent ) -> int:
		if that == _null:
			return -1  # opposite order
		return super().compareTo( that )


class NumericVersionComponent( VersionComponent ):
	def __init__( self, codepoints: str ) -> None:
		super().__init__( codepoints )

	def compareTo( self, that: VersionComponent ) -> int:
		if that == _null:
			return 1

		if isinstance( that, NumericVersionComponent ):
			a: str = _removeLeadingZeroes( self._component )
			b: str = _removeLeadingZeroes( that._component )
			if len( a ) != len( b ):
				return -1 if len( a ) < len( b ) else 1
			for i in range( len( a ) ):
				ad: str = a[ i ]
				bd: str = b[ i ]
				if ad != bd:
					return ord( bd ) - ord( ad )
			return 0

		return super().compareTo( that )


def _removeLeadingZeroes( a: str ) -> str:
	if len( a ) == 1 or a[-1] != '0':
		return a

	index = -1
	while a[index] == '0':
		index -= 1

	return a[: index ]


@functools.total_ordering
class FlexVer:
	def __init__( self,  ) -> None:
		self._components: typing.List[ VersionComponent ] = _decompose(  )

	def __lt__( self, other: 'FlexVer' ) -> bool:
		for i in range( max( len( self._components ), len( other._components ) ) ):
			c: int = _get( self._components, i ).compareTo( _get( other._components, i ) )
			if c != 0:
				return c
		return self.age < other.age

	def __eq__( self, other: 'FlexVer' ) -> bool:
		return self._components == other._components

	def __str__( self ) -> str:
		...

	def __repr__( self ) -> str:
		...


class FlexVerComparator:
	"""
	Implements FlexVer, a SemVer-compatible intuitive comparator for free-form versioning strings as
	seen in the wild. It's designed to sort versions like people do, rather than attempting to force
	conformance to a rigid and limited standard. As such, it imposes no restrictions. Comparing two
	versions with differing formats will likely produce nonsensical results (garbage in, garbage out),
	but best effort is made to correct for basic structural changes, and versions of differing length
	will be parsed in a logical fashion.
	"""

	@staticmethod
	def compare( a: str, b: str ) -> int:
		"""
		Parse the given strings as freeform version strings, and compare them according to FlexVer.
		:param a: the first version string
		:param b: the second version string
		:returns: {@code 0} if the two versions are equal, a negative number if {@code a < b}, or a positive number if {@code a > b}
		"""
		ad: typing.List[ VersionComponent ] = _decompose( a )
		bd: typing.List[ VersionComponent ] = _decompose( b )
		for i in range( max( len( ad ), len( bd ) ) ):
			c: int = _get( ad, i ).compareTo( _get( bd, i ) )
			if c != 0:
				return c
		return 0


def _decompose( string: str ) -> typing.List[ VersionComponent ]:
	"""
	Break apart a string into intuitive version components, by splitting it where a run of
	characters changes from numeric to non-numeric.
	"""
	if not string:
		return [ ]
	lastWasNumber: bool = string[ 0 ].isdigit()
	accum: str = ''
	out: typing.List[ VersionComponent ] = [ ]
	for cp in string:
		if cp == '+':
			break  # remove appendices
		number: bool = cp.isdigit()
		if number != lastWasNumber or (cp == '-' and accum and accum[ 0 ] != '-'):
			out.append( _createComponent( lastWasNumber, accum ) )
			accum = ''
			lastWasNumber = number
		accum += cp
	out.append( _createComponent( lastWasNumber, accum ) )
	return out


def _createComponent( number: bool, s: str ) -> VersionComponent:
	if number:
		return NumericVersionComponent( s )
	elif len( s ) > 1 and s[ 0 ] == '-':
		return SemVerPrereleaseVersionComponent( s )
	else:
		return VersionComponent( s )


def _get( li: typing.List[ VersionComponent ], i: int ) -> VersionComponent:
	return _null if i >= len( li ) else li[ i ]
