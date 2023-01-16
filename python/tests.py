#! /usr/bin/env python3

from flexver import FlexVer, _decompose, _Lexical, _Numerical, _SemverPrerelease
from unittest import TestCase, main


class TestAll(TestCase):
    def test_decomposition(self):
        self.assertEqual(
            _decompose("b1.7.3"),
            [
                _Lexical("b"),
                _Numerical("1"),
                _Lexical("."),
                _Numerical("7"),
                _Lexical("."),
                _Numerical("3"),
            ],
        )

    def test_comparison(self):
        self.assertTrue(FlexVer("b1.7.3") > FlexVer("a1.2.6"))
        self.assertTrue(FlexVer("a1.1.2") < FlexVer("a1.1.2_01"))
        self.assertTrue(FlexVer("1.16.5-0.00.5") > FlexVer("1.14.2-1.3.7"))
        self.assertTrue(FlexVer("1.0.0") < FlexVer("1.0.0_01"))
        self.assertTrue(FlexVer("1.0.1") > FlexVer("1.0.0_01"))
        self.assertTrue(FlexVer("0.17.1-beta.1") < FlexVer("0.17.1"))
        self.assertTrue(FlexVer("0.17.1-beta.1") < FlexVer("0.17.1-beta.2"))
        self.assertTrue(FlexVer("1.4.5_01") == FlexVer("1.4.5_01+exp-1.17"))
        self.assertTrue(FlexVer("1.4.5_01") == FlexVer("1.4.5_01+exp-1.17-moretext"))
        self.assertTrue(FlexVer("14w16a") < FlexVer("18w40b"))
        self.assertTrue(FlexVer("18w40a") < FlexVer("18w40b"))
        self.assertTrue(FlexVer("1.4.5_01+exp-1.17") < FlexVer("18w40b"))
        self.assertTrue(FlexVer("13w02a") < FlexVer("c0.3.0_01"))
        self.assertTrue(FlexVer("1.0") < FlexVer("1.1"))
        self.assertTrue(FlexVer("1.0") < FlexVer("1.0.1"))
        self.assertTrue(FlexVer("10") > FlexVer("2"))
        self.assertTrue(FlexVer("a-a") < FlexVer("a"))


if __name__ == "__main__":
    main()
