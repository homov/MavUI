using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal class ImageSourceTypeExtension : ITypeExtension
{
	public bool CanExtend(string qualifiedTypeName)
	{
		return qualifiedTypeName == "global::Avalonia.Media.IImage";
	}

	public void WriteAttachedPropertyBuilderExtensions(AttachedPropertyInfo prop, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "source").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var assemblyName = global::System.Reflection.Assembly.GetCallingAssembly().GetName().Name ?? throw new global::System.NullReferenceException(\"Unable to determine the calling assembly. Please use a fully qualified uri.\");");
			w.AppendLine("var imageSource = MarkupImageSource.Load(source, assemblyName);");
			w.If("imageSource is not null").WithBody((ICodeWriter x) =>
			{
				x.AppendLine("return " + prop.Name + "(imageSource);");
			}).EndIf();
			w.AppendLine("return builder;");
		});
		createBuilder().AddParameter("global::System.Uri", "uri").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var assemblyName = global::System.Reflection.Assembly.GetCallingAssembly().GetName().Name ?? throw new global::System.NullReferenceException(\"Unable to determine the calling assembly. Please use a fully qualified uri.\");");
			w.AppendLine("var imageSource = MarkupImageSource.Load(uri, assemblyName);");
			w.If("imageSource is not null").WithBody((ICodeWriter x) =>
			{
				x.AppendLine("return " + prop.Name + "(imageSource);");
			}).EndIf();
			w.AppendLine("return builder;");
		});
	}

	public void WritePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "source").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var assemblyName = global::System.Reflection.Assembly.GetCallingAssembly().GetName().Name ?? throw new global::System.NullReferenceException(\"Unable to determine the calling assembly. Please use a fully qualified uri.\");");
			w.AppendLine("var imageSource = MarkupImageSource.Load(source, assemblyName);");
			w.If("imageSource is not null").WithBody((ICodeWriter x) =>
			{
				x.AppendLine("return element." + info.PropertyName + "(imageSource);");
			}).EndIf();
			w.AppendLine("return element;");
		});
		createBuilder().AddParameter("global::System.Uri", "uri").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var assemblyName = global::System.Reflection.Assembly.GetCallingAssembly().GetName().Name ?? throw new global::System.NullReferenceException(\"Unable to determine the calling assembly. Please use a fully qualified uri.\");");
			w.AppendLine("var imageSource = MarkupImageSource.Load(uri, assemblyName);");
			w.If("imageSource is not null").WithBody((ICodeWriter x) =>
			{
				x.AppendLine("return element." + info.PropertyName + "(imageSource);");
			}).EndIf();
			w.AppendLine("return element;");
		});
	}
	public void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
	{
		createBuilder().AddParameter("string", "source").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var assemblyName = global::System.Reflection.Assembly.GetCallingAssembly().GetName().Name ?? throw new global::System.NullReferenceException(\"Unable to determine the calling assembly. Please use a fully qualified uri.\");");
			w.AppendLine("var imageSource = MarkupImageSource.Load(source, assemblyName);");
			w.If("imageSource is not null").WithBody((ICodeWriter x) =>
			{
				x.AppendLine("return style.Setter(" + avaloniaProperty + ", imageSource);");
			}).EndIf();
			w.AppendLine("return style;");
		});
		createBuilder().AddParameter("global::System.Uri", "uri").WithBody((ICodeWriter w) =>
		{
			w.AppendLine("var assemblyName = global::System.Reflection.Assembly.GetCallingAssembly().GetName().Name ?? throw new global::System.NullReferenceException(\"Unable to determine the calling assembly. Please use a fully qualified uri.\");");
			w.AppendLine("var imageSource = MarkupImageSource.Load(uri, assemblyName);");
			w.If("imageSource is not null").WithBody((ICodeWriter x) =>
			{
				x.AppendLine("return style.Setter(" + avaloniaProperty + ", imageSource);");
			}).EndIf();
			w.AppendLine("return style;");
		});
	}

}
