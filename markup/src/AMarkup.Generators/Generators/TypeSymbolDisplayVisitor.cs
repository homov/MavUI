using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AMarkup.Generators;

public sealed class TypeSymbolDisplayVisitor : SymbolVisitor
{
	private readonly StringBuilder _builder;

	private readonly bool _includeGlobalNamespace;

	public TypeSymbolDisplayVisitor(StringBuilder builder, bool includeGlobalNamespace)
	{
		_builder = builder;
		_includeGlobalNamespace = includeGlobalNamespace;
	}

	private void AppendName(string text)
	{
		if (SyntaxFacts.GetKeywordKind(text) != SyntaxKind.None)
		{
			_builder.Append('@');
		}
		_builder.Append(text);
	}

	public override void VisitArrayType(IArrayTypeSymbol symbol)
	{
		Visit(symbol.ElementType);
		_builder.Append("[]");
	}

	public override void VisitDynamicType(IDynamicTypeSymbol symbol)
	{
		_builder.Append("dynamic");
	}

	public override void VisitNamedType(INamedTypeSymbol symbol)
	{
		string specialTypeName = GetSpecialTypeName(symbol);
		if (specialTypeName != null)
		{
			_builder.Append(specialTypeName);
			return;
		}
		if (symbol.IsGenericType && symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			Visit(symbol.TypeArguments[0]);
			_builder.Append('?');
			return;
		}
		Visit(symbol.ContainingSymbol);
		if (symbol.ContainingSymbol is INamespaceSymbol { IsGlobalNamespace: not false })
		{
			if (_includeGlobalNamespace)
			{
				_builder.Append("global::");
			}
		}
		else
		{
			_builder.Append('.');
		}
		AppendName(symbol.Name);
		if (!symbol.IsGenericType)
		{
			return;
		}
		_builder.Append('<');
		for (int i = 0; i < symbol.TypeArguments.Length; i++)
		{
			Visit(symbol.TypeArguments[i]);
			if (i < symbol.TypeArguments.Length - 1)
			{
				_builder.Append(", ");
			}
		}
		_builder.Append('>');
	}

	private static string? GetSpecialTypeName(INamedTypeSymbol symbol)
	{
		return symbol.SpecialType switch
		{
			SpecialType.System_SByte => "sbyte", 
			SpecialType.System_Int16 => "short", 
			SpecialType.System_Int32 => "int", 
			SpecialType.System_Int64 => "long", 
			SpecialType.System_Byte => "byte", 
			SpecialType.System_UInt16 => "ushort", 
			SpecialType.System_UInt32 => "uint", 
			SpecialType.System_UInt64 => "ulong", 
			SpecialType.System_Single => "float", 
			SpecialType.System_Double => "double", 
			SpecialType.System_Decimal => "decimal", 
			SpecialType.System_Char => "char", 
			SpecialType.System_Boolean => "bool", 
			SpecialType.System_String => "string", 
			SpecialType.System_Object => "object", 
			_ => null, 
		};
	}

	public override void VisitNamespace(INamespaceSymbol symbol)
	{
		Visit(symbol.ContainingSymbol);
		if (symbol.IsGlobalNamespace)
		{
			return;
		}
		INamespaceSymbol containingNamespace = symbol.ContainingNamespace;
		if (containingNamespace != null && containingNamespace.IsGlobalNamespace)
		{
			if (_includeGlobalNamespace)
			{
				_builder.Append("global::");
			}
		}
		else
		{
			_builder.Append('.');
		}
		AppendName(symbol.Name);
	}

	public override void VisitTypeParameter(ITypeParameterSymbol symbol)
	{
		AppendName(symbol.Name);
	}
}
