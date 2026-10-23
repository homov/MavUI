using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

internal record struct GenerationTypeInfo(string TypeFullyQualifiedName, string TypeName, string TypeContainingNamespace, Accessibility DeclaredAccessibility, bool IsSealed)
{
	public static GenerationTypeInfo From(INamedTypeSymbol typeSymbol)
	{
		return new GenerationTypeInfo(typeSymbol.GetFullyQualifiedTypeIncludingGlobal(), typeSymbol.Name, typeSymbol.ContainingNamespace.ToDisplayString(), typeSymbol.DeclaredAccessibility, typeSymbol.IsSealed);
	}
}
