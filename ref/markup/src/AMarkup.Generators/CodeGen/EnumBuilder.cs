using System.Collections.Generic;
using System.Linq;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public class EnumBuilder : IBuilder
{
	private readonly List<string> _attributes = new List<string>();

	private readonly List<EnumValueBuilder> _values = new List<EnumValueBuilder>();

	private DocumentationComment? _xmlDoc;

	public CodeBuilder Builder { get; internal set; }

	public string Name { get; }

	public string FullyQualifiedName => Builder.Namespace + "." + Name;

	public Accessibility? AccessModifier { get; private set; }

	internal EnumBuilder(string name, CodeBuilder builder)
	{
		Name = name;
		Builder = builder;
	}

	public EnumBuilder WithSummary(string summary)
	{
		_xmlDoc = new SummaryDocumentationComment
		{
			Summary = summary
		};
		return this;
	}

	public EnumBuilder WithInheritDoc(bool inherit = true)
	{
		_xmlDoc = new InheritDocumentationComment();
		return this;
	}

	public EnumBuilder WithInheritDoc(string from)
	{
		_xmlDoc = new InheritDocumentationComment
		{
			InheritFrom = from
		};
		return this;
	}

	public EnumValueBuilder AddValue(string name, int? numericValue = null)
	{
		EnumValueBuilder enumValueBuilder = new EnumValueBuilder(name, this, numericValue);
		_values.Add(enumValueBuilder);
		return enumValueBuilder;
	}

	public EnumBuilder AddNamespaceImport(string importedNamespace)
	{
		Builder.AddNamespaceImport(importedNamespace);
		return this;
	}

	public EnumBuilder AddNamespaceImport(ISymbol symbol)
	{
		Builder.AddNamespaceImport(symbol);
		return this;
	}

	public EnumBuilder AddNamespaceImport(INamespaceSymbol symbol)
	{
		Builder.AddNamespaceImport(symbol);
		return this;
	}

	public EnumBuilder AddAttribute(string attribute)
	{
		string item = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
		if (!_attributes.Contains(item))
		{
			_attributes.Add(item);
		}
		return this;
	}

	public EnumBuilder MakePublicEnum()
	{
		return WithAccessModifier(Accessibility.Public);
	}

	public EnumBuilder MakeInternalEnum()
	{
		return WithAccessModifier(Accessibility.Internal);
	}

	public EnumBuilder WithAccessModifier(Accessibility accessModifier)
	{
		AccessModifier = accessModifier;
		return this;
	}

	public string Build()
	{
		return Builder.Build();
	}

	void IBuilder.Write(in CodeWriter writer)
	{
		_xmlDoc?.Write(in writer);
		Queue<IBuilder> queue = new Queue<IBuilder>();
		_values.OrderBy((EnumValueBuilder x) => x.Value).ToList().ForEach((EnumValueBuilder x) =>
		{
			queue.Enqueue(x);
		});
		foreach (string item in _attributes.OrderBy((string x) => x))
		{
			writer.AppendLine("[" + item + "]");
		}
		string[] source = new string[3]
		{
			AccessModifier.Code(),
			"enum",
			Name
		};
		using (writer.Block(string.Join(" ", source.Where((string x) => !string.IsNullOrEmpty(x)))))
		{
			while (queue.Any())
			{
				queue.Dequeue().Write(in writer);
				if (queue.Any())
				{
					writer.AppendUnindentedLine(",");
					writer.NewLine();
				}
				else
				{
					writer.NewLine();
				}
			}
		}
	}
}
