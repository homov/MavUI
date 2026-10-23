using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

internal record struct MethodParameterInfo(
	string ParameterTypeFullyQualified,
	bool IsReferenceTypeOrNullableValueType)
{
	public static MethodParameterInfo From(ITypeSymbol type) =>
		new(type.GetFullyQualifiedTypeIncludingGlobal(), type.IsReferenceTypeOrNullableValueType());
}
