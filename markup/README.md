# AMarkup

AMarkup is an experimental C#-first markup layer for Avalonia. It uses a Roslyn source generator to turn Avalonia controls, their properties, attached properties, events, and styles into fluent extension methods.

The result is normal Avalonia code with no XAML and no reflection-based property lookup:

```csharp
new Button()
    .Content("Save")
    .Background("#F59E0B")
    .Padding(12, 8)
    .OnClick(() => Save());
```

AMarkup is not a replacement for Avalonia. It is a thin authoring layer on top of Avalonia controls and is intended to stay close to Avalonia's property, binding, styling, and resource models.

> **Status:** experimental. `AMarkup.Controls` is versioned as `12.1.4` to match Avalonia; the generator remains `0.0.1-dev`. Targets `net10.0` and Avalonia `12.1.4`.

## Features

- Fluent setters for Avalonia properties.
- Value, `BindingBase`, string-path, and compiled strongly typed expression bindings with string format and fallback values.
- Automatic conversion for common primitive and nullable mismatches.
- Attached-property setters, including `Grid`, `Canvas`, `DockPanel`, and automation properties.
- Panel helpers such as `Children(...)`, `Grid.Cols(...)`, and `Grid.Rows(...)`.
- Fluent event subscriptions such as `.OnClick(...)`.
- Typed styles: `Style<TControl>` with generated setters for properties and attached properties.
- Data and control template helpers.
- Typed resource lookup, `DynamicResource` support, and generated `Resources(...)` helpers.
- Optional Hot Reload support that works with existing control base classes.

## Repository layout

- `src/AMarkup.Generators` — incremental Roslyn source generator.
- `src/AMarkup.Controls` — runtime helpers and checked-in generated Avalonia extensions.
- `src/AMarkup.Demo` — example application built entirely with generated fluent methods.
- `README.md` — this guide.
- `src/Directory.Build.props` — the shared `AvaloniaVersion` (`12.1.4`) used by the library and demo.

## Requirements

- .NET SDK 10.
- Avalonia `12.1.4`.
- A project that references `AMarkup.Controls`.
- The generator referenced as an analyzer if you want extensions for your own controls or additional assemblies.

## Setup

Reference the runtime library:

```xml
<ItemGroup>
  <ProjectReference Include="..\AMarkup.Controls\AMarkup.Controls.csproj" />
</ItemGroup>
```

To generate extensions in your project, also reference the generator as an analyzer:

```xml
<ItemGroup>
  <ProjectReference Include="..\AMarkup.Generators\AMarkup.Generators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

If the generator is distributed as a package, use the equivalent package reference with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`.

`AMarkup.Controls` already contains generated snapshots for the Avalonia version used by this repository. The analyzer reference is needed when you want generation for your own controls, third-party assemblies, or a different Avalonia surface.

## How generation is triggered

Generation happens at compile time. There are two sources of generated extensions:

1. **Controls in the current project** are discovered automatically if they are accessible `AvaloniaObject` descendants. No marker interface is required.
2. **Controls from another assembly** are scanned when the consuming project declares `GenerateMarkupForAssembly`.

For controls in an external assembly that are not already covered by `AMarkup.Controls`, add an assembly attribute in the consuming project:

```csharp
using AMarkup;

[assembly: GenerateMarkupForAssembly(typeof(MyCompany.Controls.CustomButton))]
```

The type passed to the attribute identifies the assembly to scan. You can add more attributes for other assemblies:

```csharp
[assembly: GenerateMarkupForAssembly(typeof(MyCompany.Controls.CustomButton))]
[assembly: GenerateMarkupForAssembly(typeof(AnotherCompany.Controls.FancyControl))]
```

`AMarkup.Controls` already includes generated extensions for the standard Avalonia controls used by this repository. Do not scan an assembly that is already covered by those snapshots: doing so can produce duplicate or ambiguous extension methods.

Build the project normally:

```bash
dotnet build
```

The compiler consumes the generated sources in memory. To inspect them on disk, opt in to emitted generated files:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

They then appear under `obj/.../generated/AMarkup.Generators/`. In an IDE, enable generated-file display if needed. Do not edit generated files manually.

## Quick start

```csharp
using AMarkup;
using Avalonia.Controls;
using Avalonia.Layout;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        this
            .Title("AMarkup")
            .Width(720)
            .Height(480)
            .Content(
                new StackPanel()
                    .Spacing(12)
                    .Children(
                        new TextBlock()
                            .Text("Hello from AMarkup")
                            .FontSize(24),
                        new Button()
                            .Content("Click me")
                            .HorizontalAlignment(HorizontalAlignment.Left)
                            .OnClick(() => Console.WriteLine("Clicked"))));
    }
}
```

