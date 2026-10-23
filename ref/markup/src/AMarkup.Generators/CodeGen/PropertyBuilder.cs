using System;
using System.Collections.Generic;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public class PropertyBuilder : IBuilder
{
	internal enum FieldType
	{
		Const,
		ReadOnly,
		Default,
		Property
	}

	private bool _autoprops;

	internal FieldType FieldTypeValue = FieldType.Property;

	internal ValueType PropertyValueType;

	private Action<ICodeWriter>? _getter;

	private string? _getterExpression;

	private Action<ICodeWriter>? _setter;

	private string? _setterExpression;

	private string? _value;

	private bool _getOnly;

	private bool _virtual;

	private bool _override;

	private Accessibility? _setterAccessibility;

	private readonly List<string> _attributes = new List<string>();

	private DocumentationComment? _xmlDoc;

	public string Name { get; }

	public string? Type { get; private set; }

	public ClassBuilder Class { get; }

	public Accessibility? AccessModifier { get; private set; }

	public bool IsStatic { get; private set; }

	internal PropertyBuilder(string name, Accessibility? accessModifier, ClassBuilder builder)
	{
		Name = name;
		AccessModifier = accessModifier;
		Class = builder;
	}

	public PropertyBuilder WithSummary(string summary)
	{
		_xmlDoc = new SummaryDocumentationComment
		{
			Summary = summary
		};
		return this;
	}

	public PropertyBuilder WithInheritDoc(bool inherit = true)
	{
		_xmlDoc = new InheritDocumentationComment();
		return this;
	}

	public PropertyBuilder WithInheritDoc(string from)
	{
		_xmlDoc = new InheritDocumentationComment
		{
			InheritFrom = from
		};
		return this;
	}

	public PropertyBuilder AddNamespaceImport(string importedNamespace)
	{
		Class.AddNamespaceImport(importedNamespace);
		return this;
	}

	public PropertyBuilder AddNamespaceImport(ISymbol symbol)
	{
		return AddNamespaceImport(symbol.ContainingNamespace.ToString());
	}

	public PropertyBuilder AddNamespaceImport(INamespaceSymbol symbol)
	{
		return AddNamespaceImport(symbol.ToString());
	}

	public PropertyBuilder SetType(string type)
	{
		Type = type;
		return this;
	}

	public PropertyBuilder SetType(INamedTypeSymbol symbol)
	{
		return AddNamespaceImport(symbol.ContainingNamespace).SetType(symbol.GetTypeName());
	}

	public PropertyBuilder SetType(Type type)
	{
		return AddNamespaceImport(type.Namespace).SetType(type.GetTypeName());
	}

	public PropertyBuilder SetType<T>()
	{
		return SetType(typeof(T));
	}

	public PropertyBuilder MakePublicProperty()
	{
		return WithAccessModifier(Accessibility.Public);
	}

	public PropertyBuilder MakePrivateProperty()
	{
		return WithAccessModifier(Accessibility.Private);
	}

	public PropertyBuilder MakeProtectedProperty()
	{
		return WithAccessModifier(Accessibility.Protected);
	}

	public PropertyBuilder MakeInternalProperty()
	{
		return WithAccessModifier(Accessibility.Internal);
	}

	public PropertyBuilder WithAccessModifier(Accessibility accessModifier)
	{
		AccessModifier = accessModifier;
		return this;
	}

	public PropertyBuilder Override(bool @override = true)
	{
		_override = @override;
		return this;
	}

	public PropertyBuilder MakeStatic()
	{
		IsStatic = true;
		if (string.IsNullOrEmpty(_getterExpression))
		{
			FieldTypeValue = FieldType.Const;
		}
		return this;
	}

	public PropertyBuilder MakeVirtualProperty()
	{
		_virtual = true;
		return this;
	}

	public PropertyBuilder AddAttribute(string attribute)
	{
		string item = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
		if (!_attributes.Contains(item))
		{
			_attributes.Add(item);
		}
		return this;
	}

	public ClassBuilder UseGetOnlyAutoProp()
	{
		_autoprops = true;
		_getOnly = true;
		return Class;
	}

	public ClassBuilder UseAutoProps()
	{
		return UseAutoProps(null);
	}

	public ClassBuilder UseAutoProps(Accessibility? setterAccessibility)
	{
		_autoprops = true;
		_setterAccessibility = setterAccessibility;
		return Class;
	}

	public PropertyBuilder WithGetterExpression(string expression)
	{
		_getterExpression = expression;
		FieldTypeValue = FieldType.Property;
		return this;
	}

	public PropertyBuilder WithGetter(Action<ICodeWriter> getterBody)
	{
		_getter = getterBody;
		return this;
	}

	public PropertyBuilder WithSetterExpression(string expression)
	{
		_setterExpression = expression;
		return this;
	}

	public PropertyBuilder WithSetter(Action<ICodeWriter> setterBody)
	{
		_setter = setterBody;
		return this;
	}

	public ClassBuilder WithConstValue(string value, ValueType valueType = ValueType.UserSpecified)
	{
		_value = value;
		FieldTypeValue = FieldType.Const;
		PropertyValueType = valueType;
		return Class;
	}

	public ClassBuilder WithReadonlyValue(ValueType valueType = ValueType.UserSpecified)
	{
		return WithReadonlyValue(null, valueType);
	}

	public ClassBuilder WithReadonlyValue(string? value, ValueType valueType = ValueType.UserSpecified)
	{
		_value = value;
		FieldTypeValue = FieldType.ReadOnly;
		PropertyValueType = valueType;
		return Class;
	}

	public ClassBuilder WithValue(string? value, ValueType valueType = ValueType.UserSpecified)
	{
		_value = value;
		FieldTypeValue = FieldType.Default;
		PropertyValueType = valueType;
		return Class;
	}

	public ClassBuilder MakeBackingField(string? defaultValue = null)
	{
		return WithValue(defaultValue);
	}

	public PropertyBuilder WithBackingField(string? defaultValue = null, Accessibility? accessModifier = null)
	{
		string name = $"_{char.ToLower(Name[0])}{Name.Substring(1)}";
		PropertyBuilder propertyBuilder = Class.AddProperty(name, accessModifier ?? Accessibility.Private);
		propertyBuilder.Type = Type;
		propertyBuilder.FieldTypeValue = FieldType.Default;
		propertyBuilder.WithValue(defaultValue);
		return this;
	}

	void IBuilder.Write(in CodeWriter writer)
	{
		_xmlDoc?.Write(in writer);
		foreach (string attribute in _attributes)
		{
			writer.AppendLine("[" + attribute + "]");
		}
		string text = PropertyValueType switch
		{
			ValueType.Null => "null", 
			ValueType.Default => "default", 
			_ => _value, 
		};
		if (Type == null || string.IsNullOrEmpty(Type))
		{
			throw new ArgumentNullException("There is no 'Type' Specified for " + Name);
		}
		string text2 = Type.Trim();
		string text3 = Name.Trim();
		string text4 = (IsStatic ? " static" : null);
		string text5 = ((text3 == "Equals") ? " new" : string.Empty);
		string text6 = null;
		if (_virtual)
		{
			text6 = "virtual";
		}
		else if (_override)
		{
			text6 = "override";
		}
		string text7 = (FieldTypeValue switch
		{
			FieldType.Const => AccessModifier.Code() + text5 + " const " + text2 + " " + text3, 
			FieldType.ReadOnly => AccessModifier.Code() + text5 + text4 + " readonly " + text2 + " " + text3, 
			_ => (text6 == null) ? (AccessModifier.Code() + text5 + text4 + " " + text2 + " " + text3) : (AccessModifier.Code() + " " + text6 + " " + text2 + " " + text3), 
		}).Trim();
		int num = ((text != null && text.StartsWith("\"")) ? 9 : 5);
		if (FieldTypeValue != FieldType.Property)
		{
			if (string.IsNullOrEmpty(text) && FieldTypeValue != FieldType.Const)
			{
				writer.AppendLine(text7 + ";");
			}
			else if (text != null && text.Length > num)
			{
				writer.AppendLine(text7 + " =");
				writer.IncreaseIndent();
				writer.AppendLine(text + ";");
				writer.DecreaseIndent();
			}
			else
			{
				writer.AppendLine(text7 + " = " + text + ";");
			}
			return;
		}
		if (_autoprops)
		{
			if (_getOnly)
			{
				writer.AppendLine(text7 + " { get; }");
				return;
			}
			string text8 = (_setterAccessibility.Code() + " set").Trim();
			writer.AppendLine(text7 + " { get; " + text8 + "; }");
			return;
		}
		if (!string.IsNullOrEmpty(_getterExpression) && string.IsNullOrEmpty(_setterExpression) && _setter == null)
		{
			writer.AppendLine(text7 + " => " + _getterExpression + ";");
			return;
		}
		if (string.IsNullOrEmpty(_getterExpression) && string.IsNullOrEmpty(_setterExpression) && _getter == null && _setter == null)
		{
			writer.AppendLine(text7 + ";");
			return;
		}
		using (writer.Block(text7))
		{
			if (!string.IsNullOrEmpty(_getterExpression))
			{
				writer.AppendLine("get => " + _getterExpression + ";");
			}
			else if (_getter != null)
			{
				using (writer.Block("get"))
				{
					_getter(writer);
				}
			}
			if (!string.IsNullOrEmpty(_setterExpression))
			{
				writer.AppendLine("set => " + _setterExpression + ";");
			}
			else if (_setter != null)
			{
				using (writer.Block("set"))
				{
					_setter(writer);
					return;
				}
			}
		}
	}
}
