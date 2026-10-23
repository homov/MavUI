using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

internal sealed class ImmutableArrayComparer : IEqualityComparer<ImmutableArray<IAssemblySymbol>>
{
	public static IEqualityComparer<ImmutableArray<IAssemblySymbol>> Instance { get; } = new ImmutableArrayComparer();

	private ImmutableArrayComparer()
	{
	}

	public bool Equals(ImmutableArray<IAssemblySymbol> x, ImmutableArray<IAssemblySymbol> y)
	{
		if (x.Length != y.Length)
		{
			return false;
		}
		for (int i = 0; i < x.Length; i++)
		{
			if (!x[i].Equals(y[i], SymbolEqualityComparer.Default))
			{
				return false;
			}
		}
		return true;
	}

	public int GetHashCode(ImmutableArray<IAssemblySymbol> obj)
	{
		HashCode hashCode = default;
		foreach (IAssemblySymbol item in obj)
		{
			hashCode.Add(item);
		}
		return hashCode.ToHashCode();
	}
}
