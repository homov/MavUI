namespace Mav.MarkupGenerator;

/// <summary>
/// One generated setter. For a property, <see cref="FieldName"/> is the static styled property field and
/// <see cref="ValueType"/> is its value type. For an event, <see cref="ValueType"/> is the event args type.
/// </summary>
internal sealed class MarkupMember
{
    public MarkupMember(string name, string fieldName, string valueType, bool isEvent)
    {
        Name = name;
        FieldName = fieldName;
        ValueType = valueType;
        IsEvent = isEvent;
    }

    public string Name { get; }

    public string FieldName { get; }

    public string ValueType { get; }

    public bool IsEvent { get; }
}
