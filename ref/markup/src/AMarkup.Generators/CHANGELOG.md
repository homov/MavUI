# AMarkup.Generators changes

## Avalonia 12.1.4

- Migrated the runtime, demo and generated snapshots from Avalonia 11.2.3 to
  Avalonia 12.1.4.
- Synchronized the `AMarkup.Controls` project version with Avalonia: `12.1.4`.
  The version is centralized as `AvaloniaVersion` in `src/Directory.Build.props`.
  The generator version remains `0.0.1-dev`.
- Replaced the removed `Avalonia.Data.IBinding` parameter type with
  `Avalonia.Data.BindingBase`. `MarkupBinding.FromExpression` now returns a
  `CompiledBinding`.
- Expression bindings now create `Avalonia.Data.CompiledBinding` instances via
  `CompiledBinding.Create`, so property access is compiled instead of
  reflection-based. `AvaloniaPropertyBuilder` uses the same compiled path.
- Added `stringFormat` and `fallbackValue` to expression, builder and string-path
  binding overloads. String-path bindings also accept optional `BindingMode` and
  `IValueConverter` values.
- Avoided duplicate `BindingBase` overloads when the target property itself is
  typed as `BindingBase` (for example `TextSearch.TextBinding` or
  `TableViewColumn.Binding`).
- Regenerated all checked-in snapshots from the Avalonia 12.1.4 public surface.
  This picks up renames such as `SystemDecorations` -> `WindowDecorations` and
  `GotFocusEventArgs` -> `FocusChangedEventArgs`, removes generated APIs for
  types that became internal, and adds newly public Avalonia 12 controls and
  navigation pages (`ContentPage`, `Page`, `NavigationPage`, `TabbedPage`, ...).
- Reduced the generated surface from 119,814 to 88,334 lines and from 7.2 MB
  to 5.8 MB by removing the parameterless `Func<TSource>`/DataContext
  expression overloads and the unused generated `MarkupExtensionAttribute`
  annotations. The attribute type remains for source compatibility.
- Fixed generic-base property shadowing in `GetMembersForGeneration`; derived
  properties now replace same-named generic-base properties instead of
  producing ambiguous overloads.
- Replaced repeated linear member scans with name sets/dictionaries in the
  property and attached-property generators.

## 0.0.1-dev

- Generator version is `0.0.1-dev`.
- `AMarkup.Generators`, `AMarkup.Controls` and `AMarkup.Demo` target `net10.0`
  and use C# 14, matching the installed .NET SDK 10.
- All legacy Uno/WinUI naming has been removed. The generator and runtime use
  Avalonia terms: `AvaloniaProperty`, `AvaloniaObject`, `Control`,
  `ControlTemplate`, `IBrush`, `Avalonia.Media.Color`, and
  `Avalonia.Controls.ResourceDictionary`.
- Generator sources live under `Generators/` and builder helpers under
  `CodeGen/`. Namespaces are limited to `AMarkup.Generators` and
  `AMarkup.Generators.CodeGen`.
- Runtime helpers live in the single `AMarkup` namespace: `MarkupBinding`,
  `AvaloniaPropertyBuilder<T>`, `IAvaloniaPropertyBuilder<T>`, `Style<TControl>`,
  `MarkupExtensionAttribute` (compatibility marker, no longer emitted by the
  generator), `GenerateMarkupForAssemblyAttribute`, `MarkupImageSource`, and the
  hot reload API.
- Generated classes import `AMarkup` directly; there is no `Internals`
  namespace and no generated reference to WinUI.
- Removed generator-side duplicates of `GenerateMarkupForAssemblyAttribute`.
- Removed polyfills and dead helpers: `HashCode`, `NotNullWhenAttribute`,
  `IsExternalInit`, `CallerArgumentExpressionAttributeGenerator`, `ObjectPool`,
  `PooledStringBuilder`, and the unused `AvantiPoint` helper.
- Added a typed `Style<TControl>` runtime type and generated style setters.
  Regular property, attached-property, event and data-template generation remain.
- Data-template generation now emits valid Avalonia `FuncControlTemplate` and
  `FuncDataTemplate<T>` instances instead of WinUI template APIs.

## Hot reload

- `MarkupHotReload` is registered as a `MetadataUpdateHandler`.
- `IMarkupReloadable` allows a control to rebuild itself.
- `AvaloniaObject.UseMarkupHotReload(Action)` registers a rebuild callback.
- `AppBuilder.UseMarkupHotReload()` opts the application into hot reload handling.
- `MarkupViewLifecycle` provides lazy content construction, attach/opened
  registration, detach/closed cleanup, and reload handling for custom base
  classes. `rebuildOnReattach` can force a fresh rebuild when a page returns
  from navigation.
- Callbacks run on the Avalonia UI thread after a metadata update.

## Fluent APIs

- Every writable property gets value, `BindingBase`, string-path,
  `Action<IAvaloniaPropertyBuilder<T>>` and
  `TSource + Expression<Func<TSource, TSourceValue>>` overloads.
- Panel children are added with the generated `Children(this T panel,
  params Control[] children)` extension. It is emitted from the real
  `Panel.Children` collection and is generic, so panel types without their own
  generated file (`Canvas`, `UniformGrid`, ...) keep their concrete return type.
