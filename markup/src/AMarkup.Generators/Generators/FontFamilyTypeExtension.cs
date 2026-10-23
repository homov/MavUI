using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal class FontFamilyTypeExtension : ITypeExtension
{
	public bool CanExtend(string qualifiedTypeName)
	{
		return qualifiedTypeName == "global::Avalonia.Media.FontFamily";
	}

	public void WriteAttachedPropertyBuilderExtensions(AttachedPropertyInfo prop, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "fontFamily").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return " + prop.Name + "(new global::Avalonia.Media.FontFamily(fontFamily));");
		});
	}

	public void WritePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "fontFamily").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("element." + info.PropertyName + " = new global::Avalonia.Media.FontFamily(fontFamily);");
			w.AppendLine("return element;");
		});
	}
	public void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "fontFamily").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return style.Setter(" + avaloniaProperty + ", new global::Avalonia.Media.FontFamily(fontFamily));");
		});
	}

}
