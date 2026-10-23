using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

[Generator("C#", new string[] { })]
internal sealed class AvaloniaPropertyExtensionGenerator : IncrementalExtensionsGeneratorBase<AvaloniaPropertyExtensionInfo>
{
	private protected override EquatableArray<AvaloniaPropertyExtensionInfo>? GetInfoForType(INamedTypeSymbol namedType)
	{
		if (!namedType.IsAvaloniaObject() || namedType.IsGenericType)
		{
			return null;
		}
		if (namedType.Name is "Binding" or "Setter")
		{
			return null;
		}
		ImmutableArray<ISymbol> membersForGeneration = namedType.GetMembersForGeneration();
		IEnumerable<IPropertySymbol> enumerable = from x in membersForGeneration.OfType<IPropertySymbol>()
			where x.IsCollectionWithAddMethod() || x.HasPublicSetter()
			select x;
		HashSet<string> avaloniaPropertyNames = new HashSet<string>(
			membersForGeneration
				.Where(m => m.IsAccessibleFromGeneratedMarkup() && IsAvaloniaProperty(m))
				.Select(m => m.Name),
			StringComparer.Ordinal);
		HashSet<string>? basePropertyNames = null;
		if (!namedType.IsSealed)
		{
			basePropertyNames = new HashSet<string>(StringComparer.Ordinal);
			for (INamedTypeSymbol? baseType = namedType.BaseType; baseType is not null; baseType = baseType.BaseType)
			{
				foreach (ISymbol baseMember in baseType.GetMembers())
				{
					if (baseMember is IPropertySymbol baseProperty)
					{
						basePropertyNames.Add(baseProperty.Name);
					}
				}
			}
		}

		ImmutableArray<AvaloniaPropertyExtensionInfo>.Builder builder = ImmutableArray.CreateBuilder<AvaloniaPropertyExtensionInfo>();
		GenerationTypeInfo typeInfo = GenerationTypeInfo.From(namedType);
		bool isStyledElement = namedType.IsStyledElement();
		foreach (IPropertySymbol property in enumerable)
		{
			if (!property.IsStatic
				&& !property.IsIndexer
				&& !property.Name.Contains(".")
				&& property.DeclaredAccessibility is not Accessibility.Private and not Accessibility.Protected
				&& !IsIgnoredProperty(property))
			{
				builder.Add(AvaloniaPropertyExtensionInfo.From(
					property,
					typeInfo,
					avaloniaPropertyNames.Contains(property.Name + "Property"),
					basePropertyNames?.Contains(property.Name) == true,
					isStyledElement));
			}
		}
		return builder.ToImmutable().AsEquatableArray();
	}

	private static string AdjustParameterName(string parameterName)
	{
		if (parameterName is "element" or "style")
		{
			return "value";
		}
		return parameterName.EscapeIdentifier();
	}

	private static bool IsIgnoredProperty(IPropertySymbol property)
	{
		if (property.Name == "TemplatedParent")
		{
			return true;
		}

		if (property.Name == "BasedOn")
		{
			return property.ContainingType.Name == "Style";
		}

		if (property.Name == "TargetType")
		{
			return property.ContainingType.Name is "Style" or "ControlTemplate";
		}

		if (property.Name == "RoutedEvent")
		{
			return property.ContainingType.Name == "EventTrigger";
		}

		return property.Name == "Name"
			&& property.ContainingType.Name is "VisualState" or "VisualStateGroup";
	}

	private protected override string GetClassName(string typeName)
	{
		return typeName + "Markup";
	}

	private protected override void GenerateCodeFromInfosCore(ClassBuilder builder, EquatableArray<AvaloniaPropertyExtensionInfo> infos, SourceProductionContext context, CancellationToken cancellationToken)
	{
		foreach (AvaloniaPropertyExtensionInfo item in infos)
		{
			cancellationToken.ThrowIfCancellationRequested();
			GeneratePropertyExtensions(builder, item);
		}
	}

