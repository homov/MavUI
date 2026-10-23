using System;

namespace AMarkup.Generators.CodeGen;

public sealed class SwitchCaseBuilder : IBuilder
{
	private Action<ICodeWriter>? _content;

	private SwitchBuilder _parent { get; }

	private string _case { get; }

	public SwitchCaseBuilder(SwitchBuilder parent, string @case)
	{
		_parent = parent;
		_case = @case;
	}

	public SwitchBuilder WithContent(Action<ICodeWriter> contentDelegate)
	{
		_content = contentDelegate ?? new Action<ICodeWriter>(EmptyBody);
		return _parent;
	}

	public SwitchBuilder WithExpression(string returnValue = "null")
	{
		_content = (ICodeWriter w) =>
		{
			w.Append(_case + " => " + returnValue);
		};
		return _parent;
	}

	void IBuilder.Write(in CodeWriter writer)
	{
		if (_parent.Expression)
		{
			_content?.Invoke(writer);
			return;
		}
		writer.AppendLine("case " + _case + ":");
		writer.IncreaseIndent();
		_content?.Invoke(writer);
		writer.AppendLine("break;");
		writer.DecreaseIndent();
	}

	private static void EmptyBody(ICodeWriter writer)
	{
	}
}
