namespace AMarkup.Generators.CodeGen;

internal class InheritDocumentationComment : DocumentationComment
{
	internal string? InheritFrom { get; set; }

	public override void Write(in CodeWriter writer)
	{
		writer.AppendLine("/// <inheritdoc " + ((InheritFrom == null) ? string.Empty : ("cref=\"" + InheritFrom + "\"")) + "/>");
	}
}
