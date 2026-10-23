using System.Collections.Generic;
using System.Linq;

namespace AMarkup.Generators.CodeGen;

internal class GenericCollection : List<GenericBuilder>
{
	public override string ToString()
	{
		if (!this.Any())
		{
			return string.Empty;
		}
		return "<" + string.Join(", ", this.Select((GenericBuilder x) => x.Name)) + ">";
	}

	public string[] Contraints()
	{
		return (from x in this
			select x.ToString() into x
			where !string.IsNullOrEmpty(x)
			select x).ToArray();
	}
}
