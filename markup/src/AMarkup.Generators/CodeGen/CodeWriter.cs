using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMarkup.Generators.CodeGen;

internal sealed class CodeWriter : IDisposable, ICodeWriter
{
	private readonly IndentStyle _indentStyle;

	private readonly string Indent;

	private int _indentLevel;

	private int _extraIndent;

	private readonly List<(StringBuilder? StringBuilder, CodeWriter? Writer)> _blocks = new List<(StringBuilder, CodeWriter)>();

	public CodeWriter(IndentStyle indentStyle, int startingLevel = 0)
	{
		_indentStyle = indentStyle;
		Indent = ((indentStyle != IndentStyle.Tabs) ? "    " : "\t");
	}

	public void IncreaseIndent()
	{
		_extraIndent++;
	}

	public void DecreaseIndent()
	{
		_extraIndent--;
	}

	public IDisposable Block(string value, params string[] constraints)
	{
		AppendLine(value.TrimEnd(Array.Empty<char>()));
		_indentLevel++;
		foreach (string value2 in constraints)
		{
			if (!string.IsNullOrEmpty(value2))
			{
				AppendLine(value2);
			}
		}
		_indentLevel--;
		AppendLine("{");
		_indentLevel++;
		return this;
	}

	public ICodeWriter BlockWriter(string? originalLine)
	{
		CodeWriter codeWriter = new CodeWriter(_indentStyle, _indentLevel);
		if (originalLine != null)
		{
			codeWriter.AppendLine(originalLine);
		}
		codeWriter.AppendLine("{");
		codeWriter._indentLevel++;
		_blocks.Add((null, codeWriter));
		return codeWriter;
	}

	private StringBuilder EnsureStringBuilder()
	{
		StringBuilder stringBuilder = _blocks.LastOrDefault().StringBuilder;
		if (stringBuilder == null)
		{
			stringBuilder = new StringBuilder();
			_blocks.Add((stringBuilder, null));
		}
		return stringBuilder;
	}

	public void Append(string value)
	{
		EnsureStringBuilder().Append(GetIndentedValue(value.TrimEnd(Array.Empty<char>())));
	}

	public void AppendUnindented(string value)
	{
		EnsureStringBuilder().Append(value.TrimEnd(Array.Empty<char>()));
	}

	public void NewLine()
	{
		EnsureStringBuilder().AppendLine();
	}

	public void AppendLine(string value)
	{
		EnsureStringBuilder().AppendLine(GetIndentedValue(value.TrimEnd(Array.Empty<char>())));
	}

	public void AppendUnindentedLine(string value)
	{
		EnsureStringBuilder().AppendLine(value.TrimEnd(Array.Empty<char>()));
	}

	private string GetIndentedValue(string value)
	{
		string text = string.Empty;
		for (int i = 0; i < _indentLevel + _extraIndent; i++)
		{
			text += Indent;
		}
		return text + value;
	}

	public void Dispose()
	{
		if (_indentLevel > 0)
		{
			_indentLevel--;
			EnsureStringBuilder().AppendLine(GetIndentedValue("}"));
		}
	}

	public string Render()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (var block in _blocks)
		{
			if (block.StringBuilder != null)
			{
				stringBuilder.Append((object?)block.StringBuilder);
			}
			if (block.Writer != null)
			{
				block.Writer.Dispose();
				stringBuilder.Append(block.Writer.Render());
			}
		}
		Dispose();
		return stringBuilder.ToString();
	}
}
