using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal interface ITypeExtension
{
	bool CanExtend(string qualifiedTypeName);

	void WriteAttachedPropertyBuilderExtensions(AttachedPropertyInfo prop, Func<MethodBuilder> createBuilder);

	void WritePropertyExtensions(ClassBuilder builder, AvaloniaPropertyExtensionInfo info, Func<MethodBuilder> createBuilder);
	void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
	{
	}

}
