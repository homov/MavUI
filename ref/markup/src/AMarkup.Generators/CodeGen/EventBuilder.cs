using System;
using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public class EventBuilder : BuilderBase<EventBuilder>
{
	private Accessibility _declaredAccessibility = Accessibility.Public;

	private string? _explicitImplementation;

	private string _eventDelegateType = "EventHandler";

	private Action<ICodeWriter>? _addHandlerDelegate;

	private Action<ICodeWriter>? _removeHandlerDelegate;

	private bool _static;

	public string Name { get; }

	public ClassBuilder Class { get; }

	internal EventBuilder(ClassBuilder @class, string eventName)
	{
		Name = eventName;
		Class = @class;
	}

	public EventBuilder MakeStatic(bool isStatic = true)
	{
		_static = isStatic;
		return this;
	}

	public ClassBuilder WithBackingField(string backingField)
	{
		return WithAddExpression(backingField + " += value").WithRemoveExpression(backingField + " -= value").Class;
	}

	public EventBuilder WithAddHandler(Action<ICodeWriter> addDelegate)
	{
		_addHandlerDelegate = BlockHandler;
		return this;
		void BlockHandler(ICodeWriter writer)
		{
			using (writer.Block("add"))
			{
				addDelegate?.Invoke(writer);
			}
		}
	}

	public EventBuilder WithAddExpression(string addDelegateExpression)
	{
		_addHandlerDelegate = (ICodeWriter w) =>
		{
			w.AppendLine("add => " + addDelegateExpression + ";");
		};
		return this;
	}

	public EventBuilder WithRemoveHandler(Action<ICodeWriter> addDelegate)
	{
		_removeHandlerDelegate = BlockHandler;
		return this;
		void BlockHandler(ICodeWriter writer)
		{
			using (writer.Block("remove"))
			{
				addDelegate?.Invoke(writer);
			}
		}
	}

	public EventBuilder WithRemoveExpression(string addDelegateExpression)
	{
		_removeHandlerDelegate = (ICodeWriter w) =>
		{
			w.AppendLine("remove => " + addDelegateExpression + ";");
		};
		return this;
	}

	public EventBuilder WithExplicitImplementation(string inferfaceName)
	{
		_explicitImplementation = inferfaceName;
		return this;
	}

	public ClassBuilder WithDefaultExplicitImplementation(string interfaceName, Accessibility backingFieldAccessibility = Accessibility.Private, string? backingFieldName = null)
	{
		if (backingFieldName == null || string.IsNullOrEmpty(backingFieldName))
		{
			backingFieldName = $"_{char.ToLower(Name[0])}{Name.Substring(1)}";
		}
		return WithExplicitImplementation(interfaceName).WithBackingField(backingFieldName).AddEvent(backingFieldName).WithAccessibility(backingFieldAccessibility)
			.WithDelegateHandler(_eventDelegateType)
			.Class;
	}

	public EventBuilder WithDelegateHandler(string handlerType)
	{
		_eventDelegateType = handlerType;
		return this;
	}

	public EventBuilder WithDelegateHandler(INamedTypeSymbol handlerType)
	{
		_eventDelegateType = handlerType.Name;
		return AddNamespaceImport(handlerType);
	}

	public EventBuilder WithAccessibility(Accessibility accessibility)
	{
		_declaredAccessibility = accessibility;
		return this;
	}

	public EventBuilder MakePublic()
	{
		_declaredAccessibility = Accessibility.Public;
		return this;
	}

	public EventBuilder MakeProtected()
	{
		_declaredAccessibility = Accessibility.Protected;
		return this;
	}

	public EventBuilder MakePrivate()
	{
		_declaredAccessibility = Accessibility.Private;
		return this;
	}

	public EventBuilder MakeInternal()
	{
		_declaredAccessibility = Accessibility.Internal;
		return this;
	}

	public EventBuilder MakeInternalProtected()
	{
		_declaredAccessibility = Accessibility.ProtectedAndInternal;
		return this;
	}

	public override EventBuilder AddAssemblyAttribute(string attribute)
	{
		Class.AddAssemblyAttribute(attribute);
		return this;
	}

	public override EventBuilder AddNamespaceImport(string importedNamespace)
	{
		Class.AddNamespaceImport(importedNamespace);
		return this;
	}

	public override EventBuilder AddNamespaceImport(ISymbol symbol)
	{
		Class.AddNamespaceImport(symbol);
		return this;
	}

	public override EventBuilder AddNamespaceImport(INamespaceSymbol symbol)
	{
		Class.AddNamespaceImport(symbol);
		return this;
	}

	internal override void Write(in CodeWriter writer)
	{
		string text = (_static ? "static " : string.Empty);
		string text2 = _declaredAccessibility.Code() + " " + text + "event " + _eventDelegateType + " " + Name;
		if (!string.IsNullOrEmpty(_explicitImplementation))
		{
			text2 = "event " + text + _eventDelegateType + " " + _explicitImplementation + "." + Name;
		}
		if (_addHandlerDelegate == null || _removeHandlerDelegate == null)
		{
			writer.AppendLine(text2 + ";");
			return;
		}
		using (writer.Block(text2))
		{
			_addHandlerDelegate(writer);
			_removeHandlerDelegate(writer);
		}
	}
}
