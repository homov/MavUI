using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

internal record AvaloniaPropertyExtensionInfo(
	string PropertyTypeFullyQualified,
	string PropertyName,
	bool PropertyHasPublicSetter,
	bool PropertyIsCollection,
	bool PropertyTypeIsReferenceTypeOrNullableValueType,
	bool PropertyTypeIsInstantiable,
	bool IsTemplateType,
	Accessibility PropertyDeclaredAccessibility,
	bool IsAvaloniaProperty,
	EquatableArray<MethodParameterInfo> MethodParameterTypes,
	bool IsNotSealedAndIsShadowing,
	bool IsStyledElement,
	GenerationTypeInfo GenerationTypeInfo) : BaseModel(GenerationTypeInfo)
{
	public static AvaloniaPropertyExtensionInfo From(
		IPropertySymbol propertySymbol,
		GenerationTypeInfo typeInfo,
		bool isAvaloniaProperty,
		bool isNotSealedAndIsShadowing,
		bool isStyledElement)
	{
		var propertyType = propertySymbol.Type;
		bool isCollection = propertySymbol.IsCollectionWithAddMethod();
		bool isTemplate = (propertyType as INamedTypeSymbol).IsControlTemplate();

		return new AvaloniaPropertyExtensionInfo(
			propertyType.GetFullyQualifiedTypeIncludingGlobal(),
			propertySymbol.Name,
			propertySymbol.HasPublicSetter(),
			isCollection,
			propertyType.IsReferenceTypeOrNullableValueType(),
			propertyType.IsInstantiable(),
			isTemplate,
			propertySymbol.DeclaredAccessibility,
			isAvaloniaProperty,
			isCollection ? GetAddMethodParameterTypes(propertySymbol) : default,
			isNotSealedAndIsShadowing,
			isStyledElement,
			typeInfo);
	}

	private static EquatableArray<MethodParameterInfo> GetAddMethodParameterTypes(IPropertySymbol property)
	{
		ImmutableHashSet<MethodParameterInfo>.Builder builder = ImmutableHashSet.CreateBuilder<MethodParameterInfo>();

		for (ITypeSymbol? type = property.Type; type is not null; type = type.BaseType)
		{
			AddParameterTypes(type, builder);
		}

		if (property.Type.TypeKind == TypeKind.Interface)
		{
			foreach (INamedTypeSymbol @interface in property.Type.AllInterfaces)
			{
				AddParameterTypes(@interface, builder);
			}
		}

		return builder
			.OrderBy(static parameter => parameter.ParameterTypeFullyQualified, StringComparer.Ordinal)
			.ThenBy(static parameter => parameter.IsReferenceTypeOrNullableValueType)
			.ToImmutableArray()
			.AsEquatableArray();
	}

	private static void AddParameterTypes(
		ITypeSymbol type,
		ImmutableHashSet<MethodParameterInfo>.Builder builder)
	{
		foreach (IMethodSymbol method in type.GetMembers("Add").OfType<IMethodSymbol>())
		{
			if (method.Parameters.Length == 1)
			{
				builder.Add(MethodParameterInfo.From(method.Parameters[0].Type));
			}
		}
	}
}
