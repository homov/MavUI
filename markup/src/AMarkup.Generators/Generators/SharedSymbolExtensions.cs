using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

internal static class SharedSymbolExtensions
{
	public static string GetFullyQualifiedTypeIncludingGlobal(this ITypeSymbol type)
	{
		return type.GetFullyQualifiedType(includeGlobalNamespace: true);
	}

	public static string GetFullyQualifiedTypeExcludingGlobal(this ITypeSymbol type)
	{
		return type.GetFullyQualifiedType(includeGlobalNamespace: false);
	}

	private static string GetFullyQualifiedType(this ITypeSymbol type, bool includeGlobalNamespace)
	{
		var builder = new StringBuilder();
		var visitor = new TypeSymbolDisplayVisitor(builder, includeGlobalNamespace);
		type.Accept(visitor);
		return builder.ToString();
	}

	public static bool IsReferenceTypeOrNullableValueType(this ITypeSymbol type)
	{
		if (!type.IsReferenceType)
		{
			return type.IsNullableValueType();
		}
		return true;
	}

	public static bool IsNullableValueType(this ITypeSymbol type)
	{
		if (type.IsValueType)
		{
			return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
		}
		return false;
	}

	public static bool IsNullableValueType(this ITypeSymbol type, out ITypeSymbol? underlyingType)
	{
		if (type.IsNullableValueType())
		{
			underlyingType = ((INamedTypeSymbol)type).TypeArguments[0];
			return true;
		}
		underlyingType = null;
		return false;
	}

	public static bool IsInstantiable(this ITypeSymbol type)
	{
		if (!type.IsAbstract && type is INamedTypeSymbol { InstanceConstructors: var instanceConstructors } namedTypeSymbol && instanceConstructors.Any((IMethodSymbol c) => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.IsEmpty))
		{
			return !ForceNotInstantiable(namedTypeSymbol);
		}
		return false;
	}

	private static bool ForceNotInstantiable(INamedTypeSymbol namedTypeSymbol)
	{
		string name = namedTypeSymbol.Name;
		if (name == "Control" || name == "ElementStub")
		{
			return true;
		}
		return false;
	}

	public static bool IsTypeOrDerivesFromType(this ITypeSymbol? symbol, ITypeSymbol compareSymbol)
	{
		if (symbol == null)
		{
			return false;
		}
		if (symbol.AllInterfaces.Any((INamedTypeSymbol x) => x.Equals(compareSymbol, SymbolEqualityComparer.Default)))
		{
			return true;
		}
		while (symbol != null)
		{
			if (symbol.Equals(compareSymbol, SymbolEqualityComparer.Default))
			{
				return true;
			}
			symbol = symbol.BaseType;
		}
		return false;
	}

	public static bool IsControlTemplate(this INamedTypeSymbol? propertyType, out bool isControlTemplate)
	{
		isControlTemplate = propertyType is not null
			&& (propertyType.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.Controls.Templates.IControlTemplate"
				|| propertyType.AllInterfaces.Any(@interface =>
					@interface.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.Controls.Templates.IControlTemplate"));

		return isControlTemplate;
	}

	public static bool IsControlTemplate(this INamedTypeSymbol? propertyType)
	{
		bool isControlTemplate;
		return propertyType.IsControlTemplate(out isControlTemplate);
	}

	public static bool IsDataTemplate(this INamedTypeSymbol? propertyType)
	{
		return propertyType is not null
			&& (propertyType.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.Controls.Templates.IDataTemplate"
				|| propertyType.AllInterfaces.Any(@interface =>
					@interface.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.Controls.Templates.IDataTemplate"));
	}

	public static bool IsStyledElement(this ITypeSymbol symbol)
	{
		for (ITypeSymbol? typeSymbol = symbol; typeSymbol is not null; typeSymbol = typeSymbol.BaseType)
		{
			if (typeSymbol.Name == "StyledElement"
				&& typeSymbol.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.StyledElement")
			{
				return true;
			}
		}

		return false;
	}

	public static bool IsAvaloniaObject(this ITypeSymbol symbol)
	{
		if (symbol.AllInterfaces.Any((INamedTypeSymbol x) => x.Name == "AvaloniaObject" && x.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.AvaloniaObject"))
		{
			return true;
		}
		for (ITypeSymbol typeSymbol = symbol; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
		{
			if (typeSymbol.Name == "AvaloniaObject" && typeSymbol.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.AvaloniaObject")
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsResourceDictionary(this ITypeSymbol symbol)
	{
		for (ITypeSymbol typeSymbol = symbol; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
		{
			if (typeSymbol.Name == "ResourceDictionary" && typeSymbol.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.Controls.ResourceDictionary")
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsControl(this ITypeSymbol symbol)
	{
		for (ITypeSymbol typeSymbol = symbol; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
		{
			if (typeSymbol.Name == "Control" && typeSymbol.GetFullyQualifiedTypeExcludingGlobal() == "Avalonia.Controls.Control")
			{
				return true;
			}
		}
		return false;
	}
}
