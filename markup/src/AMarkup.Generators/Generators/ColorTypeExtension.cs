using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal class ColorTypeExtension : ITypeExtension
{
	public bool CanExtend(string qualifiedTypeName)
	{
		return qualifiedTypeName == "global::Avalonia.Media.Color";
	}

	public void WriteAttachedPropertyBuilderExtensions(AttachedPropertyInfo prop, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "hexString").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return " + prop.Name + "((global::Avalonia.Media.Color)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Color), hexString));");
		});
	}

	public void WritePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "hexString").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return element." + info.PropertyName + "((global::Avalonia.Media.Color)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Color), hexString));");
		});
	}
	public void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "hexString").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return style.Setter(" + avaloniaProperty + ", (global::Avalonia.Media.Color)global::AMarkup.MarkupBinding.ParseValue(typeof(global::Avalonia.Media.Color), hexString));");
		});
	}

}
