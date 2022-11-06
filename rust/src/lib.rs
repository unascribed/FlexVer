/*
 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 *
 * See <http://creativecommons.org/publicdomain/zero/1.0/>
 */

#![feature(pattern)]

use std::cmp::{
    Ordering,
    Ordering::{Equal, Greater, Less},
};
use std::collections::VecDeque;
use std::str::pattern::Pattern;

#[derive(Debug)]
enum SortingType {
    Numerical(i64),
    Lexical(String),
    SemverPrerelease(String),
}

impl SortingType {
    fn to_string(self) -> String {
        match self {
            Self::Numerical(a) => a.to_string(),
            Self::Lexical(a) | Self::SemverPrerelease(a) => a,
        }
    }
}

fn split_once_rest<'a, P>(s: &'a str, pat: P) -> Option<(&str, &str)>
where
    P: Pattern<'a>,
{
    let loc = s.find(pat);
    if let Some(index) = loc {
        Some(s.split_at(index))
    } else {
        Some((s, &""))
    }
}

fn is_semver_prerelease(s: &str) -> bool {
    s.len() > 1 && s.starts_with('-')
}

fn decompose(str_in: &str) -> VecDeque<SortingType> {
    if str_in.is_empty() {
        return VecDeque::new();
    }

    let mut last_numeric = str_in.starts_with(|c: char| c.is_ascii_digit());
    let mut s = str_in.to_owned();
    let mut out: VecDeque<SortingType> = VecDeque::new();

    if let Some((left, _)) = s.split_once('+') {
        s = left.to_owned();
    };

    while !s.is_empty() {
        if last_numeric {
            if let Some((left, right)) = split_once_rest(&s, |c: char| !c.is_ascii_digit()) {
                out.push_back(SortingType::Numerical(left.parse::<i64>().unwrap()));
                s = right.to_owned();
                last_numeric = false;
            }
        } else {
            if let Some((left, right)) = split_once_rest(&s, |c: char| c.is_ascii_digit()) {
                out.push_back(if is_semver_prerelease(left) {
                    SortingType::SemverPrerelease(left.to_string())
                } else {
                    SortingType::Lexical(left.to_string())
                });
                s = right.to_owned();
                last_numeric = true;
            }
        }
    }

    out
}

#[derive(Debug)]
struct VersionComparisonIterator {
    left: VecDeque<SortingType>,
    right: VecDeque<SortingType>,
}

impl Iterator for VersionComparisonIterator {
    type Item = (Option<SortingType>, Option<SortingType>);

    fn next(&mut self) -> Option<Self::Item> {
        let item = (self.left.pop_front(), self.right.pop_front());
        if let (None, None) = item {
            None
        } else {
            Some(item)
        }
    }
}

pub fn compare(left: &str, right: &str) -> Ordering {
    let mut iter = VersionComparisonIterator {
        left: decompose(left),
        right: decompose(right),
    };

    while let Some(next) = iter.next() {
        use SortingType::*;

        let current = match next {
            (Some(l), None) => {
                if let SemverPrerelease(_) = l {
                    Less
                } else {
                    Greater
                }
            }
            (None, Some(r)) => {
                if let SemverPrerelease(_) = r {
                    Greater
                } else {
                    Less
                }
            }
            (Some(l), Some(r)) => match (l, r) {
                (Numerical(l), Numerical(r)) => l.cmp(&r),
                (l, r) => l.to_string().cmp(&r.to_string()),
            },
            (None, None) => unreachable!(),
        };
        if current != Equal {
            return current;
        }
    }
    return Equal;
}

#[derive(Debug, Copy, Clone)]
pub struct FlexVer<'a>(pub &'a str);

impl PartialEq for FlexVer<'_> {
    fn eq(&self, other: &Self) -> bool {
        compare(&self.0, &other.0) == Equal
    }
}

impl Eq for FlexVer<'_> {}

impl PartialOrd for FlexVer<'_> {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(compare(&self.0, &other.0))
    }
}

