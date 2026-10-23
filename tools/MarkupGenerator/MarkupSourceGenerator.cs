using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mav.MarkupGenerator;

/// <summary>
/// Writes a static <c>&lt;Type&gt;Markup</c> class with fluent setters for each public styled property and CLR event
/// that a type declares. Targets are the public <c>Mav*</c> types of the compiled assembly and a fixed list of
/// Avalonia base types, so inherited members are reached through the base types.
/// </summary>
[Generator]
public sealed class MarkupSourceGenerator : IIncrementalGenerator
{
    /// <summary>Avalonia base types whose own members get fluent setters (layout, input, templates, panels).</summary>
    private static readonly string[] AvaloniaBaseTypes =
    {
        "Avalonia.StyledElement",
        "Avalonia.Visual",
        "Avalonia.Input.InputElement",
        "Avalonia.Layout.Layoutable",
        "Avalonia.Controls.Control",
        "Avalonia.Controls.Primitives.TemplatedControl",
        "Avalonia.Controls.ContentControl",
        "Avalonia.Controls.Decorator",
        "Avalonia.Controls.Panel",
        "Avalonia.Controls.ItemsControl",
    };

    /// <summary>
    /// Names that the hand-written <c>MavMarkup</c> helpers or the collection APIs own. Members with these names are not
    /// generated, so the generated and the hand-written methods never share a name.
    /// </summary>
    private static readonly HashSet<string> ExcludedNames = new(StringComparer.Ordinal)
    {
        "Content", "Child", "Children", "Items", "Name", "Property", "Binding", "Classes", "Styles", "Resources",
    };

    private const string PropertySuffix = "Property";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Generate);
    }

    private static void Generate(SourceProductionContext context, Compilation compilation)
    {
        var avaloniaObject = compilation.GetTypeByMetadataName("Avalonia.AvaloniaObject");
        var styledProperty = compilation.GetTypeByMetadataName("Avalonia.StyledProperty`1");
        if (avaloniaObject is null || styledProperty is null)
            return;

        var targets = new List<INamedTypeSymbol>();
        CollectOwnTypes(compilation.Assembly.GlobalNamespace, targets, avaloniaObject);
        foreach (var name in AvaloniaBaseTypes)
        {
            var type = compilation.GetTypeByMetadataName(name);
            if (type is not null)
                targets.Add(type);
        }

        var classNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in targets.OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal))
        {
            var members = CollectMembers(type, styledProperty);
            if (members.Count == 0 || !classNames.Add(type.Name))
                continue;

            var text = MarkupEmitter.Emit(type, members);
            context.AddSource(type.Name + "Markup.g.cs", SourceText.From(text, Encoding.UTF8));
        }
    }

    private static void CollectOwnTypes(INamespaceSymbol ns, List<INamedTypeSymbol> result, INamedTypeSymbol avaloniaObject)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (IsTarget(type, avaloniaObject))
                result.Add(type);
        }

        foreach (var child in ns.GetNamespaceMembers())
            CollectOwnTypes(child, result, avaloniaObject);
    }

    private static bool IsTarget(INamedTypeSymbol type, INamedTypeSymbol avaloniaObject)
    {
        if (type.DeclaredAccessibility != Accessibility.Public || type.IsGenericType || type.IsStatic)
            return false;

        var ns = type.ContainingNamespace.ToDisplayString();
        var isMav = ns == "Mav" || ns.StartsWith("Mav.", StringComparison.Ordinal);
        var isMarkup = ns == "Mav.Markup" || ns.StartsWith("Mav.Markup.", StringComparison.Ordinal);
        if (!isMav || isMarkup)
            return false;

        for (var b = type.BaseType; b is not null; b = b.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(b, avaloniaObject))
                return true;
        }

        return false;
    }

    private static List<MarkupMember> CollectMembers(INamedTypeSymbol type, INamedTypeSymbol styledProperty)
    {
        // A member that a base type already declares keeps its method there; the subclass must not repeat it,
        // or two extension methods with the same name would apply to the same instance.
        var inherited = new HashSet<string>(StringComparer.Ordinal);
        for (var b = type.BaseType; b is not null; b = b.BaseType)
        {
            foreach (var member in b.GetMembers())
            {
                if (member is IFieldSymbol || member is IEventSymbol)
                    inherited.Add(member.Name);
            }
        }

        var result = new List<MarkupMember>();
        foreach (var member in type.GetMembers())
        {
            MarkupMember? generated = null;
            if (member is IFieldSymbol field)
                generated = PropertyOf(field, styledProperty, inherited);
            else if (member is IEventSymbol ev)
                generated = EventOf(ev, inherited);

            if (generated is not null)
                result.Add(generated);
        }

        return result;
    }

    private static MarkupMember? PropertyOf(IFieldSymbol field, INamedTypeSymbol styledProperty, HashSet<string> inherited)
    {
        if (!field.IsStatic || field.DeclaredAccessibility != Accessibility.Public)
            return null;

        if (field.Type is not INamedTypeSymbol fieldType || !fieldType.IsGenericType)
            return null;

        if (!SymbolEqualityComparer.Default.Equals(fieldType.OriginalDefinition, styledProperty))
            return null;

        if (field.Name.Length <= PropertySuffix.Length || !field.Name.EndsWith(PropertySuffix, StringComparison.Ordinal))
            return null;

        var name = field.Name.Substring(0, field.Name.Length - PropertySuffix.Length);
        if (ExcludedNames.Contains(name) || inherited.Contains(field.Name))
            return null;

        var valueType = fieldType.TypeArguments[0];
        if (!IsAccessible(valueType))
            return null;

        return new MarkupMember(name, field.Name, valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), isEvent: false);
    }

    private static MarkupMember? EventOf(IEventSymbol ev, HashSet<string> inherited)
    {
        if (ev.IsStatic || ev.DeclaredAccessibility != Accessibility.Public)
            return null;

        if (ExcludedNames.Contains(ev.Name) || inherited.Contains(ev.Name))
            return null;

        if (ev.Type is not INamedTypeSymbol handler || handler.Name != "EventHandler")
            return null;

        if (handler.ContainingNamespace.ToDisplayString() != "System")
            return null;

        string argsType;
        if (handler.TypeArguments.Length == 0)
        {
            argsType = "global::System.EventArgs";
        }
        else
        {
            var arg = handler.TypeArguments[0];
            if (!IsAccessible(arg))
                return null;
            argsType = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return new MarkupMember(ev.Name, string.Empty, argsType, isEvent: true);
    }

    /// <summary>True when generated code can name the type: public, with public containers and type arguments.</summary>
    private static bool IsAccessible(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return IsAccessible(array.ElementType);
            case ITypeParameterSymbol:
                return false;
            case INamedTypeSymbol named:
                if (named.DeclaredAccessibility != Accessibility.Public)
                    return false;
                for (var c = named.ContainingType; c is not null; c = c.ContainingType)
                {
                    if (c.DeclaredAccessibility != Accessibility.Public)
                        return false;
                }
                foreach (var arg in named.TypeArguments)
                {
                    if (!IsAccessible(arg))
                        return false;
                }
                return true;
            default:
                return true;
        }
    }
}
