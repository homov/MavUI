using System;
using System.Collections.Generic;

namespace AMarkup.Generators.CodeGen;

public sealed class SwitchBuilder
{
	private readonly List<SwitchCaseBuilder> _switchCases = new List<SwitchCaseBuilder>();

	private ICodeWriter _writer { get; }

	private string _switchOn { get; }

	internal bool Expression { get; }

	internal SwitchBuilder(ICodeWriter writer, string switchOn, bool expression)
	{
		_writer = writer;
		_switchOn = switchOn;
		Expression = expression;
	}

	public SwitchCaseBuilder AddCase(string @case)
	{
		SwitchCaseBuilder switchCaseBuilder = new SwitchCaseBuilder(this, @case);
		_switchCases.Add(switchCaseBuilder);
		return switchCaseBuilder;
	}

	public ICodeWriter Close()
	{
		if (Expression)
		{
			WriterExpressionSwitchCase();
		}
		else if (_writer is CodeWriter writer)
		{
			WriteClassicSwitchCase(writer);
		}
		return _writer;
	}

	private void WriteClassicSwitchCase(CodeWriter writer)
	{
		using (_writer.Block("switch(" + _switchOn + ")"))
		{
			foreach (SwitchCaseBuilder switchCase in _switchCases)
			{
				((IBuilder)switchCase).Write(in writer);
			}
		}
	}

	private void WriterExpressionSwitchCase()
	{
		throw new NotImplementedException();
	}
}
