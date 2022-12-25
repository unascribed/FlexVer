///
module flexver;

import std.ascii: isDigit;
import std.uni: byCodePoint;

///
int compare(in string first, in string second) pure @safe
{
	auto decomposedFirst = decompose(first);
	auto decomposedSecond = decompose(second);

	int componentIndex;
	foreach (firstComponent; decomposedFirst)
	{
		if (decomposedSecond.length > componentIndex)
		{
			immutable Component secondComponent = decomposedSecond[componentIndex];
			immutable diff = firstComponent.opCmp(secondComponent);
			if (diff != 0)
			{
				return diff;
			}
		}
		else
		{
			return firstComponent.compareEmptyComponent;
		}

		++componentIndex;
	}

	if (decomposedSecond.length > componentIndex)
	{
		return -decomposedSecond[componentIndex].compareEmptyComponent;
	}

	return 0;
}

private Component[] decompose(in string rhs) pure @safe //may throw due to autodecoding
out (r; isValid(r))
{
	Component[] result;
	Component current;

	if (rhs[0].isDigit)
	{
		current.type = ComponentType.Numeric;
	}
	else
	{
		if (rhs[0] == '-')
		{
			current.type = ComponentType.PreRelease;
		}
		else
		{
			current.type = ComponentType.Textual;
		}
	}

	foreach (codePoint; rhs.byCodePoint)
	{
		if (codePoint == '+')
		{
			result ~= current;
			return result;
		}

		final switch (current.type)
		{
		case ComponentType.Textual:
			if (!(codePoint.isDigit))
			{
				current.s ~= codePoint;
			}
			else
			{
				result ~= current;
				current = Component.init;
				current.type = ComponentType.Numeric;
				current.s ~= codePoint;
			}
			break;

		case ComponentType.Numeric:
			if (codePoint.isDigit)
			{
				current.s ~= codePoint;
			}
			else
			{
				result ~= current;

				current = Component.init;
				if (codePoint == '-')
				{
					current.type = ComponentType.PreRelease;
				}
				else
				{
					current.type = ComponentType.Textual;
				}
				current.s ~= codePoint;
			}
			break;

		case ComponentType.PreRelease:
			if (!(codePoint.isDigit))
			{
				current.s ~= codePoint;
			}
			else
			{
				if (current.s.length <= 1)
				{
					current.type = ComponentType.Textual;
				}
				result ~= current;
				current = Component.init;
				current.type = ComponentType.Numeric;
				current.s ~= codePoint;
			}
			break;

		case ComponentType.None:
			assert(false);
		}
	}

	result ~= current;
	return result;
}

private bool isValid(in Component[] list) pure @safe
{
	ComponentType lastType;
	foreach (componentIndex, Component c; list)
	{
		if (!c.isValid)
		{
			return false;
		}

		immutable temp = c.type;
		if (temp != ComponentType.Textual && temp != ComponentType.Numeric &&
				temp != ComponentType.PreRelease)
		{
			return false;
		}

		if (componentIndex == 0)
		{
			continue;
		}

		if (temp == lastType)
		{
			return false;
		}
		lastType = temp;
	}

	return true;
}

private bool isValid(Component component) pure @safe
{
	if (component.s.isAppendix)
	{
		return false;
	}

	final switch (component.type)
	{
	case ComponentType.Textual:
		break;

	case ComponentType.Numeric:
		foreach (codePoint; component.s.byCodePoint)
		{
			if (!codePoint.isDigit)
			{
				return false;
			}
		}
		break;

	case ComponentType.PreRelease:
		if (!component.s.isPreRelease)
		{
			return false;
		}
		break;

	case ComponentType.None:
		return false;
	}

	return true;
}

version (unittest)
{
	private alias CT_T = ComponentType.Textual;
	private alias CT_N = ComponentType.Numeric;
	private alias CT_P = ComponentType.PreRelease;

	private auto componentList(Args...)(Args args) pure @safe //may throw in out contract

	if ((args.length & 1) == 0)
	out (r; isValid(r))
	{
		Component[] result;

		foreach (i, arg; args)
		{
			static if (is(typeof(arg) == ComponentType)) //caution: can accept int literals
			{
				Component component;
				component.type = arg;
				result ~= component;
			}
			else static if (is(typeof(arg) == string))
			{
				result[i / 2].s = arg;
			}
		}
		return result;
	}
}

