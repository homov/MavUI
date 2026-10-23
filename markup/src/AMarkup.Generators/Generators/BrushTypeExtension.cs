using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal class BrushTypeExtension : ITypeExtension
{
	public bool CanExtend(string qualifiedTypeName)
	{
		if (!(qualifiedTypeName == "global::Avalonia.Media.IBrush"))
		{
			return qualifiedTypeName == "global::Avalonia.Media.SolidColorBrush";
		}
		return true;
	}

	public void WriteAttachedPropertyBuilderExtensions(AttachedPropertyInfo prop, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("global::Avalonia.Media.Color", "solidColor").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return " + prop.Name + "(new global::Avalonia.Media.SolidColorBrush(solidColor));");
		});
		createBuilder().AddParameter("string", "hexString").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var solidColor = (global::Avalonia.Media.Color)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Color), hexString);");
			w.AppendLine("return " + prop.Name + "(new global::Avalonia.Media.SolidColorBrush(solidColor));");
		});
	}

	public void WritePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("global::Avalonia.Media.Color", "solidColor").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("element." + info.PropertyName + " = new global::Avalonia.Media.SolidColorBrush(solidColor);");
			w.AppendLine("return element;");
		});
		createBuilder().AddParameter("string", "hexString").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var solidColor = (global::Avalonia.Media.Color)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Color), hexString);");
			w.AppendLine("element." + info.PropertyName + " = new global::Avalonia.Media.SolidColorBrush(solidColor);");
			w.AppendLine("return element;");
		});
	}
	public void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("global::Avalonia.Media.Color", "solidColor").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return style.Setter(" + avaloniaProperty + ", new global::Avalonia.Media.SolidColorBrush(solidColor));");
		});
		createBuilder().AddParameter("string", "hexString").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var solidColor = (global::Avalonia.Media.Color)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Color), hexString);");
			w.AppendLine("return style.Setter(" + avaloniaProperty + ", new global::Avalonia.Media.SolidColorBrush(solidColor));");
		});
	}

}
