using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

internal static class ISymbolExtensions
{
	private static readonly Dictionary<string, string> _fullNamesMaping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{
			"string",
			typeof(string).ToString()
		},
		{
			"long",
			typeof(long).ToString()
		},
		{
			"int",
			typeof(int).ToString()
		},
		{
			"short",
			typeof(short).ToString()
		},
		{
			"ulong",
			typeof(ulong).ToString()
		},
		{
			"uint",
			typeof(uint).ToString()
		},
		{
			"ushort",
			typeof(ushort).ToString()
		},
		{
			"byte",
			typeof(byte).ToString()
		},
		{
			"double",
			typeof(double).ToString()
		},
		{
			"float",
			typeof(float).ToString()
		},
		{
			"decimal",
			typeof(decimal).ToString()
		},
		{
			"bool",
			typeof(bool).ToString()
		}
	};

	public static string GetGloballyQualifiedTypeName(this INamespaceOrTypeSymbol symbol)
	{
		string value = symbol.GetFullMetadataName();
		if (_fullNamesMaping.Any((KeyValuePair<string, string> x) => x.Value == value))
		{
			return _fullNamesMaping.First((KeyValuePair<string, string> x) => x.Value == value).Key;
		}
		if (_fullNamesMaping.Any((KeyValuePair<string, string> x) => x.Value == "System.Nullable`1[" + value + "]"))
		{
			return _fullNamesMaping.First((KeyValuePair<string, string> x) => x.Value == "System.Nullable`1[" + value + "]").Key + "?";
		}
		return value;
	}

	public static string GetFullMetadataName(this INamespaceOrTypeSymbol symbol)
	{
		ISymbol symbol2 = symbol;
		StringBuilder stringBuilder = new StringBuilder(symbol2.MetadataName);
		ISymbol symbol3 = symbol2;
		symbol2 = symbol2.ContainingSymbol;
		if (symbol2 == null)
		{
			return symbol.GetFullName();
		}
		while (!IsRootNamespace(symbol2))
		{
			if (symbol2 is ITypeSymbol && symbol3 is ITypeSymbol)
			{
				stringBuilder.Insert(0, '+');
			}
			else
			{
				stringBuilder.Insert(0, '.');
			}
			stringBuilder.Insert(0, symbol2.MetadataName);
			symbol2 = symbol2.ContainingSymbol;
		}
		if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Any())
		{
			string text = string.Join(",", namedTypeSymbol.TypeArguments.Select(GetFullMetadataName));
			stringBuilder.Append("[" + text + "]");
		}
		return stringBuilder.ToString();
	}

	private static bool IsRootNamespace(ISymbol s)
	{
		if (s is INamespaceSymbol namespaceSymbol)
		{
			return namespaceSymbol.IsGlobalNamespace;
		}
		return false;
	}

	public static string GetFullName(this INamespaceOrTypeSymbol type)
	{
		if (type is IArrayTypeSymbol arrayTypeSymbol)
		{
			return arrayTypeSymbol.ElementType.GetFullName() + "[]";
		}
		if (((ITypeSymbol)type).IsNullable(out ITypeSymbol nullableType) && nullableType != null)
		{
			return "System.Nullable`1[" + nullableType.GetFullName() + "]";
		}
		string text = type.ToDisplayString() ?? string.Empty;
		if (!_fullNamesMaping.TryGetValue(text, out string value))
		{
			return text;
		}
		return value;
	}
}
