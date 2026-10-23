using System;
using System.Collections.Generic;
using System.Linq;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public static class SymbolExtensions
{
	public static string GetQualifiedTypeName(this ITypeSymbol typeSymbol)
	{
		string typeName = typeSymbol.GetTypeName();
		if (!typeName.Contains(".") || !(typeSymbol is INamedTypeSymbol namedTypeSymbol))
		{
			return typeName;
		}
		string text = namedTypeSymbol.ContainingNamespace.ToString();
		string name = namedTypeSymbol.Name;
		if (typeSymbol.ContainingType != null)
		{
			return typeSymbol.ContainingType.GetQualifiedTypeName() + "." + typeSymbol.Name;
		}
		string text2 = text + "." + name;
		if (namedTypeSymbol.IsGenericType)
		{
			IEnumerable<string> values = namedTypeSymbol.TypeArguments.Select((ITypeSymbol x) => x.GetQualifiedTypeName());
			string text3 = text2;
			string text4 = string.Empty;
			if (text3.EndsWith("?", StringComparison.InvariantCulture))
			{
				text3 = text3.Substring(0, text3.Length - 1);
				text4 = "?";
			}
			text2 = text3 + "<" + string.Join(", ", values) + ">" + text4;
		}
		if (text2.Contains(".") && !text2.StartsWith("global::", StringComparison.InvariantCulture))
		{
			text2 = "global::" + text2;
		}
		return text2;
	}

	public static bool IsNullable(this ITypeSymbol type)
	{
		if (type.NullableAnnotation == NullableAnnotation.Annotated)
		{
			return true;
		}
		INamedTypeSymbol obj = type as INamedTypeSymbol;
		if (obj != null && obj.IsGenericType)
		{
			return type.OriginalDefinition.ToDisplayString().Equals("System.Nullable<T>", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static bool IsNullable(this ITypeSymbol type, out ITypeSymbol? nullableType)
	{
		if (type.IsNullable())
		{
			nullableType = ((type.NullableAnnotation == NullableAnnotation.Annotated) ? type : ((INamedTypeSymbol)type).TypeArguments.First());
			return true;
		}
		nullableType = null;
		return false;
	}
}
