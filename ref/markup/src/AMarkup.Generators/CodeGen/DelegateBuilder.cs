using System;
using System.Collections.Generic;
using System.Linq;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public sealed class DelegateBuilder : BuilderBase<DelegateBuilder>, IParameterized<DelegateBuilder>
{
	private ParameterDocumentationComment? _xmlDoc;

	private readonly List<string> _attributes = new List<string>();

	private readonly GenericCollection _generics = new GenericCollection();

	private readonly List<ParameterBuilder<DelegateBuilder>> _parameters = new List<ParameterBuilder<DelegateBuilder>>();

	DelegateBuilder IParameterized<DelegateBuilder>.Parent => this;

	List<ParameterBuilder<DelegateBuilder>> IParameterized<DelegateBuilder>.Parameters => _parameters;

	public string Name { get; }

	public CodeBuilder Builder { get; internal set; }

	public Accessibility? AccessModifier { get; private set; }

	public string? ReturnType { get; private set; }

	internal DelegateBuilder(string name, CodeBuilder builder)
	{
		Name = name;
		Builder = builder;
	}

	public DelegateBuilder WithSummary(string summary)
	{
		if (_xmlDoc != null)
		{
			SummaryDocumentationComment xmlDoc = _xmlDoc;
			if (xmlDoc != null)
			{
				xmlDoc.Summary = summary;
				goto IL_002d;
			}
		}
		_xmlDoc = new ParameterDocumentationComment
		{
			Summary = summary
		};
		goto IL_002d;
		IL_002d:
		return this;
	}

	public DelegateBuilder WithParameterDoc(string paramName, string documentation)
	{
		if (_xmlDoc == null)
		{
			_xmlDoc = new ParameterDocumentationComment();
		}
		(_xmlDoc ?? throw new Exception("DocumentationComment has already been initialized with a non ParameterDocumentationComment")).AddParameter(paramName, documentation);
		return this;
	}

	public DelegateBuilder AddGeneric(string name)
	{
		return AddGeneric(name, (GenericBuilder _) =>
		{
		});
	}

	public DelegateBuilder AddGeneric(string name, Action<GenericBuilder> configureBuilder)
	{
		if (configureBuilder == null)
		{
			throw new ArgumentNullException("configureBuilder");
		}
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		if (_generics.Any((GenericBuilder x) => x.Name == name))
		{
			throw new ArgumentException("The argument " + name + " already exists");
		}
		GenericBuilder genericBuilder = new GenericBuilder(name);
		configureBuilder(genericBuilder);
		_generics.Add(genericBuilder);
		return this;
	}

	public DelegateBuilder MakePublicDelegate()
	{
		return WithAccessModifier(Accessibility.Public);
	}

	public DelegateBuilder MakeInternalDelegate()
	{
		return WithAccessModifier(Accessibility.Internal);
	}

	public DelegateBuilder WithAccessModifier(Accessibility accessModifier)
	{
		AccessModifier = accessModifier;
		return this;
	}

	public DelegateBuilder AddAttribute(string attribute)
	{
		string item = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
		if (!_attributes.Contains(item))
		{
			_attributes.Add(item);
		}
		return this;
	}

	public override DelegateBuilder AddAssemblyAttribute(string attribute)
	{
		Builder.AddAssemblyAttribute(attribute);
		return this;
	}

	public override DelegateBuilder AddNamespaceImport(string importedNamespace)
	{
		Builder.AddNamespaceImport(importedNamespace);
		return this;
	}

	public override DelegateBuilder AddNamespaceImport(ISymbol symbol)
	{
		Builder.AddNamespaceImport(symbol);
		return this;
	}

	public override DelegateBuilder AddNamespaceImport(INamespaceSymbol symbol)
	{
		Builder.AddNamespaceImport(symbol);
		return this;
	}

	public DelegateBuilder WithReturnType(string returnType)
	{
		ReturnType = returnType;
		return this;
	}

	internal override void Write(in CodeWriter writer)
	{
		_xmlDoc?.RemoveUnusedParameters(_parameters);
		_xmlDoc?.Write(in writer);
		foreach (string attribute in _attributes)
		{
			writer.AppendLine("[" + attribute + "]");
		}
		string arg = (_parameters.Any() ? string.Join(", ", _parameters.Select((ParameterBuilder<DelegateBuilder> x) => x.ToString())) : string.Empty);
		IEnumerable<string> values = new string[4]
		{
			AccessModifier.Code(),
			"delegate",
			(ReturnType == null || string.IsNullOrEmpty(ReturnType)) ? "void" : ReturnType.Trim(),
			$"{Name}{_generics}({arg})"
		}.Where((string x) => !string.IsNullOrEmpty(x));
		string text = string.Join(" ", values);
		string[] array = _generics.Contraints();
		if (array.Any())
		{
			writer.AppendLine(text);
			writer.IncreaseIndent();
			for (int num = 0; num < array.Length; num++)
			{
				string text2 = array[num];
				if (num + 1 == array.Length)
				{
					text2 += ";";
				}
				writer.AppendLine(text2);
			}
			writer.DecreaseIndent();
		}
		else
		{
			writer.AppendLine(text + ";");
		}
	}
}
