using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AMarkup.Generators;

internal abstract class IncrementalExtensionsGeneratorBase<TModel> : IIncrementalGenerator where TModel : BaseModel, IEquatable<TModel>
{
	internal static readonly string ProductVersion = typeof(IncrementalExtensionsGeneratorBase<>).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";

	private protected virtual Accessibility? GeneratedClassAccessibilityOverride => null;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<EquatableArray<TModel>?> source = from infos in context.SyntaxProvider.CreateSyntaxProvider(CanBeNodeOfInterest, Transform)
			where infos.HasValue
			select infos;
		context.RegisterSourceOutput(source, (SourceProductionContext context2, EquatableArray<TModel>? infos) =>
		{
			if (!infos.Value.IsEmpty)
			{
				GenerateCodeFromInfos(context2, infos.Value);
			}
		});
		IncrementalValueProvider<EquatableArray<EquatableArray<TModel>>> source2 = context.CompilationProvider.Select((Compilation compilation, CancellationToken cancellationToken) =>
		{
			INamedTypeSymbol typeByMetadataName = compilation.GetTypeByMetadataName("AMarkup.GenerateMarkupForAssemblyAttribute");
			ImmutableArray<AttributeData> attributes = compilation.Assembly.GetAttributes();
			ImmutableArray<IAssemblySymbol>.Builder builder = ImmutableArray.CreateBuilder<IAssemblySymbol>();
			foreach (AttributeData item in attributes)
			{
				INamedTypeSymbol? attributeClass = item.AttributeClass;
				if (attributeClass != null && attributeClass.Equals(typeByMetadataName, SymbolEqualityComparer.Default) && item.ConstructorArguments[0].Value is INamedTypeSymbol namedTypeSymbol)
				{
					builder.Add(namedTypeSymbol.ContainingAssembly);
				}
			}
			return builder.ToImmutableArray();
		}).WithComparer(ImmutableArrayComparer.Instance).Select((ImmutableArray<IAssemblySymbol> assemblies, CancellationToken cancellationToken) =>
		{
			ImmutableArray<EquatableArray<TModel>>.Builder builder = ImmutableArray.CreateBuilder<EquatableArray<TModel>>();
			foreach (IAssemblySymbol item2 in assemblies)
			{
				foreach (INamedTypeSymbol publicClass in item2.GlobalNamespace.GetPublicClasses())
				{
					cancellationToken.ThrowIfCancellationRequested();
					EquatableArray<TModel>? infoForType = GetInfoForType(publicClass);
					if (infoForType.HasValue)
					{
						builder.Add(infoForType.GetValueOrDefault());
					}
				}
			}
			return builder.ToImmutableArray().AsEquatableArray();
		});
		context.RegisterSourceOutput(source2, (SourceProductionContext context2, EquatableArray<EquatableArray<TModel>> infosArray) =>
		{
			foreach (EquatableArray<TModel> item3 in infosArray)
			{
				context2.CancellationToken.ThrowIfCancellationRequested();
				if (!item3.IsEmpty)
				{
					GenerateCodeFromInfos(context2, item3);
				}
			}
		});
	}

	private static bool CanBeNodeOfInterest(SyntaxNode node, CancellationToken _)
	{
		if (!(node is ClassDeclarationSyntax classDeclarationSyntax))
		{
			return false;
		}
		if (!classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
		{
			BaseListSyntax baseList = classDeclarationSyntax.BaseList;
			if (baseList != null)
			{
				return baseList.Types.Count > 0;
			}
			return false;
		}
		return true;
	}

	private EquatableArray<TModel>? Transform(GeneratorSyntaxContext context, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		ClassDeclarationSyntax classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
		INamedTypeSymbol? declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, ct);
		if (declaredSymbol is null)
		{
			return null;
		}
		if (!declaredSymbol.IsVisibleFromGeneratedMarkupScope())
		{
			return null;
		}
		if (declaredSymbol.DeclaringSyntaxReferences[0].GetSyntax(ct) != classDeclarationSyntax)
		{
			return null;
		}
		return GetInfoForType(declaredSymbol);
	}

	protected static bool IsAvaloniaProperty(ISymbol symbol)
	{
		if (!symbol.IsStatic)
		{
			return false;
		}

		ITypeSymbol? type = symbol switch
		{
			IFieldSymbol field => field.Type,
			IPropertySymbol property => property.Type,
			_ => null,
		};

		for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
		{
			if (current.ToDisplayString() == "Avalonia.AvaloniaProperty")
			{
				return true;
			}
		}

		return false;
	}

	private void GenerateCodeFromInfos(SourceProductionContext context, EquatableArray<TModel> infos)
	{
		TModel val = infos[0];
		string text = val.GenerationTypeInfo.TypeContainingNamespace;
		if (text == "Avalonia.Controls.Primitives")
		{
			text = "Avalonia.Controls";
		}
		Accessibility? generatedClassAccessibilityOverride = GeneratedClassAccessibilityOverride;
		Accessibility accessibility;
		if (!generatedClassAccessibilityOverride.HasValue)
		{
			val = infos[0];
			accessibility = val.GenerationTypeInfo.DeclaredAccessibility;
		}
		else
		{
			accessibility = generatedClassAccessibilityOverride.GetValueOrDefault();
		}
		Accessibility accessModifier = accessibility;
		CodeBuilder codeBuilder = CodeBuilder.Create(text).Nullable(NullableState.Enable);
		val = infos[0];
		ClassBuilder classBuilder = codeBuilder.AddClass(GetClassName(val.GenerationTypeInfo.TypeName)).WithAccessModifier(accessModifier).MakeStaticClass()
			.DisableWarning("AMarkup0001")
			.AddNamespaceImport("System")
			.AddNamespaceImport("System.Collections.Generic")
			.AddNamespaceImport("System.Runtime.CompilerServices")
			.AddNamespaceImport("System.Linq")
			.AddNamespaceImport("System.Linq.Expressions")
			.AddNamespaceImport("Avalonia")
			.AddNamespaceImport("Avalonia.Controls")
			.AddNamespaceImport("Avalonia.Controls.Templates")
			.AddNamespaceImport("Avalonia.Data")
			.AddNamespaceImport("Avalonia.Data.Converters")
			.AddNamespaceImport("Avalonia.Markup")
			.AddNamespaceImport("AMarkup");
		val = infos[0];
		if (val.GenerationTypeInfo.TypeContainingNamespace == "Avalonia.Controls.Primitives")
		{
			classBuilder.AddNamespaceImport("Avalonia.Controls.Primitives");
		}
		GenerateCodeFromInfosCore(classBuilder, infos, context, context.CancellationToken);

		SourceProductionContext context2 = context;
		val = infos[0];
		AddSource(context2, classBuilder, val.GenerationTypeInfo);
	}

	private protected void AddSource(SourceProductionContext context, ClassBuilder builder, GenerationTypeInfo generationTypeInfo, string? fileNamePrefix = null)
	{
		if (!builder.Methods.Any())
		{
			return;
		}
		builder.AddNamespaceImport("System.CodeDom.Compiler");
		foreach (MethodBuilder method in builder.Methods)
		{
			method.AddAttribute("GeneratedCode(\"AMarkup\", \"" + ProductVersion + "\")");
		}
		string source = builder.Build();
		string text = GetGeneratedFileName(builder, generationTypeInfo);
		if (!string.IsNullOrEmpty(fileNamePrefix))
		{
			text = fileNamePrefix + "_" + text;
		}
		context.AddSource(text + ".g.cs", source);
	}

	private protected virtual string GetGeneratedFileName(ClassBuilder builder, GenerationTypeInfo generationTypeInfo)
	{
		return builder.FullyQualifiedName;
	}

	private protected abstract EquatableArray<TModel>? GetInfoForType(INamedTypeSymbol namedType);

	private protected abstract string GetClassName(string typeName);

	private protected abstract void GenerateCodeFromInfosCore(ClassBuilder builder, EquatableArray<TModel> infos, SourceProductionContext context, CancellationToken cancellationToken);
}
