using System;
using System.Collections.Generic;
using System.Linq;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public class MethodBuilder : BuilderBase<MethodBuilder>, IBuilder, IParameterized<MethodBuilder>
{
	private readonly List<string> _attributes = new List<string>();

	private readonly GenericCollection _generics = new GenericCollection();

	private DocumentationComment? _xmlDoc;

	private readonly List<ParameterBuilder<MethodBuilder>> _parameters = new List<ParameterBuilder<MethodBuilder>>();

	private bool _override;

	private bool _virtual;

	private Action<ICodeWriter>? _methodBodyWriter;

	List<ParameterBuilder<MethodBuilder>> IParameterized<MethodBuilder>.Parameters => _parameters;

	MethodBuilder IParameterized<MethodBuilder>.Parent => this;

	public IReadOnlyCollection<ParameterBuilder<MethodBuilder>> Parameters => _parameters;

	public string Name { get; }

	public string? ReturnType { get; private set; }

	public bool IsAsync { get; private set; }

	public bool IsAbstract { get; private set; }

	public bool HasBody
	{
		get
		{
			if (_methodBodyWriter == null)
			{
				return false;
			}
			CodeWriter codeWriter = new CodeWriter(IndentStyle.Spaces);
			_methodBodyWriter(codeWriter);
			return !string.IsNullOrEmpty(codeWriter.ToString());
		}
	}

	public ClassBuilder Class { get; }

	public Accessibility? AccessModifier { get; private set; }

	public bool IsStatic { get; private set; }

	internal MethodBuilder(string name, Accessibility? accessModifier, ClassBuilder builder)
	{
		Name = name;
		AccessModifier = accessModifier;
		Class = builder;
	}

	public MethodBuilder WithSummary(string summary)
	{
		if (_xmlDoc == null || !(_xmlDoc is SummaryDocumentationComment summaryDocumentationComment))
		{
			_xmlDoc = new ParameterDocumentationComment
			{
				Summary = summary
			};
		}
		else
		{
			summaryDocumentationComment.Summary = summary;
		}
		return this;
	}

	public MethodBuilder WithInheritDoc(bool inherit = true)
	{
		_xmlDoc = new InheritDocumentationComment();
		return this;
	}

	public MethodBuilder WithInheritDoc(string from)
	{
		_xmlDoc = new InheritDocumentationComment
		{
			InheritFrom = from
		};
		return this;
	}

	public MethodBuilder WithParameterDoc(string paramName, string documentation)
	{
		if (_xmlDoc == null)
		{
			_xmlDoc = new ParameterDocumentationComment();
		}
		((_xmlDoc as ParameterDocumentationComment) ?? throw new Exception("DocumentationComment has already been initialized with a non ParameterDocumentationComment")).AddParameter(paramName, documentation);
		return this;
	}

	public MethodBuilder AddGeneric(string name)
	{
		return AddGeneric(name, (GenericBuilder _) =>
		{
		});
	}

	public MethodBuilder AddGeneric(string name, Action<GenericBuilder> configureBuilder)
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

	public override MethodBuilder AddNamespaceImport(string importedNamespace)
	{
		Class.AddNamespaceImport(importedNamespace);
		return this;
	}

	public override MethodBuilder AddNamespaceImport(ISymbol symbol)
	{
		return AddNamespaceImport(symbol.ContainingNamespace);
	}

	public override MethodBuilder AddNamespaceImport(INamespaceSymbol symbol)
	{
		return AddNamespaceImport(symbol.ToString());
	}

	public override MethodBuilder AddAssemblyAttribute(string attribute)
	{
		Class.AddAssemblyAttribute(attribute);
		return this;
	}

	public PropertyBuilder AddProperty(string name, Accessibility? accessModifier = null)
	{
		return Class.AddProperty(name, accessModifier);
	}

	public MethodBuilder MakeAsync()
	{
		IsAsync = true;
		return this;
	}

	public MethodBuilder Abstract(bool isAbstract = true)
	{
		IsAbstract = isAbstract;
		return this;
	}

	public MethodBuilder MakePublicMethod()
	{
		return WithAccessModifier(Accessibility.Public);
	}

	public MethodBuilder MakePrivateMethod()
	{
		return WithAccessModifier(Accessibility.Private);
	}

	public MethodBuilder MakeProtectedMethod()
	{
		return WithAccessModifier(Accessibility.Protected);
	}

	public MethodBuilder MakeInternalMethod()
	{
		return WithAccessModifier(Accessibility.Internal);
	}

	public MethodBuilder WithAccessModifier(Accessibility accessModifier)
	{
		AccessModifier = accessModifier;
		return this;
	}

	public MethodBuilder Override(bool @override = true)
	{
		_override = @override;
		return this;
	}

	public MethodBuilder MakeStaticMethod()
	{
		IsStatic = true;
		return this;
	}

	public MethodBuilder MakeVirtualMethod()
	{
		_virtual = true;
		return this;
	}

	public MethodBuilder AddAttribute(string attribute)
	{
		string item = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
		if (!_attributes.Contains(item))
		{
			_attributes.Add(item);
		}
		return this;
	}

	public MethodBuilder WithBody(Action<ICodeWriter> writerDelegate)
	{
		_methodBodyWriter = writerDelegate;
		return this;
	}

	public MethodBuilder WithReturnType(string returnType)
	{
		ReturnType = returnType;
		return this;
	}

	internal override void Write(in CodeWriter writer)
	{
		string text = ((ReturnType == null || string.IsNullOrEmpty(ReturnType)) ? "void" : ReturnType.Trim());
		if (IsAsync)
		{
			text = "async " + text;
		}
		if (_override)
		{
			text = "override " + text;
		}
		else if (_virtual)
		{
			text = "virtual " + text;
		}
		else if (IsAbstract)
		{
			text = "abstract " + text;
		}
		else if (IsStatic)
		{
			text = "static " + text;
		}
		string text2 = string.Join(", ", _parameters.Select((ParameterBuilder<MethodBuilder> x) => x.ToString()));
		text = $"{AccessModifier.Code()} {text} {Name}{_generics}({text2})";
		if (_xmlDoc is ParameterDocumentationComment parameterDocumentationComment)
		{
			parameterDocumentationComment.RemoveUnusedParameters(_parameters);
		}
		_xmlDoc?.Write(in writer);
		foreach (string attribute in _attributes)
		{
			writer.AppendLine("[" + attribute + "]");
		}
		if (IsAbstract)
		{
			writer.AppendLine(text.Trim() + ";");
			return;
		}
		using (writer.Block(text.Trim(), _generics.Contraints()))
		{
			_methodBodyWriter?.Invoke(writer);
		}
	}
}
