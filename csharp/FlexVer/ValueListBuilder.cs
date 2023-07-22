using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FlexVer;

internal ref struct ValueListBuilder<T>
{
	private Span<T> _span;
	private int _pos;

	public static ValueListBuilder<T> Empty()
		=> new ValueListBuilder<T>(Span<T>.Empty);

	public ValueListBuilder(Span<T> initialSpan)
	{
		_span = initialSpan;
		_pos = 0;
	}

	public int Length
	{
		get => _pos;
		set
		{
			Debug.Assert(value >= 0);
			Debug.Assert(value <= _span.Length);
			_pos = value;
		}
	}

	public int Capacity
	{
		get => _span.Length;
	}

	public ref T this[int index]
	{
		get
		{
			Debug.Assert(index < _pos);
			return ref _span[index];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(T item)
	{
		int pos = _pos;
		if (pos >= _span.Length)
			Grow();

		_span[pos] = item;
		_pos = pos + 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AppendRange(T[] items)
	{
		int pos = _pos;
		while (pos + items.Length >= _span.Length) {
			Grow();
		}


		foreach (T item in items)
		{
			_span[pos] = item;
			_pos = pos + 1;
			pos++;
		}
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return _span[.._pos];
	}

	public void Grow()
	{
		T[] array = new T[_span.Length * 2];

		bool success = _span.TryCopyTo(array);
		Debug.Assert(success);

		_span = array;
	}
}
