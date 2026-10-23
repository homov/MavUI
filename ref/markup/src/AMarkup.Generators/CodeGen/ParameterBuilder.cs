using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public class ParameterBuilder<T> where T : BuilderBase<T>
{
	public T Parent { get; }

	public string? Type { get; internal set; }

	public string? Name { get; internal set; }

	public string? DefaultValue { get; internal set; }

	public bool HasDefaultValue { get; internal set; }

	internal ParameterBuilder(T parent)
	{
		Parent = parent;
	}

	public ParameterBuilder<T> WithType(string typeName)
	{
		Type = typeName;
		return this;
	}

	public ParameterBuilder<T> WithType(INamedTypeSymbol symbol)
	{
		Parent.AddNamespaceImport(symbol);
		return WithType(symbol.Name);
	}

	public ParameterBuilder<T> WithDefaultValue()
	{
		return WithDefaultValue("default");
	}

	public ParameterBuilder<T> WithDefaultValue(string defaultValue)
	{
		DefaultValue = defaultValue;
		HasDefaultValue = true;
		return this;
	}

	public ParameterBuilder<T> WithDefaultValue(object? value)
	{
		return WithDefaultValue($"{value}");
	}

	public ParameterBuilder<T> WithNullDefault()
	{
		return WithDefaultValue("null");
	}

	public override string ToString()
	{
		string text = Type + " " + Name;
		if (HasDefaultValue)
		{
			text = text + "= " + DefaultValue;
		}
		return text;
	}

	public override bool Equals(object obj)
	{
		if (obj != null && obj is ParameterBuilder<T> parameterBuilder && parameterBuilder.Type == Type)
		{
			return parameterBuilder.Name == Name;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ToString().GetHashCode();
	}
}
