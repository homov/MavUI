using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators;

[Generator("C#", new string[] { })]
internal sealed class AttachedPropertyGenerator : IncrementalExtensionsGeneratorBase<AttachedPropertyInfo>
{
	private protected override void GenerateCodeFromInfosCore(ClassBuilder builder, EquatableArray<AttachedPropertyInfo> infos, SourceProductionContext context, CancellationToken cancellationToken)
	{
		AttachedPropertyInfo attachedPropertyInfo = infos.FirstOrDefault();
		if ((object)attachedPropertyInfo != null)
		{
			GenerationTypeInfo generationTypeInfo = attachedPropertyInfo.GenerationTypeInfo;
			string typeName = generationTypeInfo.TypeName;
			CreateOptionParameterExtension(builder, generationTypeInfo, infos);
			CreateSimplePropertyExtensions(builder, generationTypeInfo, infos);
			CreateStyleExtensions(builder, generationTypeInfo, infos);
			CreateMethodBuilder(builder, generationTypeInfo).AddParameter("Action<" + typeName + "PropertyBuilder>", "configure").WithBody((ICodeWriter w) =>
			{
				w.AppendLine("var builder = new " + typeName + "PropertyBuilder(element);");
				w.AppendLine("configure(builder);");
				w.AppendLine("return element;");
			});
			CreatePropertyBuilder(generationTypeInfo, infos, context);
		}
	}

	private protected override string GetClassName(string typeName)
	{
		return typeName + "Markup";
	}

	private protected override EquatableArray<AttachedPropertyInfo>? GetInfoForType(INamedTypeSymbol namedType)
	{
		// Design.* and VisualStateManager expose design-time/state attached properties that
		// would collide with regular instance property extensions (DataContext, Width, Height).
		if (namedType.Name == "Design" || namedType.Name == "VisualStateManager" || namedType.IsGenericType)
		{
			return null;
		}
		ImmutableArray<ISymbol> members = namedType.GetMembers();
		ISymbol[] attachedProperties = members
			.Where(static x => IncrementalExtensionsGeneratorBase<AttachedPropertyInfo>.IsAvaloniaProperty(x) && x.IsAccessibleFromGeneratedMarkup())
			.ToArray();
		if (attachedProperties.Length == 0)
		{
			return null;
		}

		IMethodSymbol[] staticMethods = members.OfType<IMethodSymbol>()
			.Where(static x => x.IsStatic)
			.ToArray();
		if (staticMethods.Length == 0)
		{
			return null;
		}
		Dictionary<string, IMethodSymbol> getters = staticMethods
			.Where(static x => x.Name.StartsWith("Get", StringComparison.Ordinal))
			.GroupBy(static x => x.Name, StringComparer.Ordinal)
			.ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
		HashSet<string> instancePropertyNames = new HashSet<string>(StringComparer.Ordinal);
		for (INamedTypeSymbol? type = namedType; type is not null; type = type.BaseType)
		{
			foreach (ISymbol member in type.GetMembers())
			{
				if (member is IPropertySymbol { IsStatic: false } property)
				{
					instancePropertyNames.Add(property.Name);
				}
			}
		}

		ImmutableArray<AttachedPropertyInfo>.Builder builder = ImmutableArray.CreateBuilder<AttachedPropertyInfo>();
		GenerationTypeInfo generationTypeInfo = GenerationTypeInfo.From(namedType);
		foreach (ISymbol item in attachedProperties)
		{
			string name = item.Name.Substring(0, item.Name.Length - "Property".Length);
			// When the type also exposes an instance property with the same name (for example
			// TextElement.FontFamily or ScrollViewer.BringIntoViewOnFocusChange), the regular
			// property generator already emits a fluent extension with an identical signature.
			// Skipping the attached variant avoids duplicate members in the generated class.
			if (instancePropertyNames.Contains(name))
			{
				continue;
			}
			if (getters.TryGetValue("Get" + name, out IMethodSymbol? methodSymbol)
				&& methodSymbol.ReturnType is INamedTypeSymbol namedTypeSymbol)
			{
				builder.Add(new AttachedPropertyInfo(name, namedTypeSymbol.GetFullyQualifiedTypeIncludingGlobal(), namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T, "global::" + generationTypeInfo.TypeContainingNamespace + "." + namedType.Name + "." + item.Name, generationTypeInfo));
			}
		}
		if (builder.Count == 0)
		{
			return null;
		}
		return builder.OrderBy((AttachedPropertyInfo x) => x.Name).ToImmutableArray().AsEquatableArray();
	}



