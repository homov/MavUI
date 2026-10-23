using System.Collections.Generic;

namespace AMarkup.Generators.CodeGen;

public interface IParameterized<T> where T : BuilderBase<T>
{
	T Parent { get; }

	List<ParameterBuilder<T>> Parameters { get; }
}
