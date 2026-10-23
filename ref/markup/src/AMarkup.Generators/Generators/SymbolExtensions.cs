using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

public static class SymbolExtensions
{
	public static IEnumerable<AttributeData> GetAllAttributes(this ISymbol? symbol)
	{
		while (symbol != null)
		{
			foreach (AttributeData attribute in symbol.GetAttributes())
			{
				yield return attribute;
			}
			symbol = (symbol as INamedTypeSymbol)?.BaseType;
		}
	}

	public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol sym)
	{
		foreach (INamedTypeSymbol typeMember in sym.GetTypeMembers())
		{
			yield return typeMember;
		}
		foreach (INamespaceSymbol namespaceMember in sym.GetNamespaceMembers())
		{
			foreach (INamedTypeSymbol namespaceType in namespaceMember.GetNamespaceTypes())
			{
				yield return namespaceType;
			}
		}
	}

	public static IEnumerable<INamedTypeSymbol> GetPublicClasses(this INamespaceSymbol sym)
	{
		foreach (INamedTypeSymbol typeMember in sym.GetTypeMembers())
		{
			if (typeMember.DeclaredAccessibility == Accessibility.Public && typeMember.TypeKind == TypeKind.Class)
			{
				yield return typeMember;
			}
		}
		foreach (INamespaceSymbol namespaceMember in sym.GetNamespaceMembers())
		{
			foreach (INamedTypeSymbol publicClass in namespaceMember.GetPublicClasses())
			{
				if (publicClass.DeclaredAccessibility == Accessibility.Public && publicClass.TypeKind == TypeKind.Class)
				{
					yield return publicClass;
				}
			}
		}
	}

	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
	{
		foreach (T item in collection)
		{
			action(item);
		}
		return collection;
	}

	public static bool HasPublicSetter(this IPropertySymbol propertySymbol)
	{
		IMethodSymbol setMethod = propertySymbol.SetMethod;
		if (setMethod != null && setMethod.DeclaredAccessibility == Accessibility.Public)
		{
			return !setMethod.IsInitOnly;
		}
		return false;
	}

	public static bool IsAccessibleFromGeneratedMarkup(this ISymbol symbol)
	{
		switch (symbol.DeclaredAccessibility)
		{
		case Accessibility.Public:
			return true;
		case Accessibility.Internal:
		case Accessibility.ProtectedOrInternal:
			return !symbol.DeclaringSyntaxReferences.IsEmpty;
		default:
			return false;
		}
	}

	public static bool IsVisibleFromGeneratedMarkupScope(this INamedTypeSymbol typeSymbol)
	{
		for (INamedTypeSymbol namedTypeSymbol = typeSymbol; namedTypeSymbol != null; namedTypeSymbol = namedTypeSymbol.ContainingType)
		{
			if (!namedTypeSymbol.IsAccessibleFromGeneratedMarkup())
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsAssignableFromGeneratedMarkup(this IPropertySymbol propertySymbol)
	{
		IMethodSymbol setMethod = propertySymbol.SetMethod;
		if (setMethod != null && !setMethod.IsInitOnly)
		{
			return setMethod.IsAccessibleFromGeneratedMarkup();
		}
		return false;
	}

	public static bool IsCollectionWithAddMethod(this IPropertySymbol propertySymbol)
	{
		if (propertySymbol.IsCollection())
		{
			return HasAddMethod(propertySymbol.Type);
		}
		return false;
	}

	public static bool IsCollection(this IPropertySymbol propertySymbol)
	{
		return propertySymbol.Type.AllInterfaces.Any((INamedTypeSymbol x) => x.Name == "IEnumerable" || x.Name == "ICollection" || x.Name == "IList");
	}

	private static bool HasAddMethod(ITypeSymbol symbol)
	{
		for (ITypeSymbol typeSymbol = symbol; typeSymbol != null; typeSymbol = typeSymbol.BaseType)
		{
			if (typeSymbol.GetMembers("Add").Any((ISymbol x) => x is IMethodSymbol methodSymbol && methodSymbol.Parameters.Length == 1))
			{
				return true;
			}
		}
		if (symbol.TypeKind == TypeKind.Interface)
		{
			foreach (INamedTypeSymbol allInterface in symbol.AllInterfaces)
			{
				if (allInterface.GetMembers("Add").Any((ISymbol x) => x is IMethodSymbol methodSymbol && methodSymbol.Parameters.Length == 1))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsContentProperty(this IPropertySymbol propertySymbol)
	{
		AttributeData attributeData = propertySymbol.ContainingType.GetAllAttributes().FirstOrDefault((AttributeData x) => x.AttributeClass?.Name == "ContentPropertyAttribute");
		if (attributeData == null)
		{
			return false;
		}
		if (attributeData.NamedArguments.Length == 1 && attributeData.NamedArguments[0].Value.Value is string text)
		{
			return text == propertySymbol.Name;
		}
		return false;
	}

	public static ImmutableArray<ISymbol> GetMembersForGeneration(this INamedTypeSymbol namedType)
	{
		ImmutableArray<ISymbol> members = namedType.GetMembers();
		HashSet<string> propertyNames = new HashSet<string>(
			members.OfType<IPropertySymbol>().Select(static property => property.Name),
			StringComparer.Ordinal);

		INamedTypeSymbol? baseType = namedType.BaseType;
		if (baseType is not null && baseType.IsGenericType && baseType.IsAvaloniaObject())
		{
			ProcessGenericType(baseType);
		}

		return members;

		void ProcessGenericType(INamedTypeSymbol baseNamedType)
		{
			// Keep the most-derived property when a generic base type hides another
			// property with the same name. Otherwise both overloads would be emitted.
			IEnumerable<ISymbol> second = from member in baseNamedType.GetMembers()
				where !member.IsAbstract
					&& (member is not IPropertySymbol property || propertyNames.Add(property.Name))
				select member;
			members = members.Union(second, SymbolEqualityComparer.Default).ToImmutableArray();
			INamedTypeSymbol? baseType2 = baseNamedType.BaseType;
			if (baseType2 is not null && baseType2.IsGenericType)
			{
				ProcessGenericType(baseType2);
			}
		}
	}

}
