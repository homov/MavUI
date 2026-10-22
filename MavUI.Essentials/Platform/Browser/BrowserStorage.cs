using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Mav.Essentials.Browser;

/// <summary>JavaScript interop for <c>window.localStorage</c>. Used only by the browser backend.</summary>
[SupportedOSPlatform("browser")]
internal static partial class BrowserStorage
{
    [JSImport("globalThis.window.localStorage.getItem")]
    internal static partial string? GetItem(string key);

    [JSImport("globalThis.window.localStorage.setItem")]
    internal static partial void SetItem(string key, string value);

    [JSImport("globalThis.window.localStorage.removeItem")]
    internal static partial void RemoveItem(string key);
}
