using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;

namespace AMarkup.Generators;

public static class StringExtensions
{
	public static string Camelcase(this string str)
	{
		if (str.Length <= 1)
		{
			return str.ToLowerInvariant();
		}
		return char.ToLower(str[0], CultureInfo.InvariantCulture) + str.Substring(1);
	}

	public static string EscapeIdentifier(this string identifier)
	{
		if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None)
		{
			return "@" + identifier;
		}
		return identifier;
	}
}
