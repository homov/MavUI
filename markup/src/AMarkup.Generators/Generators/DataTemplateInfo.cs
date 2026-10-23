namespace AMarkup.Generators;

internal record DataTemplateInfo(
	string PropertyTypeFullyQualifiedName,
	bool PropertyTypeIsControlTemplate,
	bool PropertyTypeIsDataTemplate,
	string PropertyName,
	GenerationTypeInfo GenerationTypeInfo) : BaseModel(GenerationTypeInfo);
