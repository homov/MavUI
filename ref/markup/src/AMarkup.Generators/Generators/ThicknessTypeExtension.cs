using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal class ThicknessTypeExtension : ITypeExtension
{
	public bool CanExtend(string qualifiedTypeName)
	{
		return qualifiedTypeName == "global::Avalonia.Thickness";
	}

	public void WriteAttachedPropertyBuilderExtensions(AttachedPropertyInfo prop, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("double", "uniformLength").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return " + prop.Name + "(new Thickness(uniformLength));");
		});
		createBuilder().AddParameter("double", "x").AddParameter("double", "y").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return " + prop.Name + "(new Thickness(x, y, x, y));");
		});
		createBuilder().AddParameter("double", "left").AddParameter("double", "top").AddParameter("double", "right")
			.AddParameter("double", "bottom")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("return " + prop.Name + "(new Thickness(left, top, right, bottom));");
			});
	}

	public void WritePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, Func<MethodBuilder> createBuilder)
	{
		_ = info.GenerationTypeInfo;
		createBuilder().AddParameter("double", "uniformLength").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("element." + info.PropertyName + " = new Thickness(uniformLength);");
			w.AppendLine("return element;");
		});
		createBuilder().AddParameter("double", "x").AddParameter("double", "y").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("element." + info.PropertyName + " = new Thickness(x, y, x, y);");
			w.AppendLine("return element;");
		});
		createBuilder().AddParameter("double", "left").AddParameter("double", "top").AddParameter("double", "right")
			.AddParameter("double", "bottom")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("element." + info.PropertyName + " = new Thickness(left, top, right, bottom);");
				w.AppendLine("return element;");
			});
	}
	public void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("double", "uniformLength").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return style.Setter(" + avaloniaProperty + ", new Thickness(uniformLength));");
		});
		createBuilder().AddParameter("double", "x").AddParameter("double", "y").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return style.Setter(" + avaloniaProperty + ", new Thickness(x, y, x, y));");
		});
		createBuilder().AddParameter("double", "left").AddParameter("double", "top").AddParameter("double", "right")
			.AddParameter("double", "bottom")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("return style.Setter(" + avaloniaProperty + ", new Thickness(left, top, right, bottom));");
			});
	}

}
