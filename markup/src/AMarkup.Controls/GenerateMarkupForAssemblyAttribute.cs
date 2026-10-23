using System;

namespace AMarkup;

/// <summary>
/// Requests markup extension generation for the assembly that contains the specified type.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class GenerateMarkupForAssemblyAttribute : Attribute
{
    public GenerateMarkupForAssemblyAttribute(Type assemblyType)
    {
        AssemblyType = assemblyType ?? throw new ArgumentNullException(nameof(assemblyType));
    }

    /// <summary>
    /// Gets a type that belongs to the assembly to scan.
    /// </summary>
    public Type AssemblyType { get; }
}
