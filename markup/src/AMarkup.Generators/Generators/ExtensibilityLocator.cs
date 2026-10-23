using System;
using System.Linq;

namespace AMarkup.Generators;

internal static class ExtensibilityLocator
{
	public static ITypeExtension[] Extensions { get; }

	static ExtensibilityLocator()
	{
		Extensions = (from x in typeof(ExtensibilityLocator).Assembly.GetTypes()
			where typeof(ITypeExtension).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract
			select (ITypeExtension)Activator.CreateInstance(x)).ToArray();
	}
}