## Examples

### Bindings

Use a source expression when the ViewModel instance is available:

```csharp
new TextBlock()
    .Text(viewModel, vm => vm.UserName);
```

Source expressions are converted to `Avalonia.Data.CompiledBinding` through
`CompiledBinding.Create`, so property access is compiled rather than resolved by
reflection. Nested paths, indexers and casts supported by Avalonia's compiled
binding parser work as well.

String formatting and fallback values are available on expression, string-path
and builder overloads:

```csharp
new TextBlock()
    .Text(viewModel, vm => vm.Counter,
        stringFormat: "Count: {0}",
        fallbackValue: "—");

new TextBlock()
    .Text("UserName", viewModel,
        stringFormat: "User: {0}",
        fallbackValue: "unknown");
```

Builder overloads accept the same `stringFormat` and `fallbackValue` named
arguments, for example `b => b.Binding(viewModel, vm => vm.Counter, stringFormat: "Count: {0}")`.

When `DataContext` is assigned later, pass a ready-made Avalonia binding:

```csharp
using Avalonia.Data;

new TextBlock()
    .Text(new Binding(nameof(MainViewModel.UserName)));
```

A ready-made binding is also the escape hatch for converters, relative sources, element names, string formats, and other advanced binding features. To keep a manually created binding compiled, pass `CompiledBinding.Create(...)` to the same `BindingBase` overload.

### Grid and attached properties

```csharp
new Grid()
    .Cols("Auto, *")
    .Rows("Auto, *")
    .Children(
        new TextBlock()
            .Text("Name")
            .GridRow(0)
            .GridColumn(0),
        new TextBox()
            .GridRow(0)
            .GridColumn(1),
        new Button()
            .Content("Save")
            .GridRow(1)
            .GridColumn(1));
```

### Events

```csharp
new Button()
    .Content("Increment")
    .OnClick(() => viewModel.Count++);
```

The generated event methods also provide event-args and sender-aware overloads where the delegate shape allows it.

### Styles and resources

```csharp
Resources = new ResourceDictionary
{
    ["AccentBrush"] = new SolidColorBrush(Color.Parse("#F59E0B")),
};

Styles.Add(
    new Style<Button>(selector => selector.Class("accent"))
        .Background(MarkupResource.DynamicResource("AccentBrush"))
        .Foreground("#111827")
        .Padding(14, 8)
        .CornerRadius(7));
```

Read a resource once:

```csharp
IBrush brush = this.FindResource<IBrush>("AccentBrush");
```

Use a dynamic resource when the value should update automatically:

```csharp
new Button()
    .Background(MarkupResource.DynamicResource("AccentBrush"));
```

## Hot Reload

AMarkup Hot Reload rebuilds the control tree after the runtime applies a C# hot reload update. It is opt-in and does not require a specific view base class.

### 1. Register the metadata handler

Add this assembly attribute to the executable project:

```csharp
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(AMarkup.MarkupHotReload))]
```

### 2. Enable Hot Reload

```csharp
using AMarkup;

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseMarkupHotReload();
```

### 3. Run with `dotnet watch`

```bash
dotnet watch run --project src/AMarkup.Demo
```

A normal `dotnet run` does not apply changes to a running process.

### Simple window callback

For a window that builds its content in a method:

```csharp
public sealed class MainWindow : Window
{
    public MainWindow()
    {
        this
            .UseMarkupHotReload(Rebuild)
            .OnClosed(() => MarkupHotReload.Unregister(this));

        Rebuild();
    }

    private void Rebuild()
    {
        Content = new TextBlock().Text("Hello");
    }
}
```

### Reusable base classes

A reusable base class can combine lazy construction with attach/detach registration. `MarkupViewLifecycle` handles the differences between windows and child controls.

```csharp
using AMarkup;
using Avalonia.Controls;

public abstract class MarkupWindow : Window, IMarkupReloadable
{
    protected MarkupWindow()
    {
        Lifecycle = new MarkupViewLifecycle(
            this,
            Build,
            content => Content = content);
    }

    protected MarkupViewLifecycle Lifecycle { get; }

    protected abstract Control Build();

    public void ReloadMarkup() => Lifecycle.Reload();
}
```

For a normal child control:

```csharp
public abstract class MarkupUserControl : UserControl, IMarkupReloadable
{
    protected MarkupUserControl(bool rebuildOnReattach = false)
    {
        Lifecycle = new MarkupViewLifecycle(
            this,
            Build,
            content => Content = content,
            rebuildOnReattach);
    }

    protected MarkupViewLifecycle Lifecycle { get; }

    protected abstract Control Build();

    public void ReloadMarkup() => Lifecycle.Reload();
}
```