impl Ord for FlexVer<'_> {
    fn cmp(&self, other: &Self) -> Ordering {
        compare(&self.0, &other.0)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn test(left: &str, right: &str, result: Ordering) {
        assert_eq!(compare(&left, &right), result);
        assert_eq!(
            compare(&right, &left),
            match result {
                Less => Greater,
                Greater => Less,
                Equal => Equal,
            }
        );
    }

    #[test]
    fn test_compare() {
        test("b1.7.3", "a1.2.6", Greater);
        test("b1.2.6", "a1.7.3", Greater);
        test("a1.1.2", "a1.1.2_01", Less);
        test("1.16.5-0.00.5", "1.14.2-1.3.7", Greater);
        test("1.0.0", "1.0.0-2", Less);
        test("1.0.0", "1.0.0_01", Less);
        test("1.0.1", "1.0.0_01", Greater);
        test("1.0.0_01", "1.0.1", Less);
        test("0.17.1-beta.1", "0.17.1", Less);
        test("0.17.1-beta.1", "0.17.1-beta.2", Less);
        test("1.4.5_01", "1.4.5_01+fabric-1.17", Equal);
        test("1.4.5_01", "1.4.5_01+fabric-1.17+ohno", Equal);
        test("14w16a", "18w40b", Less);
        test("18w40a", "18w40b", Less);
        test("1.4.5_01+fabric-1.17", "18w40b", Less);
        test("13w02a", "c0.3.0_01", Less);
        test("0.6.0-1.18.x", "0.9.beta-1.18.x", Less);
    }

    #[test]
    fn test_ord() {
        assert!(FlexVer("b1.7.3") > FlexVer("a1.2.6"));
        assert!(FlexVer("b1.2.6") > FlexVer("a1.7.3"));
        assert!(FlexVer("a1.1.2") < FlexVer("a1.1.2_01"));
        assert!(FlexVer("1.16.5-0.00.5") > FlexVer("1.14.2-1.3.7"));
        assert!(FlexVer("1.0.0") < FlexVer("1.0.0-2"));
        assert!(FlexVer("1.0.0") < FlexVer("1.0.0_01"));
        assert!(FlexVer("1.0.1") > FlexVer("1.0.0_01"));
        assert!(FlexVer("1.0.0_01") < FlexVer("1.0.1"));
        assert!(FlexVer("0.17.1-beta.1") < FlexVer("0.17.1"));
        assert!(FlexVer("0.17.1-beta.1") < FlexVer("0.17.1-beta.2"));
        assert!(FlexVer("1.4.5_01") == FlexVer("1.4.5_01+fabric-1.17"));
        assert!(FlexVer("1.4.5_01") == FlexVer("1.4.5_01+fabric-1.17+ohno"));
        assert!(FlexVer("14w16a") < FlexVer("18w40b"));
        assert!(FlexVer("18w40a") < FlexVer("18w40b"));
        assert!(FlexVer("1.4.5_01+fabric-1.17") < FlexVer("18w40b"));
        assert!(FlexVer("13w02a") < FlexVer("c0.3.0_01"));
        assert!(FlexVer("0.6.0-1.18.x") < FlexVer("0.9.beta-1.18.x"));

        assert_eq!(FlexVer("b1.7.3"), FlexVer("b1.7.3").max(FlexVer("a1.2.6")));
        assert_eq!(FlexVer("b1.2.6"), FlexVer("b1.2.6").max(FlexVer("a1.7.3")));
        assert_eq!(FlexVer("a1.2.6"), FlexVer("b1.7.3").min(FlexVer("a1.2.6")));
        assert_eq!(FlexVer("a1.7.3"), FlexVer("b1.2.6").min(FlexVer("a1.7.3")));
        assert_eq!(FlexVer("1.0.0"), FlexVer("1.0.0").max(FlexVer("1.0.0")));
        assert_eq!(FlexVer("1.0.0"), FlexVer("1.0.0").min(FlexVer("1.0.0")));
        assert_eq!(
            FlexVer("1.1.0"),
            FlexVer("1.1.0").clamp(FlexVer("1.0.0"), FlexVer("1.2.0"))
        );
    }
}
