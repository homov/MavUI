using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

[Generator("C#", new string[] { })]
internal sealed class AvaloniaEventExtensionGenerator : IncrementalExtensionsGeneratorBase<AvaloniaEventExtensionInfo>
{
	private protected override EquatableArray<AvaloniaEventExtensionInfo>? GetInfoForType(INamedTypeSymbol namedType)
	{
		if (!namedType.IsAvaloniaObject() || namedType.IsGenericType)
		{
			return null;
		}

		GenerationTypeInfo typeInfo = GenerationTypeInfo.From(namedType);
		ImmutableArray<AvaloniaEventExtensionInfo>.Builder builder = ImmutableArray.CreateBuilder<AvaloniaEventExtensionInfo>();

		foreach (IEventSymbol eventSymbol in namedType.GetMembersForGeneration()
			.OfType<IEventSymbol>()
			.OrderBy(static eventSymbol => eventSymbol.Name, StringComparer.Ordinal))
		{
			AvaloniaEventExtensionInfo? info = AvaloniaEventExtensionInfo.From(eventSymbol, typeInfo);
			if (info is not null)
			{
				builder.Add(info);
			}
		}

		return builder.Count == 0 ? null : builder.ToImmutable().AsEquatableArray();
	}

	private protected override string GetClassName(string typeName)
	{
		return typeName + "Markup";
	}

	private protected override void GenerateCodeFromInfosCore(
		ClassBuilder builder,
		EquatableArray<AvaloniaEventExtensionInfo> infos,
		SourceProductionContext context,
		CancellationToken cancellationToken)
	{
		foreach (AvaloniaEventExtensionInfo info in infos)
		{
			cancellationToken.ThrowIfCancellationRequested();
			GenerateEventExtensions(builder, info);
		}
	}

	private static void GenerateEventExtensions(ClassBuilder builder, AvaloniaEventExtensionInfo info)
	{
		string methodName = "On" + info.EventName;
		int parameterCount = info.ParameterTypes.AsImmutableArray().Length;
		string lambdaParameters = BuildLambdaParameters(parameterCount);

		// Provide an argument-less callback: OnClick(() => ...).
		// For a parameterless custom delegate the full-handler overload is
		// already callable with the same lambda shape, so avoid a duplicate.
		if (parameterCount > 0 || !info.HasCustomHandlerOverload)
		{
			CreateEventMethod(builder, info, methodName)
				.AddParameter("Action", "action")
				.WithBody((ICodeWriter writer) =>
				{
					writer.AppendLine("element." + info.EventName + " += (" + lambdaParameters + ") => action();");
					writer.AppendLine("return element;");
				});
		}

		// Provide the common event-arguments callback: OnClick(e => ...).
		// Standard EventHandler signatures drop the sender parameter. For
		// custom delegates with other signatures the full-handler overload
		// below is used to avoid two identical lambda shapes.
		if (parameterCount > 0
			&& (info.EventArgsTypeFullyQualified is not null || !info.HasCustomHandlerOverload))
		{
			IReadOnlyList<string> callbackTypes = info.EventArgsTypeFullyQualified is not null
				? new[] { info.EventArgsTypeFullyQualified }
				: info.ParameterTypes.ToArray();

			string callbackType = callbackTypes.Count == 1
				? "Action<" + callbackTypes[0] + ">"
				: "Action<" + string.Join(", ", callbackTypes) + ">";

			string callbackArguments = info.EventArgsTypeFullyQualified is not null
				? "arg1"
				: string.Join(", ", Enumerable.Range(0, parameterCount).Select(static index => "arg" + index));

			CreateEventMethod(builder, info, methodName)
				.AddParameter(callbackType, "action")
				.WithBody((ICodeWriter writer) =>
				{
					writer.AppendLine("element." + info.EventName + " += (" + lambdaParameters + ") => action(" + callbackArguments + ");");
					writer.AppendLine("return element;");
				});
		}

		// Keep the original delegate signature available: OnClick((sender, e) => ...).
		if (info.HasCustomHandlerOverload)
		{
			CreateEventMethod(builder, info, methodName)
				.AddParameter(info.HandlerTypeFullyQualified, "handler")
				.WithBody((ICodeWriter writer) =>
				{
					writer.AppendLine("element." + info.EventName + " += handler;");
					writer.AppendLine("return element;");
				});
		}
	}

	private static MethodBuilder CreateEventMethod(
		ClassBuilder builder,
		AvaloniaEventExtensionInfo info,
		string methodName)
	{
		MethodBuilder methodBuilder = builder.AddMethod(methodName)
			.MakePublicMethod()
			.MakeStaticMethod();

		if (info.GenerationTypeInfo.IsSealed)
		{
			methodBuilder
				.AddParameter("this " + info.GenerationTypeInfo.TypeFullyQualifiedName, "element")
				.WithReturnType(info.GenerationTypeInfo.TypeFullyQualifiedName);
		}
		else
		{
			methodBuilder
				.AddGeneric("T", (GenericBuilder genericBuilder) =>
				{
					genericBuilder.AddConstraint(info.GenerationTypeInfo.TypeFullyQualifiedName);
				})
				.AddParameter("this T", "element")
				.WithReturnType("T");
		}

		return methodBuilder;
	}

	private static string BuildLambdaParameters(int count)
	{
		return string.Join(", ", Enumerable.Range(0, count).Select(static index => "arg" + index));
	}
}
