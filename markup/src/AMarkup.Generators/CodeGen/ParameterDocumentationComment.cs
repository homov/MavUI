using System.Collections.Generic;
using System.Linq;

namespace AMarkup.Generators.CodeGen;

internal class ParameterDocumentationComment : SummaryDocumentationComment
{
	private readonly Dictionary<string, string> parameterDocs = new Dictionary<string, string>();

	public void AddParameter(string parameterName, string documentationComment)
	{
		parameterDocs[parameterName] = documentationComment;
	}

	internal void RemoveUnusedParameters<T>(List<ParameterBuilder<T>> parameters) where T : BuilderBase<T>, IParameterized<T>
	{
		KeyValuePair<string, string>[] array = parameterDocs.Where((KeyValuePair<string, string> p) => !parameters.Any((ParameterBuilder<T> x) => x.Type == p.Key)).ToArray();
		foreach (KeyValuePair<string, string> keyValuePair in array)
		{
			parameterDocs.Remove(keyValuePair.Key);
		}
	}

	public override void Write(in CodeWriter writer)
	{
		base.Write(in writer);
		foreach (KeyValuePair<string, string> item in parameterDocs.OrderBy((KeyValuePair<string, string> x) => x.Key))
		{
			writer.AppendLine("/// <param name=\"" + item.Key + "\">" + item.Value + "</param>");
		}
	}
}
