namespace Mav.MarkupGenerator;

/// <summary>
/// One generated setter. For a property, <see cref="FieldName"/> is the static styled property field and
/// <see cref="ValueType"/> is its value type. For an event, <see cref="ValueType"/> is the event args type.
/// </summary>
internal sealed class MarkupMember
{
    public MarkupMember(string name, string fieldName, string valueType, bool isEvent, bool isAttached = false)
    {
        Name = name;
        FieldName = fieldName;
        ValueType = valueType;
        IsEvent = isEvent;
        IsAttached = isAttached;
    }

    public string Name { get; }

    public string FieldName { get; }

    public string ValueType { get; }

    public bool IsEvent { get; }

    /// <summary>True for an attached property such as <c>Grid.Row</c>; it is set on any <c>AvaloniaObject</c>.</summary>
    public bool IsAttached { get; }
}
