using System;
using AMarkup.Generators.CodeGen;

namespace AMarkup.Generators;

internal sealed class ResourceDictionaryTypeExtension : ITypeExtension
{
    public bool CanExtend(string qualifiedTypeName) =>
        qualifiedTypeName == "global::Avalonia.Controls.ResourceDictionary";

    public void WriteAttachedPropertyBuilderExtensions(
        AttachedPropertyInfo prop,
        Func<MethodBuilder> createBuilder)
    {
    }

    public void WritePropertyExtensions(
        ClassBuilder builder,
        AvaloniaPropertyExtensionInfo info,
        Func<MethodBuilder> createBuilder)
    {
        createBuilder()
            .AddParameter("Action<global::Avalonia.Controls.ResourceDictionary>", "configureResources")
            .WithBody((ICodeWriter w) =>
            {
                w.AppendLine("element." + info.PropertyName + " ??= new global::Avalonia.Controls.ResourceDictionary();");
                w.AppendLine("configureResources(element." + info.PropertyName + ");");
                w.AppendLine("return element;");
            });
    }
    public void WriteStyleExtensions(string avaloniaProperty, Func<MethodBuilder> createBuilder)
    {
        createBuilder()
            .AddParameter("Action<global::Avalonia.Controls.ResourceDictionary>", "configureResources")
            .WithBody((ICodeWriter w) =>
            {
                w.AppendLine("var resources = new global::Avalonia.Controls.ResourceDictionary();");
                w.AppendLine("configureResources(resources);");
                w.AppendLine("return style.Setter(" + avaloniaProperty + ", resources);");
            });
    }

}
