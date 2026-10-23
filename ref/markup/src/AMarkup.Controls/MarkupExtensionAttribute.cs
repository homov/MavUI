using System;

namespace AMarkup;

/// <summary>
/// Compatibility marker for generated fluent markup methods.
/// New generated code uses <c>GeneratedCodeAttribute</c> instead.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MarkupExtensionAttribute : Attribute
{
}
