using System;
using System.Collections.Generic;
using System.Linq;

namespace AMarkup.Generators.CodeGen;

public sealed class LogicalConditionBuilder
{
	private ICodeWriter _writer { get; }

	private string? _condition { get; }

	private string _operator { get; }

	private Queue<LogicalConditionBuilder> _builders { get; }

	private Action<ICodeWriter>? _innerWrite { get; set; }

	private LogicalConditionBuilder? _parent { get; }

	internal LogicalConditionBuilder(ICodeWriter writer, string? condition, string @operator = "if", LogicalConditionBuilder? parent = null)
	{
		_writer = writer;
		_condition = condition;
		_operator = @operator;
		_parent = parent;
		_builders = new Queue<LogicalConditionBuilder>();
	}

	public LogicalConditionBuilder WithBody(Action<ICodeWriter> innerWrite)
	{
		_innerWrite = innerWrite;
		return this;
	}

	public LogicalConditionBuilder ElseIf(string condition)
	{
		LogicalConditionBuilder logicalConditionBuilder = new LogicalConditionBuilder(_writer, condition, "else if", this);
		_builders.Enqueue(logicalConditionBuilder);
		return logicalConditionBuilder;
	}

	public LogicalConditionBuilder ElseIf(string condition, Action<ICodeWriter> innerWrite)
	{
		LogicalConditionBuilder logicalConditionBuilder = new LogicalConditionBuilder(_writer, condition, "else if", this).WithBody(innerWrite);
		_builders.Enqueue(logicalConditionBuilder);
		return logicalConditionBuilder;
	}

	public LogicalConditionBuilder Else()
	{
		LogicalConditionBuilder logicalConditionBuilder = new LogicalConditionBuilder(_writer, null, "else", this);
		_builders.Enqueue(logicalConditionBuilder);
		return logicalConditionBuilder;
	}

	public LogicalConditionBuilder Else(Action<ICodeWriter> innerWrite)
	{
		LogicalConditionBuilder logicalConditionBuilder = new LogicalConditionBuilder(_writer, null, "else", this).WithBody(innerWrite);
		_builders.Enqueue(logicalConditionBuilder);
		return logicalConditionBuilder;
	}

	public ICodeWriter EndIf()
	{
		if (_parent != null)
		{
			_parent.EndIf();
		}
		else
		{
			WriteInternal();
		}
		return _writer;
	}

	private void WriteInternal()
	{
		string value = (string.IsNullOrEmpty(_condition) ? _operator : (_operator + " (" + _condition + ")"));
		using (_writer.Block(value))
		{
			_innerWrite?.Invoke(_writer);
		}
		while (_builders.Any())
		{
			_builders.Dequeue().WriteInternal();
		}
	}
}
