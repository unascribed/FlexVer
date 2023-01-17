/*
 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 *
 * See <http://creativecommons.org/publicdomain/zero/1.0/>
 */

use std::cmp::Ordering::{self, Equal, Greater, Less};
use std::collections::VecDeque;

#[derive(Debug)]
enum SortingType {
    Numerical(i64, String),
    Lexical(String),
    SemverPrerelease(String),
}

impl SortingType {
    fn into_string(self) -> String {
        match self {
            Self::Numerical(_, a) | Self::Lexical(a) | Self::SemverPrerelease(a) => a,
        }
    }
}

fn is_semver_prerelease(s: &str) -> bool {
    s.len() > 1 && s.starts_with('-')
}

fn decompose(str_in: &str) -> VecDeque<SortingType> {
    if str_in.is_empty() {
        return VecDeque::new();
    }

    let s = if let Some((left, _)) = str_in.split_once('+') {
        left
    } else {
        str_in
    };

    let mut out: VecDeque<SortingType> = VecDeque::new();
    let mut current = String::new();

    let mut currently_numeric = s.starts_with(|c: char| c.is_ascii_digit());
    let mut skip = s.starts_with('-');

    fn handle_split(
        current: &str,
        c: Option<&char>,
        currently_numeric: bool,
    ) -> Option<SortingType> {
        let numeric = if let Some(c) = c {
            c.is_ascii_digit()
        } else {
            false
        };

        use SortingType::*;

        if currently_numeric {
            if numeric {
                return None;
            } else {
                return Some(Numerical(
                    current.parse::<i64>().unwrap(),
                    current.to_owned(),
                ));
            }
        }

        if !(numeric || c == Some(&'-') || c.is_none()) {
            return None;
        }

        if is_semver_prerelease(current) {
            if c == Some(&'-') {
                // Pre-releases can have multiple dashes
                None
            } else {
                Some(SemverPrerelease(current.to_owned()))
            }
        } else {
            Some(Lexical(current.to_owned()))
        }
    }

    for c in s.chars() {
        if let Some(part) = handle_split(&current, Some(&c), currently_numeric) {
            if skip {
                skip = false;
            } else {
                out.push_back(part);
                current.clear();
                currently_numeric = c.is_ascii_digit();
            }
        }
        current.push(c);
    }

    if let Some(part) = handle_split(&current, None, currently_numeric) {
        out.push_back(part);
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
    let iter = VersionComparisonIterator {
        left: decompose(left),
        right: decompose(right),
    };

    for next in iter {
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
                (Numerical(l, _), Numerical(r, _)) => l.cmp(&r),
                (l, r) => l.into_string().cmp(&r.into_string()),
            },
            (None, None) => unreachable!(),
        };

        if current != Equal {
            return current;
        }
    }

    Equal
}

#[derive(Debug, Copy, Clone)]
pub struct FlexVer<'a>(pub &'a str);

impl PartialEq for FlexVer<'_> {
    fn eq(&self, other: &Self) -> bool {
        compare(self.0, other.0) == Equal
    }
}

impl Eq for FlexVer<'_> {}

impl PartialOrd for FlexVer<'_> {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(compare(self.0, other.0))
    }
}

impl Ord for FlexVer<'_> {
    fn cmp(&self, other: &Self) -> Ordering {
        compare(self.0, other.0)
    }
}

#[cfg(test)]
mod tests {
    use std::path::PathBuf;
    use std::fs;

    use super::*;

    const ENABLED_TESTS: &'static [&str] = &[ "test_vectors.txt" ];

    fn test(left: &str, right: &str, expected: Ordering) -> Result<(), String> {
        if compare(left, right) != expected {
            return Err(format!("Expected {:?} but found {:?}", expected, compare(left, right)));
        }

        // Assert commutativity, if right > left than left < right and vice versa  
        let inverse = match expected {
            Less => Greater,
            Greater => Less,
            Equal => Equal,
        };
        if compare(right, left) != inverse {
            return Err(format!("Comparison method violates its general contract!"));
        }

        Ok(())
    }

    #[test]
    fn standardized_tests() {
        let test_folder = PathBuf::from("../test");
        let errors = ENABLED_TESTS.iter().flat_map(|test_file_name| {
            let test_file = test_folder.join(test_file_name);
            fs::read_to_string(test_file).unwrap()
                .lines()
                .enumerate()
                .filter(|(_, line)| !line.starts_with("#"))
                .filter(|(_, line)| !line.is_empty())
                .map(|(num, line)| {
                    let split: Vec<&str> = line.split(" ").collect();
                    if split.len() != 3 { panic!("{}:{} Line formatted incorrectly, expected 2 spaces: {}", test_file_name, num, line) }
                    let ord = match split[1] {
                        "<" => Less,
                        "=" => Equal,
                        ">" => Greater,
                        _ => panic!("{} is not a valid ordering", split[1])
                    };
                    test(split[0], split[2], ord).map_err(|message| (line.to_owned(), message))
                }).collect::<Vec<_>>()
            })
            .filter_map(|res| res.err())
            .collect::<Vec<_>>();
        
        if !errors.is_empty() {
            errors.iter().for_each(|(line, message)| println!("{}: {}", line, message));
            panic!()
        }
    }

    #[test]
    fn test_min() {
        assert_eq!(FlexVer("1.0.0"), FlexVer("1.0.0").min(FlexVer("1.0.0")));
        assert_eq!(FlexVer("a1.2.6"), FlexVer("b1.7.3").min(FlexVer("a1.2.6")));
        assert_eq!(FlexVer("a1.7.3"), FlexVer("b1.2.6").min(FlexVer("a1.7.3")));
    }

    #[test]
    fn test_max() {
        assert_eq!(FlexVer("b1.7.3"), FlexVer("b1.7.3").max(FlexVer("a1.2.6")));
        assert_eq!(FlexVer("b1.2.6"), FlexVer("b1.2.6").max(FlexVer("a1.7.3")));
        assert_eq!(FlexVer("1.0.0"), FlexVer("1.0.0").max(FlexVer("1.0.0")));
    }

    #[test]
    fn test_clamp() {
        assert_eq!(
            FlexVer("1.1.0"),
            FlexVer("1.1.0").clamp(FlexVer("1.0.0"), FlexVer("1.2.0"))
        );
    }
}
