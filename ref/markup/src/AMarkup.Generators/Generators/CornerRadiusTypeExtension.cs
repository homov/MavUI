using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal class CornerRadiusTypeExtension : ITypeExtension
{
	public bool CanExtend(string qualifiedTypeName)
	{
		return qualifiedTypeName == "global::Avalonia.CornerRadius";
	}

	public void WriteAttachedPropertyBuilderExtensions(AttachedPropertyInfo prop, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("double", "topLeft").AddParameter("double", "topRight").AddParameter("double", "bottomRight")
			.AddParameter("double", "bottomLeft")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("return " + prop.Name + "(new global::Avalonia.CornerRadius(topLeft, topRight, bottomRight, bottomLeft));");
			});
		createBuilder().AddParameter("double", "uniformRadius").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return " + prop.Name + "(new global::Avalonia.CornerRadius(uniformRadius));");
		});
	}

	public void WritePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("double", "topLeft").AddParameter("double", "topRight").AddParameter("double", "bottomRight")
			.AddParameter("double", "bottomLeft")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("element." + info.PropertyName + " = new global::Avalonia.CornerRadius(topLeft, topRight, bottomRight, bottomLeft);");
				w.AppendLine("return element;");
			});
		createBuilder().AddParameter("double", "uniformRadius").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("element." + info.PropertyName + " = new global::Avalonia.CornerRadius(uniformRadius);");
			w.AppendLine("return element;");
		});
	}
	public void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("double", "topLeft").AddParameter("double", "topRight").AddParameter("double", "bottomRight")
			.AddParameter("double", "bottomLeft")
			.WithBody((ICodeWriter w) =>
			{
				w.AppendLine("return style.Setter(" + avaloniaProperty + ", new global::Avalonia.CornerRadius(topLeft, topRight, bottomRight, bottomLeft));");
			});
		createBuilder().AddParameter("double", "uniformRadius").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("return style.Setter(" + avaloniaProperty + ", new global::Avalonia.CornerRadius(uniformRadius));");
		});
	}

}
