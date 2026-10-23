using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

internal static class AccessibilityExtensions
{
	public static string? Code(this Accessibility accessModifier)
	{
		return accessModifier switch
		{
			Accessibility.ProtectedAndInternal => "private protected", 
			Accessibility.ProtectedOrInternal => "protected internal", 
			Accessibility.NotApplicable => null, 
			_ => accessModifier.ToString().ToLowerInvariant(), 
		};
	}

	public static string? Code(this Accessibility? accessModifier)
	{
		if (!accessModifier.HasValue)
		{
			return null;
		}
		return accessModifier.Value.Code();
	}

	public static string? Code(this Accessibility? accessModifier, Accessibility defaultValue)
	{
		if (!accessModifier.HasValue)
		{
			return defaultValue.Code();
		}
		return accessModifier.Value.Code();
	}
}
