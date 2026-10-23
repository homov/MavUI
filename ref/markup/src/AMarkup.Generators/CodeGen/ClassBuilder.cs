using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public sealed class ClassBuilder : BuilderBase<ClassBuilder>
{
	private readonly List<string> _attributes = new List<string>();

	private readonly List<string> _interfaces = new List<string>();

	private readonly List<string> _classAttributes = new List<string>();

	private readonly List<ConstructorBuilder> _constructors = new List<ConstructorBuilder>();

	private readonly List<EventBuilder> _events = new List<EventBuilder>();

	private readonly List<PropertyBuilder> _properties = new List<PropertyBuilder>();

	private readonly List<MethodBuilder> _methods = new List<MethodBuilder>();

	private readonly Queue<ClassBuilder> _nestedClass = new Queue<ClassBuilder>();

	private readonly Queue<DelegateBuilder> _nestedDelegates = new Queue<DelegateBuilder>();

	private readonly GenericCollection _generics = new GenericCollection();

	private readonly bool _isPartial;

	private DocumentationComment? _xmlDoc;

	private Func<PropertyBuilder, string>? _propertiesOrderBy;

	public string Name { get; }

	public string FullyQualifiedName => Builder.Namespace + "." + Name;

	public IReadOnlyList<ConstructorBuilder> Constructors => _constructors;

	public IReadOnlyList<PropertyBuilder> Properties => _properties;

	public IReadOnlyList<MethodBuilder> Methods => _methods;

	public IReadOnlyList<ClassBuilder> NestedClasses => _nestedClass.ToList();

	public CodeBuilder Builder { get; internal set; }

	public string? BaseClass { get; private set; }

	public Accessibility? AccessModifier { get; private set; }

	public TypeKind Kind { get; private set; } = TypeKind.Class;

	public bool IsStatic { get; private set; }

	public bool IsAbstract { get; private set; }

	public bool IsSealed { get; private set; }

	internal ClassBuilder(string className, CodeBuilder codeBuilder, bool partial = true)
	{
		Name = className;
		Builder = codeBuilder;
		_isPartial = partial;
	}

	public ClassBuilder WithSummary(string summary)
	{
		_xmlDoc = new SummaryDocumentationComment
		{
			Summary = summary
		};
		return this;
	}

	public ClassBuilder WithInheritDoc(bool inherit = true)
	{
		_xmlDoc = new InheritDocumentationComment();
		return this;
	}

	public ClassBuilder WithInheritDoc(string from)
	{
		_xmlDoc = new InheritDocumentationComment
		{
			InheritFrom = from
		};
		return this;
	}

	public ClassBuilder Sealed()
	{
		IsSealed = true;
		return this;
	}

	public ClassBuilder IsStruct()
	{
		Kind = TypeKind.Struct;
		return this;
	}

	public ClassBuilder OfType(TypeKind kind)
	{
		Kind = kind;
		return this;
	}

	public ClassBuilder SetBaseClass(string baseClass)
	{
		BaseClass = baseClass;
		if (BaseClass.Contains("."))
		{
			string text = baseClass.Split(new char[1] { '.' }).Last();
			AddNamespaceImport(Regex.Replace(baseClass, "\\." + text, string.Empty));
		}
		return this;
	}

	public ClassBuilder SetBaseClass(INamedTypeSymbol symbol)
	{
		if (symbol.Name == Name)
		{
			BaseClass = "global::" + symbol.GetFullMetadataName();
			return this;
		}
		BaseClass = symbol.Name;
		return AddNamespaceImport(symbol.ContainingNamespace);
	}

	public ClassBuilder AddGeneric(string name)
	{
		return AddGeneric(name, (GenericBuilder _) =>
		{
		});
	}

	public ClassBuilder AddGeneric(string name, Action<GenericBuilder> configureBuilder)
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

	public override ClassBuilder AddAssemblyAttribute(string attribute)
	{
		Builder.AddAssemblyAttribute(attribute);
		return this;
	}

	public ClassBuilder AddAttribute(string attribute)
	{
		string item = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
		if (!_attributes.Contains(item))
		{
			_attributes.Add(item);
		}
		return this;
	}

	public override ClassBuilder AddNamespaceImport(string importedNamespace)
	{
		Builder.AddNamespaceImport(importedNamespace);
		return this;
	}

	public override ClassBuilder AddNamespaceImport(ISymbol symbol)
	{
		Builder.AddNamespaceImport(symbol);
		return this;
	}

	public override ClassBuilder AddNamespaceImport(INamespaceSymbol symbol)
	{
		Builder.AddNamespaceImport(symbol);
		return this;
	}

	public ClassBuilder AddInterface(string interfaceName)
	{
		_interfaces.Add(interfaceName);
		if (Enumerable.Contains(interfaceName, '.'))
		{
			string text = interfaceName.Split(new char[1] { '.' }).Last();
			return AddNamespaceImport(Regex.Replace(interfaceName, "\\." + text + "$", string.Empty));
		}
		return this;
	}

	public ClassBuilder AddInterface(ITypeSymbol symbol)
	{
		_interfaces.Add(symbol.Name);
		return AddNamespaceImport(symbol);
	}

	public ClassBuilder AddInterfaces(IEnumerable<string> interfaces)
	{
		if (interfaces == null)
		{
			return this;
		}
		foreach (string @interface in interfaces)
		{
			AddInterface(@interface);
		}
		return this;
	}

	public ClassBuilder AddInterfaces(IEnumerable<INamedTypeSymbol> interfaces)
	{
		if (interfaces == null)
		{
			return this;
		}
		foreach (INamedTypeSymbol @interface in interfaces)
		{
			AddInterface(@interface);
		}
		return this;
	}

	public ConstructorBuilder AddConstructor(Accessibility? accessModifier = null)
	{
		ConstructorBuilder constructorBuilder = new ConstructorBuilder(accessModifier, this);
		_constructors.Add(constructorBuilder);
		return constructorBuilder;
	}

	public ConstructorBuilder AddConstructor(IMethodSymbol baseConstructor, Accessibility? accessModifier = null)
	{
		ConstructorBuilder constructorBuilder = AddConstructor(accessModifier).AddParameters(baseConstructor.Parameters);
		_constructors.Add(constructorBuilder);
		return constructorBuilder;
	}

	public MethodBuilder AddMethod(string name, Accessibility? accessModifier = null)
	{
		MethodBuilder methodBuilder = new MethodBuilder(name, accessModifier, this);
		_methods.Add(methodBuilder);
		return methodBuilder;
	}

	public PropertyBuilder AddProperty(string name, Accessibility? accessModifier = null)
	{
		PropertyBuilder propertyBuilder = new PropertyBuilder(name, accessModifier, this);
		_properties.Add(propertyBuilder);
		return propertyBuilder;
	}

	public EventBuilder AddEvent(string eventName)
	{
		EventBuilder eventBuilder = new EventBuilder(this, eventName);
		_events.Add(eventBuilder);
		return eventBuilder;
	}

	public ClassBuilder MakePublicClass()
	{
		return WithAccessModifier(Accessibility.Public);
	}

	public ClassBuilder MakeInternalClass()
	{
		return WithAccessModifier(Accessibility.Internal);
	}

	public ClassBuilder WithAccessModifier(Accessibility accessModifier)
	{
		AccessModifier = accessModifier;
		return this;
	}

	public ClassBuilder MakeStaticClass()
	{
		IsStatic = true;
		return this;
	}

	public ClassBuilder Abstract(bool isAbstract = true)
	{
		IsAbstract = isAbstract;
		return this;
	}

	public ClassBuilder AddNestedClass(string name, Accessibility? accessModifier = null)
	{
		return AddNestedClass(name, partial: false, accessModifier);
	}

	public ClassBuilder AddNestedClass(string name, bool partial, Accessibility? accessModifier = null)
	{
		ClassBuilder classBuilder = new ClassBuilder(name, Builder, partial);
		if (accessModifier.HasValue)
		{
			classBuilder.WithAccessModifier(accessModifier.Value);
		}
		_nestedClass.Enqueue(classBuilder);
		return classBuilder;
	}

	public DelegateBuilder AddNestedDelegate(string name, Accessibility? accessModifier = null)
	{
		DelegateBuilder delegateBuilder = new DelegateBuilder(name, Builder);
		if (accessModifier.HasValue)
		{
			delegateBuilder.WithAccessModifier(accessModifier.Value);
		}
		_nestedDelegates.Enqueue(delegateBuilder);
		return delegateBuilder;
	}

	public string Build()
	{
		return Builder.Build();
	}

	public ClassBuilder DontSortPropertiesByName()
	{
		_propertiesOrderBy = (PropertyBuilder _) => "";
		return this;
	}

	internal override void Write(in CodeWriter writer)
	{
		_xmlDoc?.Write(in writer);
		WriteClassAttributes(_classAttributes, in writer);
		if (!IsStatic)
		{
			_ = string.Empty;
		}
		Queue<string> queue = new Queue<string>();
		if (!string.IsNullOrEmpty(BaseClass))
		{
			queue.Enqueue(BaseClass);
		}
		foreach (string item in from x in _interfaces.Where((string x) => !string.IsNullOrEmpty(x)).Distinct()
			orderby x
			select x)
		{
			queue.Enqueue(item);
		}
		string text = (queue.Any() ? (": " + string.Join(", ", queue)) : string.Empty);
		foreach (string attribute in _attributes)
		{
			writer.AppendLine("[" + attribute + "]");
		}
		if (_methods.Any((MethodBuilder x) => x.IsAbstract))
		{
			IsAbstract = true;
		}
		string[] source = new string[8]
		{
			AccessModifier.Code(),
			IsStatic ? "static" : null,
			IsSealed ? "sealed" : null,
			IsAbstract ? "abstract" : null,
			_isPartial ? "partial" : null,
			Kind.ToString().ToLowerInvariant(),
			$"{Name}{_generics}",
			text
		};
		using (writer.Block(string.Join(" ", source.Where((string x) => !string.IsNullOrEmpty(x))), _generics.Contraints()))
		{
			bool hadOutput = false;
			hadOutput = InvokeBuilderWrite(_nestedDelegates, ref hadOutput, in writer);
			hadOutput = InvokeBuilderWrite(_events, ref hadOutput, in writer);
			Func<PropertyBuilder, string> keySelector = _propertiesOrderBy ?? ((Func<PropertyBuilder, string>)((PropertyBuilder x) => x.Name));
			hadOutput = InvokeBuilderWrite(_properties.Where((PropertyBuilder x) => x.FieldTypeValue == PropertyBuilder.FieldType.Const && !x.IsStatic).OrderBy(keySelector), ref hadOutput, in writer, group: true);
			hadOutput = InvokeBuilderWrite(_properties.Where((PropertyBuilder x) => x.FieldTypeValue == PropertyBuilder.FieldType.Const && x.IsStatic).OrderBy(keySelector), ref hadOutput, in writer, group: true);
			hadOutput = InvokeBuilderWrite(_properties.Where((PropertyBuilder x) => x.FieldTypeValue == PropertyBuilder.FieldType.ReadOnly).OrderBy(keySelector), ref hadOutput, in writer, group: true);
			hadOutput = InvokeBuilderWrite(_properties.Where((PropertyBuilder x) => x.FieldTypeValue == PropertyBuilder.FieldType.Default).OrderBy(keySelector), ref hadOutput, in writer, group: true);
			hadOutput = InvokeBuilderWrite(_constructors.OrderBy((ConstructorBuilder x) => x.Parameters.Count), ref hadOutput, in writer);
			hadOutput = InvokeBuilderWrite(_properties.Where((PropertyBuilder x) => x.FieldTypeValue == PropertyBuilder.FieldType.Property).OrderBy(keySelector), ref hadOutput, in writer);
			hadOutput = InvokeBuilderWrite(from x in _methods
				orderby x.Name, x.Parameters.Count
				select x, ref hadOutput, in writer);
			InvokeBuilderWrite(_nestedClass, ref hadOutput, in writer);
		}
	}

	private static void WriteClassAttributes(IEnumerable<string> classAttributes, in CodeWriter writer)
	{
		foreach (string item in from x in classAttributes.Distinct()
			orderby x
			select x)
		{
			writer.AppendLine(item);
		}
	}

	private static bool InvokeBuilderWrite<T>(IEnumerable<T> builders, ref bool hadOutput, in CodeWriter writer, bool group = false) where T : IBuilder
	{
		if (builders == null || !builders.Any())
		{
			return hadOutput;
		}
		if (hadOutput)
		{
			writer.NewLine();
		}
		Queue<T> queue = new Queue<T>();
		foreach (T builder in builders)
		{
			queue.Enqueue(builder);
		}
		while (queue.Any())
		{
			queue.Dequeue().Write(in writer);
			if (!group && queue.Any())
			{
				writer.NewLine();
			}
		}
		return true;
	}
}
