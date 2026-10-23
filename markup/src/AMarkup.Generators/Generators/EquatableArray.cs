using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AMarkup.Generators;

internal static class EquatableArray
{
	public static EquatableArray<T> AsEquatableArray<T>(this ImmutableArray<T> array) where T : IEquatable<T>
	{
		return new EquatableArray<T>(array);
	}
}
internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>, IEnumerable where T : IEquatable<T>
{
	private readonly T[]? array = Unsafe.As<ImmutableArray<T>, T[]>(ref array);

	public ref readonly T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ref AsImmutableArray().ItemRef(index);
		}
	}

	public bool IsEmpty
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return AsImmutableArray().IsEmpty;
		}
	}

	public bool Equals(EquatableArray<T> array)
	{
		return AsSpan().SequenceEqual(array.AsSpan());
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is EquatableArray<T> equatableArray)
		{
			return object.Equals(this, equatableArray);
		}
		return false;
	}

	public override int GetHashCode()
	{
		T[] array = this.array;
		if (array == null)
		{
			return 0;
		}
		HashCode hashCode = default;
		T[] array2 = array;
		foreach (T value in array2)
		{
			hashCode.Add(value);
		}
		return hashCode.ToHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ImmutableArray<T> AsImmutableArray()
	{
		return Unsafe.As<T[], ImmutableArray<T>>(ref Unsafe.AsRef(in array));
	}

	public static EquatableArray<T> FromImmutableArray(ImmutableArray<T> array)
	{
		return new EquatableArray<T>(array);
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return AsImmutableArray().AsSpan();
	}

	public T[] ToArray()
	{
		return AsImmutableArray().ToArray();
	}

	public ImmutableArray<T>.Enumerator GetEnumerator()
	{
		return AsImmutableArray().GetEnumerator();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)AsImmutableArray()).GetEnumerator();
	}

	public static implicit operator EquatableArray<T>(ImmutableArray<T> array)
	{
		return FromImmutableArray(array);
	}

	public static implicit operator ImmutableArray<T>(EquatableArray<T> array)
	{
		return array.AsImmutableArray();
	}

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
	{
		return !left.Equals(right);
	}
}
