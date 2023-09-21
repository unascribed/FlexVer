import unittest
import flexver

opTable = { '>': -1, '=': 0, '<': 1 }


class MyTestCase( unittest.TestCase ):
	...


def loadTests( path: str ):
	with open( path, 'r' ) as file:
		for index, line in enumerate( file ):  # type: int, str
			if (pound := line.find('#')) != -1:
				line = line[: pound ]
			if not line or line == '\n' or line[0] == '#':
				continue

			if line.endswith( '\n' ):
				line = line[: -1]

			parts = line.split(' ')

			def test( self ) -> None:
				self.assertEqual(opTable[parts[1]], flexver.FlexVerComparator.compare(parts[0], parts[2]), line)

			setattr( MyTestCase, f'test_{index}', test )


loadTests( '../test/test_vectors.txt' )
loadTests( '../test/large.txt' )


if __name__ == '__main__':
	unittest.main()