- `Grid` gets `Cols(string)` and `Rows(string)` shortcuts that accept the same
  definition syntax as AXAML, for example `.Cols("Auto, *, 2*")` and
  `.Rows("40, 2*, Auto")`.
- Attached properties get:
  - an option-parameter method (`element.Grid(row: 1, column: 2)`),
  - a builder method (`element.Grid(g => g.Row(1).Column(2))`),
  - and conflict-free direct setters (`GridRow`, `GridColumn`, `CanvasLeft`,
    `CanvasTop`, `DockPanelDock`, `AutomationPropertiesName`, ...).
- Attached-property setters also provide `BindingBase`, string-path and
  `Expression<Func<TSource, TValue>>` binding overloads.
- Duplicate attached properties that shadow an instance property
  (`TextElement.FontFamily`, `ScrollViewer.BringIntoViewOnFocusChange`, ...)
  are skipped because the regular property extension already covers them.

## Events

- Every accessible public event gets fluent `On{EventName}(...)` extension
  methods on the control type, for example `.OnClick(() => ...)`,
  `.OnClick(args => ...)`, or `.OnClick((sender, args) => ...)`.
- Standard `EventHandler`-style events drop the sender from the convenience
  overload. Events with `Action`/custom delegates keep their full delegate
  overload as well.
- Static, obsolete, non-void, and `ref`/`out` events are skipped because they
  cannot be represented safely by the fluent callback shape.
- The generated methods return the concrete control type (`T`) so they can be
  chained with the existing property and child extensions.

## Styles

- `new Style<Button>()` creates a style whose selector is
  `OfType<Button>()`. A selector factory can add classes or pseudoclasses:
  `new Style<Button>(s => s.Class("accent"))`. Explicit-root and traversal
  selectors are preserved, while simple class/pseudoclass selectors are
  appended to the implicit `OfType<TControl>()`. `WithSelector(...)` replaces
  the selector with the same behavior.
- Every Avalonia property on a `StyledElement` gets a style setter on
  `Style<TControl>`, for example:
  ```csharp
  new Style<Button>(s => s.Class("accent"))
      .Background("#F59E0B")
      .Foreground("#111827")
      .Padding(14, 8)
      .CornerRadius(7);
  ```
- Style setters support value, `BindingBase`, source expression and
  string-path binding overloads. Common value helpers
  (`Color`/hex strings for brushes, uniform/edge `Thickness` values,
  `CornerRadius`, `FontFamily`, `Geometry`, image sources) are included.
- Attached properties get the same typed style setters, for example
  `.GridRow(1)`, `.GridColumn(2)`, `.CanvasLeft(20)`, `.DockPanelDock(Dock.Left)`.
- `Style<TControl>` inherits the normal Avalonia style APIs, so existing
  `Children`, `Setters`, `Animations` and `Resources` extensions keep working.
- Sealed controls use a fixed `Style<SealedControl>` target; non-sealed controls
  use a generic `Style<TControl>` target so base-control setters also work for
  derived control styles.

## Resources

- `MarkupResource.FindResource<T>(key)` performs a typed resource lookup on any
  `IResourceHost` (`Control`, `Window`, `Application`, ...). Missing or
  incompatible resources throw `KeyNotFoundException`.
- `MarkupResource.TryFindResource<T>(key, out value)` and
  `FindResourceOrDefault<T>(key, fallback)` provide non-throwing alternatives.
- `MarkupResource.Dynamic(key)` (alias `DynamicResource(key)`) creates an
  Avalonia `DynamicResourceExtension`.
  Property, style and attached-property fluent methods now include a
  `BindingBase` overload, so dynamic resources can be used directly:
  ```csharp
  new Button()
      .Background(MarkupResource.Dynamic("AccentBrush"))
      .GridRow(MarkupResource.Dynamic("AccentRow"));
  ```
- `element.DynamicResource(property, key)` and
  `style.DynamicResource(property, key)` bind any Avalonia property to a
  dynamic resource when a property-named method is not available.
- Existing `Resources(...)` generated extensions continue to work with
  `ResourceDictionary` and `IResourceDictionary` values.

## Bindings

- Source-aware expression bindings create `CompiledBinding` instances through
  `CompiledBinding.Create` and use the full nested property path with the
  supplied source instance. The old `CallerArgumentExpression` path parser has
  been removed.
- Expression bindings expose the source value type independently from the
  target property type. Primitive and enum mismatches are converted
  automatically, so `.Text(vm, x => x.Counter)` binds an `int` to a `string`
  property without a converter.
- Custom converters can be passed as a reusable `IValueConverter` or inline with
  Avalonia's `FuncValueConverter`, for example
  `.Foreground(vm, x => x.Counter, converter: new FuncValueConverter<int, IBrush>(...))`.
- Two-way automatic conversion is supported; lossy numeric convert-back
  truncates toward zero.
- A fresh `AvaloniaPropertyBuilder<T>` is created for each generated call, so
  binding state cannot leak between properties.

## Snapshots

`src/AMarkup.Controls/Generated` contains the emitted `*.g.cs` snapshots grouped
by generator. To refresh them, temporarily add `AMarkup.Generators` as an
analyzer reference, build with `-p:EmitCompilerGeneratedFiles=true`, copy the
generated files into `Generated/`, and remove the analyzer reference again.
