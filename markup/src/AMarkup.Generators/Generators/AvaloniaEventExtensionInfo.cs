using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

/// <summary>
/// Describes one public event that can be attached through a generated fluent method.
/// </summary>
internal record AvaloniaEventExtensionInfo(
	string EventName,
	string HandlerTypeFullyQualified,
	EquatableArray<string> ParameterTypes,
	string? EventArgsTypeFullyQualified,
	bool HasCustomHandlerOverload,
	GenerationTypeInfo GenerationTypeInfo) : BaseModel(GenerationTypeInfo)
{
	public static AvaloniaEventExtensionInfo? From(IEventSymbol eventSymbol, GenerationTypeInfo typeInfo)
	{
		if (eventSymbol.IsStatic
			|| eventSymbol.Name.Contains('.')
			|| !eventSymbol.IsAccessibleFromGeneratedMarkup()
			|| IsObsolete(eventSymbol))
		{
			return null;
		}

		if (eventSymbol.Type is not INamedTypeSymbol delegateType
			|| delegateType.DelegateInvokeMethod is not { } invokeMethod
			|| !invokeMethod.ReturnsVoid
			|| invokeMethod.Parameters.Any(static parameter => parameter.RefKind != RefKind.None))
		{
			return null;
		}

		ImmutableArray<string> parameterTypes = invokeMethod.Parameters
			.Select(static parameter => parameter.Type.GetFullyQualifiedTypeIncludingGlobal())
			.ToImmutableArray();

		string? eventArgsType = null;
		if (invokeMethod.Parameters.Length == 2
			&& invokeMethod.Parameters[0].Type.SpecialType == SpecialType.System_Object
			&& IsEventArgs(invokeMethod.Parameters[1].Type))
		{
			eventArgsType = invokeMethod.Parameters[1].Type.GetFullyQualifiedTypeIncludingGlobal();
		}

		bool isActionDelegate = delegateType.ContainingNamespace.ToDisplayString() == "System"
			&& delegateType.Name == "Action";

		return new AvaloniaEventExtensionInfo(
			eventSymbol.Name,
			delegateType.GetFullyQualifiedTypeIncludingGlobal(),
			parameterTypes.AsEquatableArray(),
			eventArgsType,
			!isActionDelegate,
			typeInfo);
	}

	private static bool IsObsolete(IEventSymbol eventSymbol)
	{
		return eventSymbol.GetAttributes().Any(static attribute =>
			attribute.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute");
	}

	private static bool IsEventArgs(ITypeSymbol type)
	{
		for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
		{
			if (current.GetFullyQualifiedTypeExcludingGlobal() == "System.EventArgs")
			{
				return true;
			}
		}

		return type.Name.EndsWith("EventArgs", StringComparison.Ordinal);
	}
}
