namespace AMarkup.Generators;

internal record AttachedPropertyInfo(string Name, string PropertyTypeFullyQualified, bool PropertyTypeIsNullableValueType, string AvaloniaProperty, GenerationTypeInfo GenerationTypeInfo) : BaseModel(GenerationTypeInfo);
