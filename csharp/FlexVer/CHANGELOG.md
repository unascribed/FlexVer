# 1.1.2

- Fixed incorrect handling of leading zeroes when compared to a single-zero component. (e.g. '00' considered != '0')

# 1.1.1

- Correctly use Unicode codepoints for character comparison, instead of comparing each UTF-16 character fragment.

# 1.1.0

- Performance enhancement: Make the comparator non-allocating for version strings < 32 chars and ~4x faster
