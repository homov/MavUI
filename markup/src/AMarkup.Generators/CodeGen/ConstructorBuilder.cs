using System;
using System.Collections.Generic;
using System.Linq;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public sealed class ConstructorBuilder : BuilderBase<ConstructorBuilder>, IParameterized<ConstructorBuilder>
{
	private readonly List<ParameterBuilder<ConstructorBuilder>> _parameters = new List<ParameterBuilder<ConstructorBuilder>>();

	private readonly List<string> _attributes = new List<string>();

	private DocumentationComment? _xmlDoc;

	private Action<ICodeWriter>? _methodBodyWriter;

	private Func<string> _baseCall;

	List<ParameterBuilder<ConstructorBuilder>> IParameterized<ConstructorBuilder>.Parameters => _parameters;

	ConstructorBuilder IParameterized<ConstructorBuilder>.Parent => this;

	public IReadOnlyCollection<ParameterBuilder<ConstructorBuilder>> Parameters => _parameters;

	public Accessibility? AccessModifier { get; }

	public ClassBuilder Class { get; }

	internal int Count => _parameters.Count;

	internal ConstructorBuilder(Accessibility? accessModifier, ClassBuilder classBuilder)
	{
		AccessModifier = accessModifier;
		Class = classBuilder;
		_baseCall = () => string.Empty;
	}

	public ConstructorBuilder WithSummary(string summary)
	{
		if (_xmlDoc != null && _xmlDoc is SummaryDocumentationComment summaryDocumentationComment)
		{
			summaryDocumentationComment.Summary = summary;
		}
		else
		{
			_xmlDoc = new ParameterDocumentationComment
			{
				Summary = summary
			};
		}
		return this;
	}

	public ConstructorBuilder WithInheritDoc(bool inherit = true)
	{
		_xmlDoc = new InheritDocumentationComment();
		return this;
	}

	public ConstructorBuilder WithInheritDoc(string from)
	{
		_xmlDoc = new InheritDocumentationComment
		{
			InheritFrom = from
		};
		return this;
	}

	public ConstructorBuilder WithParameterDoc(string paramName, string documentation)
	{
		if (_xmlDoc != null && !(_xmlDoc is ParameterDocumentationComment))
		{
			throw new Exception("Documentation Comment has already been initialized using an InheritDoc.");
		}
		if (_xmlDoc is ParameterDocumentationComment parameterDocumentationComment)
		{
			parameterDocumentationComment.AddParameter(paramName, documentation);
		}
		return this;
	}

	public override ConstructorBuilder AddAssemblyAttribute(string attribute)
	{
		Class.AddAssemblyAttribute(attribute);
		return this;
	}

	public ConstructorBuilder AddAttribute(string attribute)
	{
		string item = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
		if (!_attributes.Contains(item))
		{
			_attributes.Add(item);
		}
		return this;
	}

	public ConstructorBuilder WithBody(Action<ICodeWriter> writerDelegate)
	{
		_methodBodyWriter = writerDelegate;
		return this;
	}

	public ConstructorBuilder WithThisCall()
	{
		_baseCall = () => ": this()";
		return this;
	}

	public ConstructorBuilder WithThisCall(Dictionary<string, string> parameters)
	{
		foreach (KeyValuePair<string, string> parameter in parameters)
		{
			this.AddParameter(parameter.Key, parameter.Value);
		}
		_baseCall = () =>
		{
			IEnumerable<string> values = parameters.Select((KeyValuePair<string, string> x) => x.Value);
			return ": this(" + string.Join(", ", values) + ")";
		};
		return this;
	}

	public ConstructorBuilder WithThisCall(IEnumerable<IParameterSymbol> parameters)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (IParameterSymbol item in parameters ?? Array.Empty<IParameterSymbol>())
		{
			AddNamespaceImport(item.Type);
			dictionary.Add(item.Type.Name, item.Name);
		}
		return WithThisCall(dictionary);
	}

	public ConstructorBuilder WithThisCall(IMethodSymbol baseConstructor)
	{
		return WithThisCall(baseConstructor.Parameters);
	}

	public ConstructorBuilder WithBaseCall()
	{
		_baseCall = () => ": base()";
		return this;
	}

	public ConstructorBuilder WithBaseCall(Dictionary<string, string> parameters)
	{
		foreach (KeyValuePair<string, string> parameter in parameters)
		{
			this.AddParameter(parameter.Key, parameter.Value);
		}
		_baseCall = () =>
		{
			IEnumerable<string> values = parameters.Select((KeyValuePair<string, string> x) => x.Value);
			return ": base(" + string.Join(", ", values) + ")";
		};
		return this;
	}

	public ConstructorBuilder WithBaseCall(IEnumerable<IParameterSymbol> parameters)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (IParameterSymbol item in parameters ?? Array.Empty<IParameterSymbol>())
		{
			AddNamespaceImport(item.Type);
			dictionary.Add(item.Type.Name, item.Name);
		}
		return WithBaseCall(dictionary);
	}

	public ConstructorBuilder WithBaseCall(IMethodSymbol baseConstructor)
	{
		return WithBaseCall(baseConstructor.Parameters);
	}

	public override ConstructorBuilder AddNamespaceImport(string namespaceImport)
	{
		Class.AddNamespaceImport(namespaceImport);
		return this;
	}

	public override ConstructorBuilder AddNamespaceImport(ISymbol symbol)
	{
		return AddNamespaceImport(symbol.ContainingNamespace);
	}

	public override ConstructorBuilder AddNamespaceImport(INamespaceSymbol symbol)
	{
		return AddNamespaceImport(symbol.ToString());
	}

	public ConstructorBuilder AddConstructor(Accessibility? accessModifier = null)
	{
		return Class.AddConstructor(accessModifier);
	}

	internal override void Write(in CodeWriter writer)
	{
		if (_xmlDoc is ParameterDocumentationComment parameterDocumentationComment)
		{
			parameterDocumentationComment.RemoveUnusedParameters(_parameters);
		}
		_xmlDoc?.Write(in writer);
		foreach (string attribute in _attributes)
		{
			writer.AppendLine("[" + attribute + "]");
		}
		string text = (AccessModifier.HasValue ? AccessModifier.Code() : Class.AccessModifier.ToString().ToLowerInvariant());
		string text2 = text;
		string text3 = (_parameters.Any() ? string.Join(", ", _parameters.Select((ParameterBuilder<ConstructorBuilder> x) => x.ToString())) : string.Empty);
		using (writer.Block(text2 + " " + Class.Name + "(" + text3 + ")", _baseCall()))
		{
			_methodBodyWriter?.Invoke(writer);
		}
	}
}
