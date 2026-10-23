using System.Collections.Generic;

namespace AMarkup.Generators.CodeGen;

public sealed class GenericBuilder
{
	private bool _isClass;

	private bool _newDefault;

	private List<string> _constraints { get; }

	public string Name { get; }

	internal GenericBuilder(string name)
	{
		Name = name;
		_constraints = new List<string>();
	}

	public GenericBuilder New()
	{
		_newDefault = true;
		return this;
	}

	public GenericBuilder Class()
	{
		_isClass = true;
		return this;
	}

	public GenericBuilder AddConstraint(string constraint)
	{
		if (!(constraint == "new()"))
		{
			if (constraint == "class")
			{
				return Class();
			}
			if (!string.IsNullOrWhiteSpace(constraint) && !_constraints.Contains(constraint))
			{
				_constraints.Add(constraint);
			}
			return this;
		}
		return New();
	}

	public override string ToString()
	{
		if (_isClass)
		{
			_constraints.Insert(0, "class");
		}
		if (_newDefault)
		{
			_constraints.Insert(0, "new()");
		}
		string text = string.Join(", ", _constraints).Trim();
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		return "where " + Name + " : " + text;
	}
}
