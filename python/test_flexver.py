# python: 3.11
import typing
import unittest
import flexver


class FlexVerTestCase(unittest.TestCase):
	...


def createTest( index: int, line: str ) -> typing.Callable[[unittest.TestCase], None]:
	match line.split( ' ' ):
		case [ first, '>', second ]:
			return lambda self: self.assertGreater( flexver.compare( first, second ), 0, f'In comparison `{line}`' )
		case [ first, '=', second ]:
			return lambda self: self.assertEqual( flexver.compare( first, second ), 0, f'In comparison `{line}`' )
		case [ first, '<', second ]:
			return lambda self: self.assertLess( flexver.compare( first, second ), 0, f'In comparison `{line}`' )
		case [ _, op, _ ]:
			raise RuntimeError( f'Unrecognized comparison type `{op}` at line {index}' )


def loadTests( path: str ):
	with open( path, 'r' ) as file:
		for index, line in enumerate( file ):  # type: int, str
			if (pound := line.find('#')) != -1:
				line = line[: pound ]

			if line.endswith( '\n' ):
				line = line[: -1]

			if not line or line[0] == '#':
				continue

			setattr( FlexVerTestCase, f'test_{index}', createTest( index, line ) )


loadTests( '../test/test_vectors.txt' )
loadTests( '../test/large.txt' )


if __name__ == '__main__':
	unittest.main()
