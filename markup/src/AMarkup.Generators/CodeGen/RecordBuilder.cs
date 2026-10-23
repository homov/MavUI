using System.Collections.Generic;
using System.Linq;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public sealed class RecordBuilder : BuilderBase<RecordBuilder>
{
	private readonly List<RecordPropertyBuilder> _properties = new List<RecordPropertyBuilder>();

	private readonly List<string> _attributes = new List<string>();

	private DocumentationComment? _xmlDoc;

	public string Name { get; }

	public string FullyQualifiedName => Builder.Namespace + "." + Name;

	public CodeBuilder Builder { get; internal set; }

	public Accessibility? AccessModifier { get; private set; }

	public RecordPropertyType PropertyType { get; private set; }

	internal RecordBuilder(string name, CodeBuilder codeBuilder)
	{
		Name = name;
		Builder = codeBuilder;
	}

	public RecordBuilder WithSummary(string summary)
	{
		_xmlDoc = new SummaryDocumentationComment
		{
			Summary = summary
		};
		return this;
	}

	public RecordBuilder WithInheritDoc(bool inherit = true)
	{
		_xmlDoc = new InheritDocumentationComment();
		return this;
	}

	public RecordBuilder WithInheritDoc(string from)
	{
		_xmlDoc = new InheritDocumentationComment
		{
			InheritFrom = from
		};
		return this;
	}

	public RecordBuilder UsePositionalProperties()
	{
		PropertyType = RecordPropertyType.Positional;
		return this;
	}

	public RecordBuilder UseInitProperties()
	{
		PropertyType = RecordPropertyType.Init;
		return this;
	}

	public RecordPropertyBuilder AddProperty(string type, string name, Accessibility accessModifier = Accessibility.Public)
	{
		RecordPropertyBuilder recordPropertyBuilder = new RecordPropertyBuilder(type, name, accessModifier, this);
		_properties.Add(recordPropertyBuilder);
		return recordPropertyBuilder;
	}

	public RecordBuilder MakePublicRecord()
	{
		return WithAccessModifier(Accessibility.Public);
	}

	public RecordBuilder MakeInternalRecord()
	{
		return WithAccessModifier(Accessibility.Internal);
	}

	public RecordBuilder WithAccessModifier(Accessibility accessModifier)
	{
		AccessModifier = accessModifier;
		return this;
	}

	public string Build()
	{
		return Builder.Build();
	}

	internal override void Write(in CodeWriter writer)
	{
		_xmlDoc?.Write(in writer);
		foreach (string attribute in _attributes)
		{
			writer.AppendLine("[" + attribute + "]");
		}
		if (PropertyType == RecordPropertyType.Init)
		{
			using (writer.Block(AccessModifier.Code() + " record " + Name))
			{
				foreach (RecordPropertyBuilder property in _properties)
				{
					writer.AppendLine(property.ToInitProperty());
				}
				return;
			}
		}
		string text = (_properties.Any() ? string.Join(", ", _properties.Select((RecordPropertyBuilder x) => x.ToPositionalProperty())) : string.Empty);
		writer.AppendLine(AccessModifier.Code() + " record " + Name + "(" + text + ");");
	}

	public override RecordBuilder AddNamespaceImport(string importedNamespace)
	{
		Builder.AddNamespaceImport(importedNamespace);
		return this;
	}

	public override RecordBuilder AddNamespaceImport(ISymbol symbol)
	{
		Builder.AddNamespaceImport(symbol);
		return this;
	}

	public override RecordBuilder AddNamespaceImport(INamespaceSymbol symbol)
	{
		Builder.AddNamespaceImport(symbol);
		return this;
	}

	public override RecordBuilder AddAssemblyAttribute(string attribute)
	{
		Builder.AddAssemblyAttribute(attribute);
		return this;
	}
}
