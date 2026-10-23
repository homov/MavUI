using System;

namespace AMarkup.Generators.CodeGen;

internal class SummaryDocumentationComment : DocumentationComment
{
	internal string? Summary { get; set; }

	public override void Write(in CodeWriter writer)
	{
		if (Summary != null && !string.IsNullOrEmpty(Summary))
		{
			writer.AppendLine("/// <summary>");
			string[] array = Summary.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			foreach (string text in array)
			{
				writer.AppendLine("/// " + text);
			}
			writer.AppendLine("/// </summary>");
		}
	}
}