For a page base. Avalonia 12 includes `ContentPage` and the surrounding navigation controls; use it directly or substitute your navigation framework's page type:

```csharp
public abstract class MarkupContentPage : ContentPage, IMarkupReloadable
{
    protected MarkupContentPage(bool rebuildOnReattach = false)
    {
        Lifecycle = new MarkupViewLifecycle(
            this,
            Build,
            content => Content = content,
            rebuildOnReattach);
    }

    protected MarkupViewLifecycle Lifecycle { get; }

    protected abstract Control Build();

    public void ReloadMarkup() => Lifecycle.Reload();
}
```

Usage:

```csharp
public sealed class HomePage : MarkupContentPage
{
    public HomePage()
        : base(rebuildOnReattach: true)
    {
    }

    protected override Control Build()
    {
        return new StackPanel()
            .Children(new TextBlock().Text("Home"));
    }
}
```

`MarkupViewLifecycle`:

- delays `Build()` until the owner is attached or opened;
- registers with `MarkupHotReload`;
- unregisters on detach or close;
- clears and rebuilds the content on Hot Reload;
- calls `InvalidateMeasure`, `InvalidateArrange`, and `InvalidateVisual` after rebuilding.

`rebuildOnReattach: true` is useful for pages in a navigation stack: if code changes while a page is detached, the page rebuilds when it returns. The trade-off is that it also rebuilds on every reattach, even without a code change.

### Keeping an existing base class

If your class must inherit from an existing `ContentPage`, `UserControl`, or custom base, use composition instead of a new base class:

```csharp
public sealed class HomePage : ContentPage
{
    private readonly MarkupViewLifecycle _lifecycle;

    public HomePage()
    {
        _lifecycle = new MarkupViewLifecycle(
            this,
            Build,
            content => Content = content,
            rebuildOnReattach: true);
    }

    private Control Build()
    {
        return new StackPanel()
            .Children(new TextBlock().Text("Home"));
    }
}
```

This is the recommended option when `ContentPage` is sealed, comes from a third-party package, or is required by a navigation framework that checks the exact page type.

### Why there is no single universal base class

C# has single inheritance. A class cannot derive from both `ContentPage` and `MarkupView`, and a generic base such as `MarkupView<TBase> : TBase` is not legal C#.

For that reason AMarkup provides `MarkupViewLifecycle` rather than forcing every view through one base class. Use separate thin bases (`MarkupWindow`, `MarkupUserControl`, `MarkupContentPage`) or composition, depending on the control hierarchy you need.

## How AMarkup differs from Avalonia.Markup.Declarative

Both projects let you build Avalonia UI in C#, but they have different scope.

### Avalonia.Markup.Declarative

`Avalonia.Markup.Declarative` is a broader, opinionated declarative application framework. It includes:

- `ViewBase` and `ViewBase<TViewModel>` as the central view abstraction;
- lazy view initialization and view lifecycle hooks;
- `ViewFactory` and a view factory registry;
- optional service-provider/DI integration;
- `StyleGroup` and style-building helpers;
- its own `HotReloadManager` integrated with `ViewBase`;
- diagnostics and view-build error wrapping.

Its `ViewBase` is useful for single-file components and a consistent view model workflow. The trade-off is that views are expected to participate in that framework, and a view that must inherit from a specific `ContentPage` base cannot also inherit from `ViewBase`.

### AMarkup

AMarkup is deliberately smaller and more focused:

- It generates fluent extension methods for Avalonia controls rather than defining an application/view framework.
- It does not require `ViewBase`, a marker interface, or a custom control hierarchy.
- It works with `Window`, `UserControl`, `ContentPage`, or any accessible `AvaloniaObject` descendant.
- Hot Reload is opt-in through `MarkupHotReload` and `MarkupViewLifecycle`; it can be attached to an existing base class or used through composition.
- Styles use the typed `Style<TControl>` API rather than a separate style-group abstraction.
- It leaves navigation, DI, view discovery, and application structure to Avalonia and the application itself.

In short:

- Use **Avalonia.Markup.Declarative** if you want a complete declarative view framework with `ViewBase`, view factories, DI, and its associated conventions.
- Use **AMarkup** if you want fluent, generated access to Avalonia controls while keeping the normal Avalonia inheritance and application model.

## Notes

- Generated snapshots currently target Avalonia `12.1.4`; regenerate them when changing the Avalonia surface.
- Hot Reload rebuilds the registered control tree. Local UI state such as selection, focus, and scroll position may reset; keep durable state in the ViewModel.
- `MarkupHotReload` currently reloads all registered controls rather than filtering by changed type. Unregister detached pages and closed windows when possible.
- The generator version is `0.0.1-dev`; APIs may change.