	private void GeneratePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info)
	{
		string type = info.PropertyTypeFullyQualified;
		if (info.IsStyledElement
			&& info.IsAvaloniaProperty
			&& info.PropertyHasPublicSetter
			&& !info.PropertyIsCollection)
		{
			GenerateStyleExtensions(builder, info);
		}
		if (info.PropertyHasPublicSetter)
		{
			string parameterName = AdjustParameterName(info.PropertyName.Camelcase());
			MethodBuilder methodBuilder = CreatePropertyBuilder(ref builder, info.GenerationTypeInfo, info.PropertyName, info.IsNotSealedAndIsShadowing).AddParameter(type, parameterName);
			if (type == "global::Avalonia.Controls.ResourceDictionary")
			{
				methodBuilder.WithBody((ICodeWriter w) =>
				{
					if (info.PropertyName == "Resources")
					{
						w.AppendLine("element.Resources ??= new ResourceDictionary();");
						w.AppendLine("element.Resources.MergedDictionaries.Add(" + parameterName + ");");
					}
					else
					{
						w.AppendLine("element." + info.PropertyName + " = " + parameterName + ";");
					}
					w.AppendLine("return element;");
				});
			}
			else
			{
				methodBuilder.WithBody((ICodeWriter w) =>
				{
					w.AppendLine("element." + info.PropertyName + " = " + parameterName + ";");
					w.AppendLine("return element;");
				});
			}
			ExtensibilityLocator.Extensions.ForEach((ITypeExtension x) =>
			{
				if (x.CanExtend(info.PropertyTypeFullyQualified))
				{
					x.WritePropertyExtensions(builder, info, () => CreatePropertyBuilder(ref builder, info.GenerationTypeInfo, info.PropertyName));
				}
			});
			if (info.PropertyIsCollection)
			{
				CreateCollectionExtension(builder, info);
			}
			if (!info.IsAvaloniaProperty)
			{
				return;
			}
			if (!info.IsTemplateType)
			{
				// Generate a strongly-typed source + expression overload (compiled binding).
				CreatePropertyBuilder(ref builder, info.GenerationTypeInfo, info.PropertyName)
					.AddGeneric("TSource")
					.AddGeneric("TSourceValue")
					.AddParameter("TSource", "source")
					.AddParameter("Expression<Func<TSource, TSourceValue>>", "propertyBinding")
					.AddParameter("BindingMode?", "mode = null")
					.AddParameter("IValueConverter?", "converter = null")
					.AddParameter("string?", "stringFormat = null")
					.AddParameter("object?", "fallbackValue = null")
					.WithBody((ICodeWriter w) =>
					{
						string property = info.GenerationTypeInfo.TypeFullyQualifiedName + "." + info.PropertyName + "Property";
						w.AppendLine("element.Bind(" + property + ", global::AMarkup.MarkupBinding.FromExpression(source, propertyBinding, mode, converter, " + property + ", stringFormat, fallbackValue));");
						w.AppendLine("return element;");
					});
				// Generate BindingBase overload - accepts ready-made bindings (reflection Binding, CompiledBinding, etc.)
				if (info.PropertyTypeFullyQualified is not "global::Avalonia.Data.BindingBase")
				{
					CreatePropertyBuilder(ref builder, info.GenerationTypeInfo, info.PropertyName).AddParameter("Avalonia.Data.BindingBase", "binding")
						.WithBody((ICodeWriter w) =>
						{
							w.AppendLine("element.Bind(" + info.GenerationTypeInfo.TypeFullyQualifiedName + "." + info.PropertyName + "Property, binding);");
							w.AppendLine("return element;");
						});
				}
				// Generate string path overload - creates Binding from path
				CreatePropertyBuilder(ref builder, info.GenerationTypeInfo, info.PropertyName)
					.AddParameter("string", "path")
					.AddParameter("object", "source")
					.AddParameter("BindingMode?", "mode = null")
					.AddParameter("IValueConverter?", "converter = null")
					.AddParameter("string?", "stringFormat = null")
					.AddParameter("object?", "fallbackValue = null")
					.WithBody((ICodeWriter w) =>
					{
						w.AppendLine("element.Bind(" + info.GenerationTypeInfo.TypeFullyQualifiedName + "." + info.PropertyName + "Property, new Avalonia.Data.Binding { Path = path, Source = source, Mode = mode ?? BindingMode.Default, Converter = converter, StringFormat = stringFormat, FallbackValue = fallbackValue });");
						w.AppendLine("return element;");
					});
			}
			CreatePropertyBuilder(ref builder, info.GenerationTypeInfo, info.PropertyName).AddParameter("Action<IAvaloniaPropertyBuilder<" + type + ">>", "configureProperty").WithBody((ICodeWriter w) =>
			{
				w.AppendLine("var builder = new AvaloniaPropertyBuilder<" + type + ">();");
				w.AppendLine("configureProperty(builder);");
				w.AppendLine("builder.SetBinding(element, " + info.GenerationTypeInfo.TypeFullyQualifiedName + "." + info.PropertyName + "Property, \"" + info.PropertyName + "\");");
				w.AppendLine("return element;");
			});
		}
		else if (info.PropertyIsCollection && info.PropertyDeclaredAccessibility == Accessibility.Public)
		{
			CreateCollectionExtension(builder, info);
		}
	}

	private void GenerateStyleExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info)
	{
		string property = info.GenerationTypeInfo.TypeFullyQualifiedName + "." + info.PropertyName + "Property";
		string parameterName = AdjustParameterName(info.PropertyName.Camelcase());

		CreateStyleBuilder(builder, info, info.PropertyName)
			.AddParameter(info.PropertyTypeFullyQualified, parameterName)
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("return style.Setter(" + property + ", " + parameterName + ");");
			});

		if (info.PropertyTypeFullyQualified is not "global::Avalonia.Data.BindingBase")
		{
			CreateStyleBuilder(builder, info, info.PropertyName)
				.AddParameter("global::Avalonia.Data.BindingBase", "binding")
				.WithBody((ICodeWriter w) =>
				{
					w.AppendLine("return style.Setter(" + property + ", binding);");
				});
		}

		CreateStyleBuilder(builder, info, info.PropertyName)
			.AddGeneric("TSource")
			.AddGeneric("TSourceValue")
			.AddParameter("TSource", "source")
			.AddParameter("Expression<Func<TSource, TSourceValue>>", "propertyBinding")
			.AddParameter("BindingMode?", "mode = null")
			.AddParameter("IValueConverter?", "converter = null")
			.AddParameter("string?", "stringFormat = null")
			.AddParameter("object?", "fallbackValue = null")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("return style.Setter(" + property + ", global::AMarkup.MarkupBinding.FromExpression(source, propertyBinding, mode, converter, " + property + ", stringFormat, fallbackValue));");
			});

		CreateStyleBuilder(builder, info, info.PropertyName)
			.AddParameter("string", "path")
			.AddParameter("object", "source")
			.AddParameter("BindingMode?", "mode = null")
			.AddParameter("IValueConverter?", "converter = null")
			.AddParameter("string?", "stringFormat = null")
			.AddParameter("object?", "fallbackValue = null")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("return style.Setter(" + property + ", new global::Avalonia.Data.Binding { Path = path, Source = source, Mode = mode ?? BindingMode.Default, Converter = converter, StringFormat = stringFormat, FallbackValue = fallbackValue });");
			});

		ExtensibilityLocator.Extensions.ForEach((ITypeExtension x) =>
		{
			if (x.CanExtend(info.PropertyTypeFullyQualified))
			{
				x.WriteStyleExtensions(property, () => CreateStyleBuilder(builder, info, info.PropertyName));
			}
		});
	}

	private static MethodBuilder CreateStyleBuilder(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, string methodName)
	{
		string styleType = info.GenerationTypeInfo.IsSealed
			? "global::AMarkup.Style<" + info.GenerationTypeInfo.TypeFullyQualifiedName + ">"
			: "global::AMarkup.Style<TControl>";

		MethodBuilder methodBuilder = builder.AddMethod(methodName).MakePublicMethod().MakeStaticMethod();

		if (!info.GenerationTypeInfo.IsSealed)
		{
			methodBuilder.AddGeneric("TControl", (GenericBuilder genericBuilder) =>
			{
				genericBuilder.AddConstraint(info.GenerationTypeInfo.TypeFullyQualifiedName);
			});
		}

		return methodBuilder
			.AddParameter("this " + styleType, "style")
			.WithReturnType(styleType);
	}

	private void CreateCollectionExtension(ClassBuilder builder, AvaloniaPropertyExtensionInfo info)
	{
		EquatableArray<MethodParameterInfo> methodParameterTypes = info.MethodParameterTypes;
		string parameterName = ((info.PropertyName.EndsWith("s") || info.PropertyName == "Children") ? info.PropertyName.Camelcase() : (info.PropertyName.Camelcase() + "s"));
		parameterName = AdjustParameterName(parameterName);
		foreach (MethodParameterInfo parameterSymbol in methodParameterTypes)
		{
			CreatePropertyBuilder(ref builder, info.GenerationTypeInfo, info.PropertyName).AddParameter("params " + parameterSymbol.ParameterTypeFullyQualified + "[]", parameterName).WithBody((ICodeWriter w) =>
			{
				if (info.PropertyHasPublicSetter && info.PropertyTypeIsReferenceTypeOrNullableValueType && info.PropertyTypeIsInstantiable)
				{
					w.AppendLine("element." + info.PropertyName + " ??= new " + info.PropertyTypeFullyQualified + "();");
				}
				else if (info.PropertyTypeIsReferenceTypeOrNullableValueType)
				{
					w.If("element." + info.PropertyName + " == null").WithBody((ICodeWriter x) =>
					{
						x.AppendLine("throw new NullReferenceException(\"" + info.PropertyName + "\");");
					}).EndIf();
				}
				w.ForEach("var item", parameterName).WithBody((ICodeWriter body) =>
				{
					if (parameterSymbol.IsReferenceTypeOrNullableValueType)
					{
						body.If("item is null").WithBody((ICodeWriter ifWrite) =>
						{
							ifWrite.AppendLine("continue;");
						}).EndIf();
					}

					body.AppendLine("element." + info.PropertyName + ".Add(item);");
				});
				w.AppendLine("return element;");
			});
		}
	}

	private static MethodBuilder CreatePropertyBuilder(ref ClassBuilder builder, GenerationTypeInfo generationTypeInfo, string propertyName, bool forceStronglyTyped = false)
	{
		MethodBuilder methodBuilder = builder.AddMethod(propertyName).MakePublicMethod().MakeStaticMethod();
		if (generationTypeInfo.IsSealed | forceStronglyTyped)
		{
			methodBuilder.AddParameter("this " + generationTypeInfo.TypeFullyQualifiedName, "element").WithReturnType(generationTypeInfo.TypeFullyQualifiedName);
		}
		else
		{
			methodBuilder.AddGeneric("T", (GenericBuilder _) =>
			{
				_.AddConstraint(generationTypeInfo.TypeFullyQualifiedName);
			}).AddParameter("this T", "element").WithReturnType("T");
		}
		return methodBuilder;
	}
}
