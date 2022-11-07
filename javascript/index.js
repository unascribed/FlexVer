function(a, b) {
	var comparators = {
		"null": function(a, b) {
			return b._type == "null" ? 0 : -compare(b, a);
		},
		"text": function(a, b) {
			if (b._type == "null") return 1;

			for (var i = 0; i < Math.min(a.length, b.length); i++) {
				var c1 = a[i];
				var c2 = b[i];
				if (c1 != c2) return c1 - c2;
			}

			return a.length - b.length;
		},
		"prerelease": function(a, b) {
			if (b._type == "null") return -1; // opposite order
			return comparators.text(a, b);
		},
		"numeric": function(a, b) {
			function digit(c) {
				return c-48;
			}

			function removeLeadingZeroes(c) {
				if (c.length == 1) return c;
				var i = 0;
				while (i < c.length && digit(c[i]) == 0) {
					i++;
				}
				return c.slice(i);
			}

			if (b._type == "null") return 1;
			if (b._type == "numeric") {
				a = removeLeadingZeroes(a);
				b = removeLeadingZeroes(b);
				if (a.length != b.length) return a.length - b.length;
				for (var i = 0; i < a.length; i++) {
					var ad = digit(a[i]);
					var bd = digit(b[i]);
					if (ad != bd) return ad-bd;
				}
				return 0;
			}
			return comparators.text(a, b);
		}
	};

	function compare(a, b) {
		return comparators[a._type](a, b);
	}

	function decompose(str) {
		function isAsciiDigit(c) {
			return c >= 48 && c <= 57;
		}
		function isLowSurrogate(c) {
			return c >= 0xDC00 && c <= 0xDFFF;
		}
		function isHighSurrogate(c) {
			return c >= 0xD800 && c <= 0xDBFF;
		}

		function createComponent(number, s) {
			s = s.slice();
			if (number) {
				s._type = "numeric";
			} else if (s.length > 1 && s[0] == 45) {
				s._type = "prerelease";
			} else {
				s._type = "text";
			}
			return s;
		}

		if (str.length == 0) return [];
		var lastWasNumber = isAsciiDigit(str.codePointAt(0));
		var accum = [];
		var out = [];
		for (var i = 0; i < str.length; i++) {
			if (i > 0 && isHighSurrogate(str.charAt(i-1)) && isLowSurrogate(str.charAt(i))) continue;
			var cp = str.codePointAt(i);
			if (cp == 43) break; // remove appendices
			var number = isAsciiDigit(cp);
			if (number != lastWasNumber) {
				out.push(createComponent(lastWasNumber, accum));
				accum = [];
				lastWasNumber = number;
			}
			accum.push(cp);
		}
		out.push(createComponent(lastWasNumber, accum));
		return out;
	}

	function get(a, i) {
		return i >= a.length ? {_type: "null"} : a[i];
	}

	var ad = decompose(a);
	var bd = decompose(b);

	for (var i = 0; i < Math.max(ad.length, bd.length); i++) {
		var c = compare(get(ad, i), get(bd, i));
		if (c != 0) return c;
	}
	return 0;
}