pure @safe unittest
{
	//dfmt off
	assert(decompose("b1.7.3") == componentList(CT_T, "b", CT_N, "1", CT_T, ".", CT_N, "7", CT_T, ".", CT_N, "3"));
	assert(decompose("b1.2.6") == componentList(CT_T, "b", CT_N, "1", CT_T, ".", CT_N, "2", CT_T, ".", CT_N, "6"));
	assert(decompose("a1.1.2") == componentList(CT_T, "a", CT_N, "1", CT_T, ".", CT_N, "1", CT_T, ".", CT_N, "2"));
	assert(decompose("1.16.5-0.00.5") == componentList(CT_N, "1", CT_T, ".", CT_N, "16", CT_T, ".", CT_N, "5", CT_T, "-", CT_N, "0", CT_T, ".", CT_N, "00", CT_T, ".", CT_N, "5"));
	assert(decompose("1.0.0") == componentList(CT_N, "1", CT_T, ".", CT_N, "0", CT_T, ".", CT_N, "0"));
	assert(decompose("1.0.1") == componentList(CT_N, "1", CT_T, ".", CT_N, "0", CT_T, ".", CT_N, "1"));
	assert(decompose("1.0.0_01") == componentList(CT_N, "1", CT_T, ".", CT_N, "0", CT_T, ".", CT_N, "0", CT_T, "_", CT_N, "01"));
	assert(decompose("0.17.1-beta.1") == componentList(CT_N, "0", CT_T, ".", CT_N, "17", CT_T, ".", CT_N, "1", CT_P, "-beta.", CT_N, "1"));
	assert(decompose("1.4.5_01") == componentList(CT_N, "1", CT_T, ".", CT_N, "4", CT_T, ".", CT_N, "5", CT_T, "_", CT_N, "01"));
	assert(decompose("14w16a") == componentList(CT_N, "14", CT_T, "w", CT_N, "16", CT_T, "a"));
	assert(decompose("1.4.5_01+exp-1.17") == componentList(CT_N, "1", CT_T, ".", CT_N, "4", CT_T, ".", CT_N, "5", CT_T, "_", CT_N, "01"));
	assert(decompose("13w02a") == componentList(CT_N, "13", CT_T, "w", CT_N, "02", CT_T, "a"));
	assert(decompose("0.6.0-1.18.x") == componentList(CT_N, "0", CT_T, ".", CT_N, "6", CT_T, ".", CT_N, "0", CT_T, "-", CT_N, "1", CT_T, ".", CT_N, "18", CT_T, ".x"));
	assert(decompose("1.0") == componentList(CT_N, "1", CT_T, ".", CT_N, "0"));
	//dfmt on
}

private struct Component
{
	ComponentType type;
	string s;

	invariant
	{
		assert(this.isValid);
	}

	int opCmp(in Component rhs) const pure @safe
	{
		final switch (type)
		{
		case ComponentType.Textual:
			return compareText(rhs);

		case ComponentType.Numeric:
			if (rhs.type == ComponentType.Numeric)
			{
				return compareNumber(rhs);
			}
			return compareText(rhs);

		case ComponentType.PreRelease:
			if (rhs.type == ComponentType.PreRelease)
			{
				return compareText(rhs);
			}
			return 1;

		case ComponentType.None:
			assert(false);
		}

		assert(false);
	}

	int compareText(in Component rhs) const pure @safe
	{
		import std.algorithm.comparison: cmp;

		return cmp(s.byCodePoint, rhs.s.byCodePoint);
	}

	pure @safe unittest
	{
		immutable foo = Component(ComponentType.Textual, "the quick brown fox jumped over the lazy dog");
		immutable bar = Component(ComponentType.Textual, "the quick brown fox");
		assert(foo.compareText(bar) > 0);
	}

