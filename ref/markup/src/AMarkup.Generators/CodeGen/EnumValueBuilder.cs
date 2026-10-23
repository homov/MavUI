using System.Collections.Generic;
using System.Linq;

namespace AMarkup.Generators.CodeGen;

public class EnumValueBuilder : IBuilder
{
	private readonly List<string> _attributes = new List<string>();

	private DocumentationComment? _xmlDoc;

	public string Name { get; }

	public int? Value { get; private set; }

	public EnumBuilder Enum { get; }

	internal EnumValueBuilder(string name, EnumBuilder builder, int? value)
	{
		Name = name;
		Value = value;
		Enum = builder;
	}

	public EnumValueBuilder WithSummary(string summary)
	{
		_xmlDoc = new SummaryDocumentationComment
		{
			Summary = summary
		};
		return this;
	}

	public EnumValueBuilder WithInheritDoc(bool inherit = true)
	{
		_xmlDoc = new InheritDocumentationComment();
		return this;
	}

	public EnumValueBuilder WithInheritDoc(string from)
	{
		_xmlDoc = new InheritDocumentationComment
		{
			InheritFrom = from
		};
		return this;
	}

	public EnumValueBuilder AddValue(string value, int? numericValue = null)
	{
		return Enum.AddValue(value, numericValue);
	}

	public EnumValueBuilder AddAttribute(string attribute)
	{
		string item = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
		if (!_attributes.Contains(item))
		{
			_attributes.Add(item);
		}
		return this;
	}

	void IBuilder.Write(in CodeWriter writer)
	{
		_xmlDoc?.Write(in writer);
		foreach (string item in _attributes.OrderBy((string x) => x))
		{
			writer.AppendLine("[" + item + "]");
		}
		writer.Append(Name);
		if (Value.HasValue)
		{
			writer.AppendUnindented($" = {Value}");
		}
	}
}
