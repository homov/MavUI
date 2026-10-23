using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal sealed class GeometryTypeExtension : ITypeExtension
{
	public bool CanExtend(string qualifiedTypeName)
	{
		return qualifiedTypeName == "global::Avalonia.Media.Geometry";
	}

	public void WriteAttachedPropertyBuilderExtensions(AttachedPropertyInfo prop, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "path").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var geometry = (global::Avalonia.Media.Geometry)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Geometry), path);");
			w.AppendLine("return " + prop.Name + "(geometry);");
		});
	}

	public void WritePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "path").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var geometry = (global::Avalonia.Media.Geometry)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Geometry), path);");
			w.AppendLine("element." + info.PropertyName + " = geometry;");
			w.AppendLine("return element;");
		});
	}
	public void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "path").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var geometry = (global::Avalonia.Media.Geometry)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Geometry), path);");
			w.AppendLine("return style.Setter(" + avaloniaProperty + ", geometry);");
		});
	}

}
