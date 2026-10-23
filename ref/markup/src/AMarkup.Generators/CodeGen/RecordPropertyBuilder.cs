using AMarkup.Generators.CodeGen;
using Microsoft.CodeAnalysis;

namespace AMarkup.Generators.CodeGen;

public class RecordPropertyBuilder
{
	public string Type { get; }

	public string Name { get; }

	public RecordBuilder Record { get; }

	public bool HasDefaultValue { get; private set; }

	public object? DefaultValue { get; private set; }

	public Accessibility? AccessModifier { get; private set; }

	public RecordPropertyBuilder(string type, string name, Accessibility accessModifier, RecordBuilder record)
	{
		Type = type;
		Name = name;
		AccessModifier = accessModifier;
		Record = record;
	}

	public override string ToString()
	{
		string text = Type + " " + Name;
		if (HasDefaultValue)
		{
			text += $" = {DefaultValue}";
		}
		return text;
	}

	public string ToPositionalProperty()
	{
		return ToString();
	}

	public string ToInitProperty()
	{
		string text = (HasDefaultValue ? $" = {DefaultValue};" : string.Empty);
		return AccessModifier.Code() + " " + Type + " " + Name + " { get; init; }" + text;
	}

	public RecordPropertyBuilder WithDefaultValue(string defaultValue)
	{
		DefaultValue = defaultValue;
		HasDefaultValue = true;
		return this;
	}

	public RecordPropertyBuilder WithDefaultValue(object value)
	{
		return WithDefaultValue($"{value}");
	}
}