	string stripNumeric() const pure @safe
	in (type == ComponentType.Numeric)
	out (r; r.length > 0)
	out (r; r.length <= s.length)
	{
		import std.algorithm.mutation: stripLeft;

		auto result = s.stripLeft('0');
		if (result.length == 0)
		{
			return "0";
		}
		return result;
	}

	pure @safe unittest
	{
		immutable foo = Component(ComponentType.Numeric, "000");
		assert(foo.stripNumeric == "0");
		immutable bar = Component(ComponentType.Numeric, "008");
		assert(bar.stripNumeric == "8");
	}

	int compareNumber(in Component rhs) const pure @safe
	in (type == ComponentType.Numeric)
	in (rhs.type == ComponentType.Numeric)
	{
		immutable diff = stripNumeric.length - rhs.stripNumeric.length;
		if (diff != 0)
		{
			return cast(int) diff;
		}

		return Component(ComponentType.Textual, stripNumeric).compareText(
				Component(ComponentType.Textual, rhs.stripNumeric));
	}

	pure @safe unittest
	{
		immutable foo = Component(ComponentType.Numeric, "420");
		immutable bar = Component(ComponentType.Numeric, "69");

		assert(foo.compareNumber(bar) > 0);
	}

	int compareEmptyComponent() const @nogc nothrow pure @safe
	{
		if (type == ComponentType.PreRelease)
		{
			return -1;
		}

		return 1;
	}
}

private bool isAppendix(in string s) @nogc nothrow pure @safe
{
	return s[0] == '+';
}

private bool isPreRelease(in string s) @nogc nothrow pure @safe
{
	return (s[0] == '-' && s.length > 1);
}

private enum ComponentType
{
	None, //uninitialized
	Textual,
	Numeric,
	PreRelease
}

version (unittest)
{
	private void test(in string first, in string second, in int expected) pure @safe
	{
		import std.math: sgn;

		immutable result = sgn(compare(first, second));

		assert(result == expected, "incorrect ordering: " ~ first ~ ordering(result) ~ second);
		assert(isComparisonCommutative(&compare, first, second),
				"comparison " ~ first ~ " <=> " ~ second ~ " must be commutative");
	}

	private string ordering(in int value) @nogc nothrow pure @safe
	{
		if (value < 0)
		{
			return " < ";
		}

		if (value > 0)
		{
			return " > ";
		}

		return " = ";
	}

	private bool isComparisonCommutative(T)(int function(T, T) pure @safe f, in T a, in T b) pure @safe
	{
		return (f(a, b) == -f(b, a));
	}
}

pure @safe unittest
{
	//copied wholesale from the java implementation
	test("b1.7.3", "a1.2.6", 1);
	test("b1.2.6", "a1.7.3", 1);
	test("a1.1.2", "a1.1.2_01", -1);
	test("1.16.5-0.00.5", "1.14.2-1.3.7", 1);
	test("1.0.0", "1.0.0_01", -1);
	test("1.0.1", "1.0.0_01", 1);
	test("1.0.0_01", "1.0.1", -1);
	test("0.17.1-beta.1", "0.17.1", -1);
	test("0.17.1-beta.1", "0.17.1-beta.2", -1);
	test("1.4.5_01", "1.4.5_01+fabric-1.17", 0);
	test("1.4.5_01", "1.4.5_01+fabric-1.17+ohgod", 0);
	test("14w16a", "18w40b", -1);
	test("18w40a", "18w40b", -1);
	test("1.4.5_01+fabric-1.17", "18w40b", -1);
	test("13w02a", "c0.3.0_01", -1);
	test("0.6.0-1.18.x", "0.9.beta-1.18.x", -1);
	// 2^65. Too large for a 64-bit integer or a double
	test("36893488147419103232", "36893488147419103233", -1);
	test("37", "12", 1);
	test("12", "13", -1);
	test("12", "21", -1);
	test("43", "103", -1);
	test("1.0", "1.1", -1);
}
