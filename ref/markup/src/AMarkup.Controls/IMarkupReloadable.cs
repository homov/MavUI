using System;

namespace AMarkup;

/// <summary>
/// Implemented by controls that can rebuild their markup when hot reload is applied.
/// </summary>
public interface IMarkupReloadable
{
    void ReloadMarkup();
}
