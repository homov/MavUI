using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

[Generator("C#", new string[] { })]
internal sealed class DataTemplateGenerator : IncrementalExtensionsGeneratorBase<DataTemplateInfo>
{
	private protected override EquatableArray<DataTemplateInfo>? GetInfoForType(INamedTypeSymbol namedType)
	{
		if (namedType.IsGenericType || !namedType.IsAvaloniaObject())
		{
			return null;
		}

		GenerationTypeInfo typeInfo = GenerationTypeInfo.From(namedType);
		ImmutableArray<DataTemplateInfo>.Builder builder = ImmutableArray.CreateBuilder<DataTemplateInfo>();

		foreach (IPropertySymbol property in namedType.GetMembersForGeneration().OfType<IPropertySymbol>())
		{
			if (property.IsStatic || !property.IsAssignableFromGeneratedMarkup() || property.Type is not INamedTypeSymbol propertyType)
			{
				continue;
			}

			bool isDataTemplate = propertyType.IsDataTemplate();
			bool isControlTemplate = propertyType.IsControlTemplate();

			if (isDataTemplate || isControlTemplate)
			{
				builder.Add(new DataTemplateInfo(
					propertyType.GetFullyQualifiedTypeIncludingGlobal(),
					isControlTemplate,
					isDataTemplate,
					property.Name,
					typeInfo));
			}
		}

		return builder.Count == 0 ? null : builder.ToImmutable().AsEquatableArray();
	}

	private protected override string GetClassName(string typeName) => typeName + "Markup";

	private protected override void GenerateCodeFromInfosCore(
		ClassBuilder builder,
		EquatableArray<DataTemplateInfo> infos,
		SourceProductionContext context,
		CancellationToken cancellationToken)
	{
		foreach (DataTemplateInfo info in infos)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (info.PropertyTypeIsControlTemplate)
			{
				builder.AddMethod(info.PropertyName)
					.MakePublicMethod()
					.MakeStaticMethod()
					.WithReturnType(info.GenerationTypeInfo.TypeFullyQualifiedName)
					.AddParameter("this " + info.GenerationTypeInfo.TypeFullyQualifiedName, "element")
					.AddParameter("Func<Control>", "build")
					.WithBody((ICodeWriter w) =>
					{
						w.AppendLine("element." + info.PropertyName + " = new Avalonia.Controls.Templates.FuncControlTemplate((_, _) => build());");
						w.AppendLine("return element;");
					});
				continue;
			}

			builder.AddMethod(info.PropertyName)
				.MakePublicMethod()
				.MakeStaticMethod()
				.WithReturnType(info.GenerationTypeInfo.TypeFullyQualifiedName)
				.AddGeneric("TItem")
				.AddParameter("this " + info.GenerationTypeInfo.TypeFullyQualifiedName, "element")
				.AddParameter("Func<TItem, Control>", "build")
				.WithBody((ICodeWriter w) =>
				{
					w.AppendUnindentedLine("#nullable disable");
					w.AppendLine("element." + info.PropertyName + " = new Avalonia.Controls.Templates.FuncDataTemplate<TItem>((item, _) => build(item), true);");
					w.AppendUnindentedLine("#nullable enable");
					w.AppendLine("return element;");
				});
		}
	}
}