	private void CreatePropertyBuilder(GenerationTypeInfo typeInfo, EquatableArray<AttachedPropertyInfo> attachedProperties, SourceProductionContext context)
	{
		ClassBuilder builder = CodeBuilder.Create(typeInfo.TypeContainingNamespace).Nullable(NullableState.Enable).AddClass(typeInfo.TypeName + "PropertyBuilder")
			.WithAccessModifier(typeInfo.DeclaredAccessibility)
			.DisableWarning("AMarkup0001")
			.AddNamespaceImport("System")
			.AddNamespaceImport("System.Collections.Generic")
			.AddNamespaceImport("System.Runtime.CompilerServices")
			.AddNamespaceImport("System.Linq")
			.AddNamespaceImport("System.Linq.Expressions")
			.AddNamespaceImport("Avalonia")
			.AddNamespaceImport("Avalonia.Data")
			.AddNamespaceImport("AMarkup")
			.AddNamespaceImport(typeInfo.TypeContainingNamespace);
		builder.AddProperty("_weakElement").SetType("WeakReference<AvaloniaObject>?").MakePrivateProperty()
			.UseAutoProps();
		builder.AddProperty("_element").SetType("AvaloniaObject?").MakePrivateProperty()
			.WithGetter((ICodeWriter w) =>
			{
				w.If("_weakElement is not null && _weakElement.TryGetTarget(out var element)").WithBody((ICodeWriter x) =>
				{
					x.AppendLine("return element;");
				}).EndIf();
				w.AppendLine("return null;");
			})
			.WithSetter((ICodeWriter w) =>
			{
				w.If("value is null").WithBody((ICodeWriter x) =>
				{
					x.AppendLine("_weakElement = null;");
				}).Else()
					.WithBody((ICodeWriter x) =>
					{
						x.AppendLine("_weakElement = new WeakReference<AvaloniaObject>(value);");
					})
					.EndIf();
			});
		builder.AddConstructor(Accessibility.Public)
			.AddParameter("AvaloniaObject", "element")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("_element = element;");
			});
		foreach (AttachedPropertyInfo prop in attachedProperties)
		{
			builder.AddMethod(prop.Name).MakePublicMethod().WithReturnType(builder.Name)
				.AddParameter(prop.PropertyTypeFullyQualified, prop.Name.Camelcase())
				.WithBody((ICodeWriter w) =>
				{
					w.If("_element is null").WithBody((ICodeWriter x) =>
					{
						x.AppendLine("throw new Exception(\"The Element is null or has not been set.\");");
					}).EndIf();
					w.AppendLine("_element.SetValue(" + prop.AvaloniaProperty + ", " + prop.Name.Camelcase() + ");");
					w.AppendLine("return this;");
				});
			builder.AddMethod(prop.Name).MakePublicMethod().WithReturnType(builder.Name)
				.AddParameter("Action<IAvaloniaPropertyBuilder<" + prop.PropertyTypeFullyQualified + ">>", "configureProperty")
				.WithBody((ICodeWriter w) =>
				{
					w.If("_element is null").WithBody((ICodeWriter x) =>
					{
						x.AppendLine("throw new Exception(\"The Element is null or has not been set.\");");
					}).EndIf();
					w.AppendLine("var builder = new AvaloniaPropertyBuilder<" + prop.PropertyTypeFullyQualified + ">();");
					w.AppendLine("configureProperty(builder);");
					w.AppendLine("builder.SetBinding(_element, " + prop.AvaloniaProperty + ", \"" + prop.Name + "\");");
					w.AppendLine("return this;");
				});
			ExtensibilityLocator.Extensions.ForEach((ITypeExtension x) =>
			{
				if (x.CanExtend(prop.PropertyTypeFullyQualified))
				{
					x.WriteAttachedPropertyBuilderExtensions(prop, () => CreatePropertyBuilder(builder, prop));
				}
			});
			AddDataTemplateOverload(builder, prop);
		}
		AddSource(context, builder, typeInfo);
	}

	private static void CreateSimplePropertyExtensions(ClassBuilder builder, GenerationTypeInfo typeInfo, EquatableArray<AttachedPropertyInfo> attachedProperties)
	{
		foreach (AttachedPropertyInfo prop in attachedProperties)
		{
			// Attached-property setters are prefixed with the declaring type (GridRow,
			// CanvasLeft, DockPanelDock, TextSearchText, ...) so they never collide with
			// regular instance property extensions such as TextBlock.Text.
			string methodName = typeInfo.TypeName + prop.Name;

			// element.GridRow(1) / element.CanvasLeft(10) / element.DockPanelDock(Dock.Left) ...
			builder.AddMethod(methodName).MakePublicMethod().MakeStaticMethod()
				.AddGeneric("T", (GenericBuilder x) =>
				{
					x.AddConstraint("global::Avalonia.AvaloniaObject");
				})
				.AddParameter("this T", "element")
				.AddParameter(prop.PropertyTypeFullyQualified, prop.Name.Camelcase())
				.WithReturnType("T")
				.WithBody((ICodeWriter w) =>
				{
					w.AppendLine("element.SetValue(" + prop.AvaloniaProperty + ", " + prop.Name.Camelcase() + ");");
					w.AppendLine("return element;");
				});

			// element.Row(binding) - fluent attached-property binding.
			if (prop.PropertyTypeFullyQualified is not "global::Avalonia.Data.BindingBase")
			{
				builder.AddMethod(methodName).MakePublicMethod().MakeStaticMethod()
					.AddGeneric("T", (GenericBuilder x) =>
					{
						x.AddConstraint("global::Avalonia.AvaloniaObject");
					})
					.AddParameter("this T", "element")
					.AddParameter("Avalonia.Data.BindingBase", "binding")
					.WithReturnType("T")
					.WithBody((ICodeWriter w) =>
					{
						w.AppendLine("element.Bind(" + prop.AvaloniaProperty + ", binding);");
						w.AppendLine("return element;");
					});
			}

			// element.Row(source, x => x.Row) - strongly-typed expression binding.
			builder.AddMethod(methodName).MakePublicMethod().MakeStaticMethod()
				.AddGeneric("T", (GenericBuilder x) =>
				{
					x.AddConstraint("global::Avalonia.AvaloniaObject");
				})
				.AddGeneric("TSource")
				.AddGeneric("TSourceValue")
				.AddParameter("this T", "element")
				.AddParameter("TSource", "source")
				.AddParameter("Expression<Func<TSource, TSourceValue>>", "propertyBinding")
				.AddParameter("BindingMode?", "mode = null")
				.AddParameter("IValueConverter?", "converter = null")
				.AddParameter("string?", "stringFormat = null")
				.AddParameter("object?", "fallbackValue = null")
				.WithReturnType("T")
				.WithBody((ICodeWriter w) =>
				{
					w.AppendLine("element.Bind(" + prop.AvaloniaProperty + ", global::AMarkup.MarkupBinding.FromExpression(source, propertyBinding, mode, converter, " + prop.AvaloniaProperty + ", stringFormat, fallbackValue));");
					w.AppendLine("return element;");
				});

			// element.Row(path, source) - string path binding.
			builder.AddMethod(methodName).MakePublicMethod().MakeStaticMethod()
				.AddGeneric("T", (GenericBuilder x) =>
				{
					x.AddConstraint("global::Avalonia.AvaloniaObject");
				})
				.AddParameter("this T", "element")
				.AddParameter("string", "path")
				.AddParameter("object", "source")
				.AddParameter("BindingMode?", "mode = null")
				.AddParameter("IValueConverter?", "converter = null")
				.AddParameter("string?", "stringFormat = null")
				.AddParameter("object?", "fallbackValue = null")
				.WithReturnType("T")
				.WithBody((ICodeWriter w) =>
				{
					w.AppendLine("element.Bind(" + prop.AvaloniaProperty + ", new Avalonia.Data.Binding { Path = path, Source = source, Mode = mode ?? BindingMode.Default, Converter = converter, StringFormat = stringFormat, FallbackValue = fallbackValue });");
					w.AppendLine("return element;");
				});
		}
	}

	private static void CreateStyleExtensions(ClassBuilder builder, GenerationTypeInfo typeInfo, EquatableArray<AttachedPropertyInfo> attachedProperties)
	{
		foreach (AttachedPropertyInfo prop in attachedProperties)
		{
			string methodName = typeInfo.TypeName + prop.Name;
			string parameterName = prop.Name.Camelcase().EscapeIdentifier();
			if (parameterName == "style")
			{
				parameterName = "value";
			}

			CreateStyleBuilder(builder, methodName)
				.AddParameter(prop.PropertyTypeFullyQualified, parameterName)
				.WithBody((ICodeWriter w) =>
				{
					w.AppendLine("return style.Setter(" + prop.AvaloniaProperty + ", " + parameterName + ");");
				});

			if (prop.PropertyTypeFullyQualified is not "global::Avalonia.Data.BindingBase")
			{
				CreateStyleBuilder(builder, methodName)
					.AddParameter("global::Avalonia.Data.BindingBase", "binding")
					.WithBody((ICodeWriter w) =>
					{
						w.AppendLine("return style.Setter(" + prop.AvaloniaProperty + ", binding);");
					});
			}

			CreateStyleBuilder(builder, methodName)
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
					w.AppendLine("return style.Setter(" + prop.AvaloniaProperty + ", global::AMarkup.MarkupBinding.FromExpression(source, propertyBinding, mode, converter, " + prop.AvaloniaProperty + ", stringFormat, fallbackValue));");
				});

			CreateStyleBuilder(builder, methodName)
				.AddParameter("string", "path")
				.AddParameter("object", "source")
				.AddParameter("BindingMode?", "mode = null")
				.AddParameter("IValueConverter?", "converter = null")
				.AddParameter("string?", "stringFormat = null")
				.AddParameter("object?", "fallbackValue = null")
				.WithBody((ICodeWriter w) =>
				{
					w.AppendLine("return style.Setter(" + prop.AvaloniaProperty + ", new global::Avalonia.Data.Binding { Path = path, Source = source, Mode = mode ?? BindingMode.Default, Converter = converter, StringFormat = stringFormat, FallbackValue = fallbackValue });");
				});

			ExtensibilityLocator.Extensions.ForEach((ITypeExtension x) =>
			{
				if (x.CanExtend(prop.PropertyTypeFullyQualified))
				{
					x.WriteStyleExtensions(prop.AvaloniaProperty, () => CreateStyleBuilder(builder, methodName));
				}
			});
		}
	}

	private static MethodBuilder CreateStyleBuilder(ClassBuilder builder, string methodName)
	{
		return builder.AddMethod(methodName).MakePublicMethod().MakeStaticMethod()
			.AddGeneric("TControl", (GenericBuilder genericBuilder) =>
			{
				genericBuilder.AddConstraint("global::Avalonia.StyledElement");
			})
			.AddParameter("this global::AMarkup.Style<TControl>", "style")
			.WithReturnType("global::AMarkup.Style<TControl>");
	}

	private static void CreateOptionParameterExtension(ClassBuilder builder, GenerationTypeInfo info, EquatableArray<AttachedPropertyInfo> attachedProperties)
	{
		MethodBuilder methodBuilder = CreateMethodBuilder(builder, info);
		foreach (AttachedPropertyInfo item in attachedProperties)
		{
			string text = item.PropertyTypeFullyQualified;
			if (!item.PropertyTypeIsNullableValueType)
			{
				text += "?";
			}
			methodBuilder.AddParameterWithDefaultValue(text, item.Name.Camelcase(), -1);
		}
		methodBuilder.WithBody((ICodeWriter w) =>
		{
			foreach (AttachedPropertyInfo prop in attachedProperties)
			{
				string condition = prop.Name.Camelcase() + " != default";
				string paramValue = prop.Name.Camelcase();
				if (prop.PropertyTypeIsNullableValueType)
				{
					condition = prop.Name.Camelcase() + ".HasValue";
					paramValue += ".Value";
				}
				w.If(condition).WithBody((ICodeWriter x) =>
				{
					x.AppendLine("element.SetValue(" + prop.AvaloniaProperty + ", " + paramValue + ");");
				}).EndIf();
			}
			w.AppendLine("return element;");
		});
	}

	private static MethodBuilder CreateMethodBuilder(ClassBuilder builder, GenerationTypeInfo info)
	{
		return builder.AddMethod(info.TypeName).MakePublicMethod().MakeStaticMethod()
			.AddGeneric("T", (GenericBuilder x) =>
			{
				x.AddConstraint("global::Avalonia.AvaloniaObject");
			})
			.AddParameter("this T", "element")
			.WithReturnType("T");
	}

	private static MethodBuilder CreatePropertyBuilder(ClassBuilder builder, AttachedPropertyInfo prop)
	{
		return builder.AddMethod(prop.Name).MakePublicMethod().WithReturnType(builder.Name);
	}

	private static void AddDataTemplateOverload(ClassBuilder builder, AttachedPropertyInfo prop)
	{
		if (prop.PropertyTypeFullyQualified == "global::Avalonia.Controls.Templates.IDataTemplate")
		{
			CreatePropertyBuilder(builder, prop).AddGeneric("TItem").AddParameter("Func<TItem, Avalonia.Controls.Control>", "dataTemplateDelegate").WithBody((ICodeWriter w) =>
			{
				w.If("_element is null").WithBody((ICodeWriter x) =>
				{
					x.AppendLine("throw new InvalidOperationException(\"The element is null or has not been set.\");");
				}).EndIf();
				w.AppendLine("_element.SetValue(" + prop.AvaloniaProperty + ", new Avalonia.Controls.Templates.FuncDataTemplate<TItem>((item, _) => dataTemplateDelegate(item), true));");
				w.AppendLine("return this;");
			});
		}
	}
}
