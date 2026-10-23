using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public abstract class BuilderBase : IBuilder
{
	protected readonly List<string> _pragmaWarnings = new List<string>();

	internal abstract void Write(in CodeWriter writer);

	void IBuilder.Write(in CodeWriter writer)
	{
		string text = string.Join(", ", _pragmaWarnings.Distinct());
		if (_pragmaWarnings.Any())
		{
			writer.AppendUnindentedLine("#pragma warning disable " + text);
		}
		Write(in writer);
		if (_pragmaWarnings.Any())
		{
			writer.AppendUnindentedLine("#pragma warning restore " + text);
		}
	}
}
public abstract class BuilderBase<T> : BuilderBase where T : BuilderBase
{
	public abstract T AddNamespaceImport(string importedNamespace);

	public abstract T AddNamespaceImport(ISymbol symbol);

	public abstract T AddNamespaceImport(INamespaceSymbol symbol);

	public abstract T AddAssemblyAttribute(string attribute);

	public T DisableWarning(string buildCode)
	{
		_pragmaWarnings.Add(buildCode);
		if (this is T result)
		{
			return result;
		}
		throw new InvalidOperationException("The Builder must be of type " + typeof(T).FullName);
	}
}
