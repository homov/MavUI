# Md3.Avalonia — Agent Guide

Material Design 3 component library for **Avalonia 12.1.4** / **.NET 10**, written entirely in C#.
There are no AXAML control templates, no resource dictionaries to merge, and no dependencies
beyond Avalonia itself. Every control is drawn/constructed in code.

This file is the single reference for using the library:
§1–§4 are required reading before writing UI code, §5 is a control lookup catalog,
§6 has end-to-end recipes, §7 lists pitfalls, §8 maps the repository.

> Continuing an interrupted session? Read `docs/HANDOFF.md` first: it contains the
> current navigation work state, known testing issues, and the next steps.

---

## 0. Ten rules for agents

1. Call `AppBuilder.UseMd3(...)` once at startup. It registers the bundled fonts and the overlay host.
2. Create controls in C# and assign them to `Content`/`Children`; there are no `ControlTheme`s to look up.
3. Use `MdTheme.Configure(...)` (or the `UseMd3` callback) for all color/type/shape decisions. Never hard-code MD3 colors.
4. Use `MdIcons.*` constants for icon glyphs. Do not paste raw Unicode glyphs.
5. Use `MdLabel` + `MdTypeRole` for typography, not ad-hoc `FontSize` values.
6. Prefer the control's built-in collection helpers (`AddItem`, `AddTab`, `AddDestination`, `AddColumn`, …).
7. For popups/dialogs/sheets, call `Show(anchor)` while the anchor is attached to a live `TopLevel`.
8. For forms, use the inline picker fields (`MdDatePickerField`, `MdTimePickerField`) and `MdFormValidator`.
9. Do not mix `ItemsSource` with manually added children/items; `ItemsSource` owns the child list.
10. Build with `dotnet build Md3.Avalonia/Md3.Avalonia.csproj`; the project targets `net10.0`.

---

## 1. Setup

### 1.1 Project reference

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="12.1.4" />
    <ProjectReference Include="..\Md3.Avalonia\Md3.Avalonia.csproj" />
  </ItemGroup>
</Project>
```

The library assembly/package is `Md3.Avalonia`, version `0.1.0`.

### 1.2 AppBuilder (required)

```csharp
using Avalonia;
using Md3.Avalonia;              // UseMd3 extension, MdIcons, MdLocalization
using Md3.Avalonia.Theming;      // MdTheme, MdThemeOptions, enums

public static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseMd3(opts =>
            {
                opts.SeedColor = MdColors.Purple;      // any seed color
                opts.Mode = MdThemeMode.System;        // System | Light | Dark
                opts.SchemeVariant = MdSchemeVariant.TonalSpot;
            })
            .LogToTrace();
}
```

`UseMd3` does three things:

* calls `MdOverlay.Initialize()` (Back/Escape dismissing for overlays),
* registers the embedded fonts (`fonts:Md3Fonts`),
* optionally applies the theme after setup (`AfterSetup`).

The library ships **no Avalonia control theme**. Use `MdWindow` for desktop windows and
`MdScrollViewer` instead of `ScrollViewer`; both provide their own templates. On Android the
`MdNavigationPage` overlay fallback keeps dialogs, sheets, menus and pickers working even though
the Android `EmbeddableControlRoot` has no themed `OverlayLayer`. If you install an Avalonia theme
yourself, the normal `OverlayLayer` path is used automatically.

On Android, enable system-bar synchronization from the mobile host:

```csharp
var edgeToEdge = (int)Android.OS.Build.VERSION.SdkInt >= 35; // Android 15+
MdSystemBars.Attach(navigation, edgeToEdge: edgeToEdge);
```

Edge-to-edge is an Android 15+ feature; on older releases request it off and let
`MdSystemBars` paint the bars with the current `Surface` color and icon theme. The helper
also re-applies after the native window is ready, so the first frame does not keep the
default bar color. The fallback overlay host is only hit-test-visible while an overlay is
open, so it never swallows pointer input.

If you prefer to configure later, call `UseMd3()` without a callback and then
`MdTheme.Configure(...)` from `App.OnFrameworkInitializationCompleted` or after the main window opens.

```csharp
public class App : Application
{
    public override void Initialize() { }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
```

### 1.3 Minimal window

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

public class MainWindow : MdWindow
{
    public MainWindow()
    {
        Title = "Md3.Avalonia";
        Width = 1000;
        Height = 700;

        var save = new MdButton("Save", MdButtonVariant.Filled, MdIcons.Save);
        save.Clicked += (_, _) => MdSnackbar.Show("Saved");

        Content = new MdScrollViewer
        {
            Padding = new Thickness(24),
            Content = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    new MdLabel
                    {
                        Text = "Dashboard",
                        Role = MdTypeRole.HeadlineMedium,
                    },
                    new MdTextField("Name", placeholder: "Ada Lovelace"),
                    save,
                },
            },
        };
    }
}
```

### 1.4 AXAML usage (optional)

The library exposes the XML namespace `https://github.com/homov/md3-maui` with prefix `md3`.
All controls work in AXAML even though the implementation is C#.

```xml
<md3:MdWindow xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:md3="https://github.com/homov/md3-maui"
              x:Class="MyApp.MainWindow">
  <md3:MdWindow.Styles>
    <md3:Md3Theme SeedColor="#6750A4" Mode="System"
                  Variant="TonalSpot" Density="Comfortable" />
  </md3:MdWindow.Styles>

  <md3:MdScrollViewer Padding="24">
    <StackPanel Spacing="16">
      <md3:MdLabel Text="Hello" Role="HeadlineMedium" />
      <md3:MdButton Text="Save" Icon="{x:Static md3:MdIcons.Save}" />
    </StackPanel>
  </md3:MdScrollViewer>
</md3:MdWindow>
```

`Md3Theme` is a `Style` that forwards its properties to the static `MdTheme` state, so it is
equivalent to calling `MdTheme.Configure`. You still need `UseMd3()` in `Program.cs` for fonts.
The base `Window` from `https://github.com/avaloniaui` has no template without an Avalonia theme,
so prefer `md3:MdWindow` and `md3:MdScrollViewer` in theme-free apps.

### 1.5 Fonts and icons

Bundled fonts:

| Font | Access | Notes |
|---|---|---|
| Material Icons | `MdFonts.MaterialIconsFont` | classic icon set |
| Material Symbols Rounded | `MdFonts.MaterialSymbolsFont` | variable-style symbols |
| Material Design Icons | `MdFonts.MaterialDesignIconsFont` | large community set |
| Roboto | `MdFonts.RobotoFont` | default text font |

* `MdFonts.ResolveIconFont(glyph)` / `MdFonts.ResolveIconTypeface(glyph)` pick the right family by codepoint.
* `MdIcon` does this automatically; normally you only pass `MdIcons.*`.
* `MdIcons` contains ~10,800 constants (e.g. `MdIcons.Home`, `MdIcons.Settings`, `MdIcons.ArrowBack`,
  `MdIcons.Favorite`, `MdIcons.Add`, `MdIcons.Check`, `MdIcons.Close`, `MdIcons.Delete`,
  `MdIcons.Search`, `MdIcons.Menu`, `MdIcons.MoreVert`, `MdIcons.ChevronRight`).
* Use `<md3:MdIcon Glyph="{x:Static md3:MdIcons.Home}" Size="24" />` or `new MdIcon(MdIcons.Home, 24)`.

Icon font sets are chosen at **build time** with the `Md3Fonts` MSBuild property:

| `Md3Fonts` | Embedded | `Md3.Avalonia.dll` |
|---|---|---|
| `all` (default) | Roboto + Symbols + Material Icons + Material Design Icons | ~4.87 MB |
| `no-mdi` | Roboto + Symbols + Material Icons | ~3.69 MB |
| `minimal` | Roboto + Material Symbols only | ~3.35 MB |

```bash
dotnet build Md3.Avalonia/Md3.Avalonia.csproj -p:Md3Fonts=no-mdi
```

`no-mdi` is the safe size reduction for apps that use classic `MdIcons` (the demo).
`minimal` only works for Material Symbols glyphs; classic icon codepoints (`MdIcons.Home`, …)
will fail to create a glyph typeface if the classic font is excluded.

### 1.7 Trimming and AOT

The library sets `IsTrimmable` and `IsAotCompatible` and builds without trim warnings.
Trimmed/AOT apps work, with one caveat: `DisplayMemberPath` and `MdDataColumn.PropertyName`
use reflection on application model types. In trimmed apps either root those properties
(`DynamicDependency`/`rd.xml`) or use `ItemTemplate`/`ItemFactory` and
`MdDataColumn.ValueSelector`, which are trim-safe.

Android Release publish (trimming and AOT are on by default in Release):

```bash
dotnet publish samples/Demo.Android/Demo.Android.csproj -c Release \
    -p:PublishTrimmed=true -p:Md3Fonts=no-mdi \
    -p:AndroidPackageFormats=apk -o artifacts/android
```

Measured on the demo: 65.60 MB (no trim/AOT) → 27.71 MB (trimmed+AOT, `no-mdi`).

### 1.6 Localization

Picker dialogs, tooltips, accessibility labels and built-in action buttons use
`MdLocalization`. Apps configure it **once** and never subscribe to events.

#### Provider

Point the library at your resources at startup. Missing keys fall back to the
English built-ins, so you only need to translate what you use:

```csharp
using System.Globalization;
using System.Resources;

// generated .resx designer class
MdLocalization.Current = new MdLocalization.ResourceManagerProvider(
    Strings.ResourceManager);
```

Other providers:

```csharp
// small dictionary / tests
MdLocalization.Current = new MdLocalization.DictionaryProvider(
    new Dictionary<string, string> { ["Ok"] = "Хорошо" });

// custom source (JSON, database, …); return null for a missing key
MdLocalization.Current = new MdLocalization.DelegateProvider(
    (key, culture) => MyCatalog.Lookup(key, culture));

// app resources first, then another provider
MdLocalization.Current = new MdLocalization.ChainedProvider(
    appProvider, new MdLocalization.DefaultProvider());
```

The provider contract is `string? GetString(string key, CultureInfo? culture)`.
Return `null`/empty for a missing key so `MdLocalization.Get(..., fallback)` can
use its fallback. All keys are available as constants in `MdLocalization.Keys`
(`Keys.Ok`, `Keys.SelectDate`, `Keys.PreviousPage`, …).

#### Switching language

```csharp
// also updates CurrentCulture for dates/numbers
MdLocalization.SetCulture(new CultureInfo("ru-RU"));

// UI strings only, keep the formatting culture
MdLocalization.SetCulture(new CultureInfo("ru-RU"), setFormattingCulture: false);
```

`SetCulture` refreshes every observed text automatically. If the culture or the
provider data is changed outside the library, call `MdLocalization.Refresh()`.

#### Application strings

Attach once; no event handlers or manual re-assignment:

```csharp
var title = new TextBlock().Localize("Title", "Control gallery");
var save = new MdButton("Save", MdButtonVariant.Filled).Localize("Save");
var window = new Window().Localize("AppTitle", "My app");

var name = new MdTextField()
    .LocalizeLabel("Name")
    .LocalizePlaceholder("NameHint");
```

`Localize` supports `TextBlock`, `MdButton` and `Window`; `LocalizeLabel` /
`LocalizePlaceholder` handle `MdTextField`. For any other property use the generic
observer:

```csharp
MdLocalization.Observe(statusLabel, label =>
    label.Text = MdLocalization.Get("Status", "Ready"));
```

Observers run immediately, re-run when the provider/culture changes while the
control is attached, and unsubscribe when it leaves the visual tree. Complex
strings can use `MdLocalization.Format("Rows", "{0} rows", count)`.

#### AXAML

The shortest form is the `{md:Loc ...}` markup extension. It works on any
property and updates itself after `SetCulture` or a provider change:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:md="https://github.com/homov/md3-maui"
        Title="{md:Loc AppTitle, Fallback=My app}">
  <StackPanel>
    <TextBlock Text="{md:Loc Greeting, Fallback=Hello}" />
    <md:MdButton Text="{md:Loc Save}" />
    <md:MdTextField Label="{md:Loc Name}" />
    <md:MdTextField Placeholder="{md:Loc NameHint}" />
    <Button ToolTip.Tip="{md:Loc MoreOptions}" />
  </StackPanel>
</Window>
```

`Fallback` is optional. No `Target` property is needed: the markup extension
knows the exact target property from XAML.

For scenarios where the key must be changed at runtime, the attached properties
`md:MdLocalization.Key` / `.Fallback` / `.Target` remain available. `Target=Auto`
(default) maps a `TextBlock` to `Text`, `MdButton` to `Text`,
`Window`/`MdDialog` to `Title`, `MdTextField` to `Label`, `MdSelect` to `Label`,
and `MdSearchBar` to `Placeholder`. Use `Text`, `Label`, `Placeholder`, `Title`
or `Tooltip` when the natural property is not the one you want.

`MdLocalization.Changed` still exists for advanced scenarios, but application
code does not need it. Built-in dialogs read the current language when they open;
dynamic defaults (e.g. a dialog's OK button) are resolved at show time.

---

## 2. Core conventions

### 2.1 Namespaces

```csharp
using Md3.Avalonia;             // MdFonts, MdIcons, MdColors, MdLocalization
using Md3.Avalonia.Controls;    // all UI controls
using Md3.Avalonia.Primitives;  // MdContentView, MdSurface, MdInkSurface, MdIcon
using Md3.Avalonia.Theming;     // MdTheme, tokens, typography, motion
using Md3.Avalonia.Platform;    // MdOverlay, native inputs
```

`MdColors` and `Md3Theme` both live in `Md3.Avalonia.Theming`.

### 2.2 `MdContentView` — the single-child base

Most controls derive from `MdContentView`, which is a `Border` with one child:

```csharp
var card = new MdCard(variant: MdCardVariant.Filled)
{
    Content = new MdLabel { Text = "Body" },
    BackgroundColor = MdTheme.ColorOf(MdColorRole.SurfaceContainer),
};
```

* `Content` is the child (same as `Child`).
* `BackgroundColor` is a convenience wrapper over `Background`; transparent means `null`.
* Custom-drawn controls (`MdSwitch`, `MdSlider`, `MdCarousel`, `MdTabs`, `MdSparkline`, …)
  derive from `Control` and override `Render`; they have no `Content` property.

Useful containers:

| Type | Purpose |
|---|---|
| `MdSurface` | rounded fill + optional surface tint + border + MD3 two-layer elevation |
| `MdInkSurface` | clickable surface with ripple, hover/focus/pressed state layer, `Command` |
| `MdSafeArea` | applies safe-area insets (`Edges`, `SimulatedInset`, `BasePadding`) |
| `MdKeyboardAvoidingHost` | shifts content above the soft keyboard and auto-scrolls the focused field |
| `MdScrollViewer` | theme-independent `ScrollViewer`; touch/wheel/keyboard scrolling, no visible scrollbars |
| `MdWindow` | desktop window with a built-in theme-free template and overlay layer |

### 2.3 Events, commands, and property changes

* Every clickable control exposes both a CLR event (e.g. `Clicked`, `Toggled`, `SelectedIndexChanged`)
  and an `ICommand` (`Command` + `CommandParameter`). **The event fires first, then the command.**
* Programmatic property changes can raise change events. In particular `MdTextField.TextChanged`,
  `MdTextArea.TextChanged`, `MdSearchBar.TextChanged`, `MdPinInput.CodeChanged`, and
  `MdFormValidator` subscribed fields fire for both user and code changes. Guard with a flag if needed.
* Use direct collection properties when building UI in code (`MdTabs.Tabs`, `MdStepper.Steps`,
  `MdChipGroup.Chips`, `MdList` children, `MdDataTable.Columns`, …). They are `AvaloniaList<T>`
  and also implement `IEnumerable<T>` with fluent `Add*` helpers.

### 2.4 `ItemsSource` behavior

Controls that accept `ItemsSource` (list, tabs, chips, table, tree, carousel, combobox, select,
navigation bars, etc.) **rebuild their children when the source or its collection changes**.
Consequences:

* Do not mix `ItemsSource` with manually added items/children — the rebuild replaces them.
* `ItemsSource` must be set on the UI thread.
* If the source is `INotifyCollectionChanged`, mutations rebuild the whole child list (no per-item
  virtualization or diffing).
* `ItemTemplate` / `ItemFactory` is applied during rebuild; changing the template alone may require
  re-assigning `ItemsSource`.

### 2.5 Shape and layout helpers

* `MdGridExtensions.Add(child, column, row, columnSpan, rowSpan)` avoids repetitive `Grid.Set*` calls.
* `MdMotion` extension methods (`SetTranslation`, `SetScale`, `SetRotation`) attach a shared
  `TransformGroup` to a `Visual`.
* Use `Spacing` on `StackPanel`s and `ColumnSpacing`/`RowSpacing` on `Grid`s for MD3 rhythm.

### 2.6 Accessibility

Md3 controls fill Avalonia automation properties from their visible text, so screen readers
announce meaningful names without extra code:

* buttons use `Text`; icon-only buttons (`MdIconButton`, `MdFab`) use the tooltip when set;
* fields use `Label` as `Name` and the footer (`ErrorText`, else `SupportingText`) as `HelpText`;
* toggles and selection controls expose their `Label` and the matching role
  (`CheckBox`, `RadioButton`, `Slider`, `ComboBox`, `ListItem`, `MenuItem`);
* `MdListItem` exposes `Headline` + `SupportingText`, dialogs/sheets expose `Title`/`Message`;
* `MdValidation.SetRequired(control, true)` also sets `IsRequiredForForm`.

Use the helpers when building custom UI:

```csharp
MdAutomation.SetName(control, "Delete project");
MdAutomation.SetHelpText(control, "This action cannot be undone");
MdAutomation.SetRequired(control, true);
MdAutomation.SetControlType(control, AutomationControlType.Button);
```

Explicit `AutomationProperties` values set by the application are never overwritten. Keyboard
activation is built in: Space/Enter on buttons, chips and list/menu items (via `MdInkSurface`),
Space/Enter on switches/checkboxes/radios, and arrow keys on sliders. `MdInkSurface` draws the
MD3 focus ring only for keyboard focus.

Contrast helpers live in `Md3.Avalonia.Theming.MdAccessibility`:

```csharp
double ratio = MdAccessibility.ContrastRatio(theme.Color.OnSurface, theme.Color.Surface);
bool ok = MdAccessibility.MeetsContrast(fg, bg);              // WCAG AA body text (4.5)
bool largeOk = MdAccessibility.MeetsContrast(fg, bg, 3.0);    // large text / UI (3.0)
```

### 2.7 Gesture zones

On Android, horizontal drags must not steal the system back gesture. `MdGestureZones`
reserves the first/last 20 dp of the top level:

```csharp
if (MdGestureZones.ShouldDeferToSystem(this, e))
    return; // let TopLevel.BackRequested / the platform gesture win

bool atEdge = MdGestureZones.IsInSystemEdge(visual, point);            // 20 dp default
bool custom = MdGestureZones.IsInSystemEdge(visual, point, edgeWidth: 28);
```

Built-in draggable controls already honor it: `MdListItem` swipe actions, `MdCarousel`,
`MdSlider`/`MdRangeSlider`, `MdSplitView`, `MdColorPicker`, `MdTimeDial`, `MdRadialGauge`.
Mouse input is not constrained (desktop).

---

## 3. Theming

### 3.1 Configure

```csharp
MdTheme.Configure(
    seed: Color.Parse("#006A6A"),
    mode: MdThemeMode.System,
    contrast: MdContrast.Standard,
    density: MdDensity.Comfortable,
    useSystemAccentColor: false,
    schemeVariant: MdSchemeVariant.TonalSpot,
    motion: MdMotionScheme.Expressive,
    shapeScale: MdShapeScale.Standard);
```

Equivalent object/fluent forms:

```csharp
var options = new MdThemeOptions
{
    SeedColor = MdColors.Teal,
    Mode = MdThemeMode.Dark,
    SchemeVariant = MdSchemeVariant.Vibrant,
    Contrast = MdContrast.Medium,
    Density = MdDensity.Compact,
    Motion = MdMotionScheme.Reduced,
    ShapeScale = MdShapeScale.Expressive,
};
MdTheme.Configure(options);

// or
MdTheme.Configure(o =>
{
    o.SeedColor = MdColors.Indigo;
    o.Mode = MdThemeMode.Light;
});
```

All configuration is global and rebuilds the palette immediately. Call it on the UI thread.

Convenience mode switches:

```csharp
MdTheme.UseLight();
MdTheme.UseDark();
MdTheme.UseSystemMode();
MdTheme.ToggleDark();
```

### 3.2 `MdTheme` API

| Member | Meaning |
|---|---|
| `MdTheme.Current` | current `MdTheme` snapshot |
| `MdTheme.Current.Color` | `MdColorScheme` with all roles |
| `MdTheme.Current.Typography` | 15-role type scale |
| `MdTheme.Current.Shape` | `MdShapeTokens` (corner radii) |
| `MdTheme.Current.Elevation` | `MdElevationTokens` (levels 0–5, `ToBoxShadows`) |
| `MdTheme.Current.StateLayer` | hover/focus/pressed/dragged/disabled opacities |
| `MdTheme.Current.IsDark` | resolved darkness |
| `MdTheme.ColorOf(role)` / `BrushOf(role)` | cached color/brush lookup |
| `BrushOf(Color)` / `PenOf(Color, thickness)` | cached brushes/pens for custom drawing |
| `MdTheme.Changed` | event fired after every rebuild |
| `MdTheme.Observe(view, (v, theme) => …)` | run now and on every theme change while attached |
| `MdTheme.ApplySeedFromPixels(...)` | extract a seed color from ARGB pixels |
| `MdTheme.ApplySeedFromRgba(...)` | extract a seed color from RGBA bytes |
| `MdTheme.ApplyCustomSchemes(light, dark)` | override both palettes |
| `MdTheme.ClearCustomSchemes()` | return to generated palettes |

`MdTheme.Changed` fires after every rebuild. For MVVM/DI, `MdThemeService.Instance` (`IMdThemeService`)
wraps the same state with `INotifyPropertyChanged` and a `ThemeChanged` event.

**Drawing custom controls:** always read colors from `MdTheme.Current` inside `Render`, or subscribe
with `MdTheme.Observe` and call `InvalidateVisual()`. Cache brushes via `MdTheme.BrushOf`, not `new SolidColorBrush`.

```csharp
public class AccentBar : Control
{
    public override void Render(DrawingContext context)
    {
        var c = MdTheme.Current.Color;
        context.DrawRectangle(MdTheme.BrushOf(c.PrimaryContainer), null,
            new Rect(0, 0, Bounds.Width, Bounds.Height), 8, 8);
    }
}
```

### 3.3 Color roles

`MdColorScheme` exposes Material 3 roles. `Get(role)` and the indexer return the current color;
`SurfaceAtElevation(int)` returns the tinted surface for elevation levels.

Primary group: `Primary`, `OnPrimary`, `PrimaryContainer`, `OnPrimaryContainer`,
`PrimaryFixed`, `PrimaryFixedDim`, `OnPrimaryFixed`, `OnPrimaryFixedVariant`.

Secondary group: `Secondary`, `OnSecondary`, `SecondaryContainer`, `OnSecondaryContainer`,
`SecondaryFixed`, `SecondaryFixedDim`, `OnSecondaryFixed`, `OnSecondaryFixedVariant`.

Tertiary group: `Tertiary`, `OnTertiary`, `TertiaryContainer`, `OnTertiaryContainer`,
`TertiaryFixed`, `TertiaryFixedDim`, `OnTertiaryFixed`, `OnTertiaryFixedVariant`.

Error: `Error`, `OnError`, `ErrorContainer`, `OnErrorContainer`.

Neutral/surface: `Background`, `OnBackground`, `Surface`, `OnSurface`, `SurfaceVariant`,
`OnSurfaceVariant`, `SurfaceDim`, `SurfaceBright`, `SurfaceContainerLowest`,
`SurfaceContainerLow`, `SurfaceContainer`, `SurfaceContainerHigh`, `SurfaceContainerHighest`,
`Outline`, `OutlineVariant`, `Shadow`, `Scrim`, `InverseSurface`, `InverseOnSurface`,
`InversePrimary`, `SurfaceTint`.

Rules of thumb:

* page background → `Background` or `Surface`
* cards/panels → `SurfaceContainerLow`…`Highest`
* text on surface → `OnSurface`; secondary text → `OnSurfaceVariant`
* filled buttons → `Primary` / `OnPrimary`
* destructive actions → `Error` / `OnError`

### 3.4 Scheme variants and contrast

`MdSchemeVariant`: `TonalSpot` (default), `Neutral`, `Vibrant`, `Expressive`, `Monochrome`,
`Fidelity`, `Content`, `Rainbow`, `FruitSalad`.

`MdContrast`: `Standard`, `Medium`, `High`.

`MdColorScheme.FromSeed(seed, dark, contrast, variant)` generates a full palette manually.
`MdColorScheme.FromImagePixels(...)` / `MdColorScheme.ExtractSeedFromPixels(...)` derive a seed
from an image. `MdColorScheme.Blend(a, b, amount)` blends colors.

Custom schemes:

```csharp
var light = MdColorScheme.FromSeed(MdColors.Green, dark: false);
var dark  = MdColorScheme.FromSeed(MdColors.Green, dark: true);
MdTheme.ApplyCustomSchemes(light, dark);
```

### 3.5 Typography

15 roles in `MdTypeRole`:
`DisplayLarge/Medium/Small`, `HeadlineLarge/Medium/Small`, `TitleLarge/Medium/Small`,
`BodyLarge/Medium/Small`, `LabelLarge/Medium/Small`.

```csharp
var title = new MdLabel
{
    Text = "Section",
    Role = MdTypeRole.TitleLarge,
    ColorRole = MdColorRole.Primary,
};

// For any TextBlock:
var text = new TextBlock { Text = "Body copy" };
text.Apply(MdTypeRole.BodyMedium);

// Or apply a full style:
MdTypography.Default.BodyMedium.ApplyTo(text);
```

`MdLabel` is a `TextBlock` with `Role`, `ColorRole`, `Secondary` (uses `OnSurfaceVariant`), and
`HorizontalTextAlignment`. `MdTypography.BrandFont` / `PlainFont` are `init`-only; the default is Roboto.

### 3.6 Shape, elevation, state, density

`MdTheme.Current.Shape` (an `MdShapeTokens`):

| Token | Standard value |
|---|---|
| `None` | 0 |
| `ExtraSmall` | 4 |
| `Small` | 8 |
| `Medium` | 12 |
| `Large` | 16 |
| `LargeIncreased` | 20 |
| `ExtraLarge` | 28 |
| `Full` | 999 |

`MdShapeScale`: `Compact` (×0.72), `Standard` (×1.0), `Expressive` (×1.28).

`MdTheme.Current.Elevation`: `Level0`=0, `Level1`=1, `Level2`=3, `Level3`=6,
`Level4`=8, `Level5`=12. `MdElevationTokens.ToBoxShadows(level)` builds the two-layer shadow.

`MdTheme.Current.StateLayer`: `Hover`=0.08, `Focus`=0.10, `Pressed`=0.12,
`Dragged`=0.16, `DisabledContainer`=0.12, `DisabledContent`=0.38.

`MdDensity`: `Comfortable` (0), `Compact` (-1), `Dense` (-2). It is stored on `MdTheme.Current.ScaleDensity`
for controls that consult it; do not assume every control changes size.

### 3.7 Motion

`MdMotion` constants: `Short1..4` (50/100/150/200 ms), `Medium1..4` (250/300/350/400),
`Long1..4` (450/500/550/600), `ExtraLong1..4` (700/800/900/1000).

Easings: `Linear`, `CubicIn`, `CubicOut`, `CubicInOut`, `SinInOut`, `SpringOut`,
`Standard`, `StandardDecelerate`, `StandardAccelerate`, `EmphasizedDecelerate`,
`EmphasizedAccelerate`.

`MdMotionScheme`: `Standard`, `Expressive` (default), `Reduced`, `None`.
`Reduced` caps durations at ~90 ms; `None` completes instantly.

```csharp
MdMotion.Animate(control, "fade", 0, 1, MdMotion.Medium2, MdMotion.CubicOut,
    v => control.Opacity = v);

MdMotion.FadeIn(control);
MdMotion.ScaleIn(control, from: 0.94);
control.SetTranslation(0, 8);
control.SetScale(1.1);
```

### 3.8 Theme resources for AXAML

The theme mirrors its values into `Application.Current.Resources` on every rebuild:

* `Md.Color.<Role>` — `Color` (e.g. `Md.Color.Primary`)
* `Md.Brush.<Role>` — `IImmutableSolidColorBrush`
* `Md.Theme.IsDark` — `bool`
* `Md.Shape.<Name>` — `double` (`Md.Shape.Medium`, `Md.Shape.Full`, …)

It also sets Avalonia integration keys such as `NavigationBarBackground`, `NavigationBarForeground`,
`SystemRegionBrush`, and `TextControlSelectionHighlightColor`.

```xml
<Border Background="{DynamicResource Md.Brush.SurfaceContainerHigh}"
        CornerRadius="{DynamicResource Md.Shape.Medium}" />
```

---

## 4. Overlays and transient UI

### 4.1 How the overlay works

`MdOverlay` uses the current `TopLevel`'s `OverlayLayer`. When the top level has no themed
`OverlayLayer` (Android without an Avalonia theme, or a headless window), it falls back to the
overlay host registered by the nearest `MdNavigationPage`.

* `MdOverlay.Show(anchor, content, scrimColor, onScrimTap, passThroughScrim, onViewportChanged)`
  adds full-window content and returns an `IDisposable` that closes it.
* `onViewportChanged` is invoked after a window resize, `ScalingChanged`,
  `InsetsManager.SafeAreaChanged`, `IInputPane.StateChanged` (soft keyboard) and root
  `SizeChanged`, coalesced through `Dispatcher.UIThread.Post`. Open overlays re-clamp
  themselves without re-running entrance animations; pass it for custom overlay content
  that computes geometry once in `Show`.
* `MdOverlay.ShowAnchoredPopup(anchor, popupCard, preferredWidth, maxHeight, verticalOffset, onDismiss)`
  clamps to safe-area bounds, flips above the anchor when needed and re-places itself on
  viewport changes.
* `MdOverlay.SafeAreaOf(visual)` returns the top level's `IInsetsManager.SafeAreaPadding`;
  `MdOverlay.SafeAreaBounds(visual)` returns the overlay rect without insets.
  `BoundsInOverlay(visual, root)` gives anchor coordinates in overlay space.
* The soft keyboard lifts the overlay root (real `IInputPane`, or
  `MdKeyboardAvoidingHost.SimulatedKeyboardHeight` / `MD3_KEYBOARD` on desktop).
* Escape / hardware Back dismisses the most recent dismissable overlay (`PushDismissable`).
* The anchor may be any attached `Visual`; if it is `null`, the last known `TopLevel` is used.
  Call overlay APIs only after a window/view exists — not in `Application.Initialize()`.

**Popup controls are launcher handles, not layout children.** `MdDialog`, `MdBottomSheet`,
`MdSideSheet`, `MdPopover`, `MdMenu`, `MdRichTooltip`, `MdDatePicker`, `MdTimePicker`,
`MdSearchView`, and `MdModalNavigationDrawer` are zero-size `Control`s whose `Show(anchor)`
method places their UI in the overlay. Do **not** add them to a panel expecting visible content.

```csharp
var dialog = new MdDialog
{
    Title = "Discard changes?",
    Message = "Your edits will be lost.",
    ConfirmText = "Discard",
    DismissText = "Keep editing",
};
dialog.Show(this);               // this = Window/Control currently in the tree
```

If the anchor is detached (e.g. a page that was navigated away), keep a reference to the live
window or call `MdOverlay.TrackTopLevel(liveControl)` first.

### 4.2 Dialogs

Instance API:

```csharp
var dialog = new MdDialog
{
    Icon = MdIcons.Warning,
    Title = "Delete project?",
    Message = "This action cannot be undone.",
    ConfirmText = "Delete",
    DismissText = "Cancel",
    NeutralText = "Archive",
    DismissOnScrimTap = false,
    Body = new MdCheckBox("Also delete files"),
};
dialog.Confirmed += (_, _) => Delete();
dialog.Dismissed += (_, _) => { };
dialog.NeutralClicked += (_, _) => Archive();
dialog.Show(anchor);
// dialog.Close();
```

Other members: `HeroIcon`, `Headline`, `SupportingText`, `IsOpen`, `ConfirmCommand`,
`DismissCommand`, `NeutralCommand` (+ parameter variants), `Closed`.

The dialog is adaptive: it fills the width on narrow screens (24 dp margins plus the
current safe-area insets) and is capped at 560 dp on desktop/tablet. Its body scrolls
when the content is taller than the overlay, so long messages and forms stay usable.

Static helpers (use the current/last TopLevel, or pass an anchor):

```csharp
await MdDialog.ShowAlertAsync("Saved", "Your changes are now live.", "OK", MdIcons.Check);

bool ok = await MdDialog.ShowConfirmAsync(
    "Publish?", "This will make the page public.", "Publish", "Cancel", MdIcons.Public);

using IDisposable full = MdDialog.ShowFullscreen(
    "Editor", editorControl, "Done", () => Save());
```

`MdDialogService.Instance` exposes `ShowAlertAsync`/`ShowConfirmAsync` without anchor arguments
for view-model code (`IMdDialogService`).

### 4.3 Sheets

```csharp
var sheet = new MdBottomSheet
{
    Title = "Filters",
    Body = filterPanel,
    ShowDragHandle = true,
    DismissOnScrimTap = true,
};
sheet.Closed += (_, _) => { };
sheet.Show(anchor);       // or sheet.ShowCurrent(title, body)
// sheet.Close();
```

`MdSideSheet` adds `Footer`, `Placement` (`Start`/`End`), `SheetWidth`, `ShowBackButton`,
`BackCommand`, `BackClicked`, `Closed`, and `Show`/`ShowCurrent` overloads.

### 4.4 Menus, popovers, rich tooltips

```csharp
var menu = new MdMenu()
    .AddItem("Rename", MdIcons.Edit, onSelected: Rename)
    .AddItem("Duplicate", MdIcons.ContentCopy, onSelected: Duplicate)
    .AddDivider()
    .AddItem("Delete", MdIcons.Delete, onSelected: Delete);
menu.ItemSelected += (_, item) => { };
menu.Show(anchor);       // or menu.Close();
```

Nested menus: `AddSubmenu("Move to", sub => sub.AddItem("Archive"), MdIcons.DriveFileMove)`.
Each `MdMenuItem` also supports `Command`, `IsSelected`, `IsDivider`, `TrailingText`, and `SubItems`.

Menus and popovers clamp to the current overlay: menus keep an 8 dp edge margin, popovers
12 dp, and both measure their body to pick a natural width, so they stay inside a 320 dp
phone viewport. A tall body scrolls inside the card instead of growing past the overlay.

```csharp
var popover = new MdPopover
{
    Title = "Details",
    Content = detailsPanel,
    Placement = MdPopoverPlacement.Bottom,
    DismissOnOutsideTap = true,
};
popover.Opened += (_, _) => { };
popover.Closed += (_, _) => { };
popover.Show(anchor);    // Toggle(anchor) / Close()
```

`MdRichTooltip`:

```csharp
MdRichTooltip.Show(anchor, "Keyboard shortcut", "Ctrl+K opens search",
    actionText: "Got it", onAction: null);
```

Use `MdFlyout.SetMenu(button, menu)` (or `Popover`, `RichTooltip`, `BottomSheet`, `SideSheet`,
`Dialog`, `ModalDrawer`) to open declaratively when a button/chip/list-item/fab is clicked.
This is the recommended AXAML-friendly alternative to manual `Show` calls.

### 4.5 Snackbar

`MdSnackbar` is a **static global FIFO**; only one snackbar is visible at a time.

```csharp
MdSnackbar.Show("Message deleted", "Undo", () => Restore(), duration: MdSnackbarDuration.Short);

// Queue instead of replacing:
MdSnackbar.Enqueue("Uploading…", duration: MdSnackbarDuration.Indefinite);
MdSnackbar.Dismiss();
MdSnackbar.ClearQueue();
int pending = MdSnackbar.QueueCount;
```

* `Show` clears the queue and replaces the current snackbar.
* `Enqueue` appends; if nothing is active it shows immediately.
* Durations: `Short` (3000 ms), `Long` (5500 ms), `Indefinite` (0 = stays until dismissed).
  Indefinite snackbars always render a close icon, even when `showCloseIcon` is not set, so
  they can be dismissed on touch and web where there is no Escape key.
* `MdSnackbarService.Instance` provides the same API for view models (`IMdSnackbarService`).

### 4.6 Simple tooltips

```csharp
MdTooltip.SetText(saveButton, "Save (Ctrl+S)");
MdTooltip.SetPlacement(saveButton, MdTooltipPlacement.Bottom);
MdTooltip.Attach(saveButton, "Save", MdTooltipPlacement.Top);
MdTooltip.Show(anchor, "Copied", durationMs: 1500);
```

`MdTooltip` has one global handle; a new tooltip replaces the visible one.

### 4.7 Search

```csharp
var search = new MdSearchBar
{
    Placeholder = "Search…",
    Text = "",
};
search.SearchSubmitted += (_, query) => RunSearch(query);
search.TextChanged += (_, query) => Filter(query);

MdSearchView view = search.AttachSearchView(new[] { "Inbox", "Archive" }, fullScreen: false);
```

Or use `MdSearchView` directly with `Suggestions`, `SuggestionsSource`, `ResultItemTemplate`,
`RecentSearches`, `QueryChanged`, `QuerySubmitted`, `ResultSelected`, `Show`/`Close`.

---

## 5. Control catalog

### 5.1 Buttons and actions

| Control | Purpose and key API |
|---|---|
| `MdButton` | text/icon button. `new MdButton(text, variant, icon, size)`; `Variant` (`Filled`, `Tonal`, `Elevated`, `Outlined`, `Text`), `Size` (`ExtraSmall`…`ExtraLarge`), `Icon`, `TrailingIcon`, `ButtonShape`/`Shape` (`Round`/`Square`), `IsToggle`, `IsSelected`, `IsLoading`, `Command`, `Clicked`, `SelectedChanged`, `Toggled`. |
| `MdIconButton` | icon-only button. `new MdIconButton(icon, variant, isToggle)`; `SelectedIcon`, `ButtonSize`, `TouchTargetSize` (48 dp by default, the clickable area even when the visual is smaller). |
| `MdFab` | floating action button. `new MdFab(icon, size, color)`; `Size` (`Small`/`Regular`/`Large`), `ColorScheme` (`Primary`/`Secondary`/`Tertiary`/`Surface`). |
| `MdExtendedFab` | FAB with label. `Text`, `Icon`, `IsExpanded`. |
| `MdButtonGroup` | `StackPanel` of buttons. `Kind`/`Variant` (`Standard`/`Connected`), `MultiSelect`, `AddButton`, `SelectionChanged(int)`. |
| `MdSplitButton` | primary action + menu arrow. `Menu` (`MdMenu`), `IsMenuOpen`, `MenuCommand`, `Clicked`, `MenuClicked`. |
| `MdFabMenu` | FAB that expands into labeled actions. `AddItem(label, icon, onClick)`, `Open`/`Close`/`Toggle`, `ItemClicked`, `OpenChanged`. |

```csharp
var more = new MdIconButton(MdIcons.MoreVert);
more.Clicked += (_, _) => menu.Show(more);

var fab = new MdFab(MdIcons.Add, MdFabSize.Regular, MdFabColor.Primary);
fab.Clicked += (_, _) => CreateItem();

var group = new MdButtonGroup { Kind = MdButtonGroupKind.Connected, MultiSelect = false };
group.AddButton(new MdButton("Day", MdButtonVariant.Outlined));
group.AddButton(new MdButton("Week", MdButtonVariant.Outlined));
```

### 5.2 Text input and selection

| Control | Purpose and key API |
|---|---|
| `MdTextField` | single-line text field. `new MdTextField(label, variant, placeholder, leadingIcon, supportingText)`; `Text`, `Placeholder`, `Variant` (`Filled`/`Outlined`), `LeadingIcon`, `TrailingIcon`, `PrefixText`, `SuffixText`, `ErrorText`, `HasError` (also true when `ErrorText` set), `IsPassword`, `IsReadOnly`, `ShowClearButton`, `MaxLength`, `ShowCounter`/`ShowCharacterCounter`, `ContentType`, `ReturnKeyType`, `AutoCapitalize`, `Completed`, `TrailingIconClicked`, `Focus()`/`Unfocus()`. |
| `MdTextArea` | multiline field. `new MdTextArea(label, variant, placeholder, supportingText, minHeight)`; `MaxLength`, `ShowCounter`, `MinBoxHeight`, `TextChanged`; sends `ReturnKeyType`/newline as text input options. |
| `MdPinInput` | PIN/OTP boxes. `Length`, `Code`, `Value`, `Text` (aliases), `IsMasked`, `HasError`, `CodeChanged`, `Completed`, `Clear()`; numeric keyboard and `Done` return key. |
| `MdNumberBox` | numeric stepper. `Value`, `Minimum`, `Maximum`, `Step`, `DecimalPlaces`, `Label`, `Suffix`, `FormatString`, `ValueChanged`, `Increment`, `Decrement`. |
| `MdTagInput` | chip/tag entry. `Tags`, `Label`, `Placeholder`, `MaxTags`, `AllowDuplicates`, `TagAdded`, `TagRemoved`, `TagsChanged`, `AddTag`, `RemoveTag`, `ClearTags`. |
| `MdRatingBar` | star rating. `Value`, `MaxRating`, `AllowHalf`/`AllowHalfStars`, `IsReadOnly`, `IconSize`/`ItemSize`, `ValueChanged`. |
| `MdSwitch` | toggle. **`IsToggled`** (not `IsOn`), `ShowIcons`, `OnIcon`, `OffIcon`, `Toggled(bool)`. |
| `MdCheckBox` | checkbox. `IsChecked`, `IsIndeterminate`, `IsThreeState`, `Label`, `State` (`Unchecked`/`Checked`/`Indeterminate`), `CheckedChanged`, `StateChanged`, `Toggle()`. |
| `MdRadioButton` | radio button. `IsChecked`, `Label`, `Value`, `GroupName`, `Command`. |
| `MdRadioGroup` | `StackPanel` of radios. `SelectedIndex`, `SelectedItem`, `SelectedValue`, `ItemsSource`, `AddOption(label, value, selected)`, `AddRadio`. |
| `MdSlider` | single-value slider. `Minimum`, `Maximum`, `Value`, `Step`, `Divisions`, `Centered`, `ShowValueIndicator`/`ShowLabel`/`ShowValueLabel`, `ValueFormatter`, `ValueChanged`. |
| `MdRangeSlider` | two-thumb slider. `StartValue`/`EndValue` (aliases `LowerValue`/`UpperValue`), `Step`, `Divisions`, `ValueFormatter`. |
| `MdSegmentedButton` | connected segmented control. `MultiSelect`, `AddSegment(text, icon, selected)`, `Segments`, `SelectedIndex`, `SelectedIndices`, `SelectedItem`, `SelectedItemsSource`, `SelectionChanged`. |
| `MdChip` | chip. `new MdChip(text, kind, icon, selected, elevated)`; `Kind` (`Assist`/`Filter`/`Input`/`Suggestion`), `IsSelected`, `IsElevated`, `ShowCloseIcon`/`IsRemovable`, `AvatarView`, `Clicked`, `SelectedChanged`, `CloseClicked`. |
| `MdChipGroup` | chip collection. `AddChip(text, kind, icon, selected, elevated)`, `Chips`, `ItemsSource`, `SingleSelection`, `SelectionRequired`, `Wrap`, `SelectedIndex`, `SelectedItem`, `SelectedIndices`, `SelectedTexts`, `SelectionChanged`. |
| `MdSelect` | dropdown select. `new MdSelect(label, options, variant, leadingIcon)`; `Options`, `SelectedItem` (string), `SelectedObject`, `SelectedIndex`, `ItemsSource`, `DisplayMemberPath`, `ItemFormatter`, `ErrorText`, `IsClearable`, `OpenMenu()`. |
| `MdComboBox` | editable combo box. `new MdComboBox(label, items, variant, leadingIcon)`; `Text`, `Items`, `ItemsSource`, `SelectedItem`, `SelectedIndex`, `IsClearable`, `OpenDropdown(filter)`, `CloseDropdown`, `TextChanged`, `SelectedItemChanged`. |

```csharp
var remember = new MdSwitch { IsToggled = true, ShowIcons = true };
remember.Toggled += (_, on) => Settings.Remember = on;

var role = new MdSelect("Role", new[] { "Admin", "Editor", "Viewer" })
{
    SelectedIndex = 0,
};
role.SelectedIndexChanged += (_, i) => { };

var rating = new MdRatingBar { MaxRating = 5, AllowHalf = true };
```

### 5.3 Date, time, and color

| Control | Purpose and key API |
|---|---|
| `MdDatePicker` | modal calendar handle. `SelectedDate`, `RangeStart`/`RangeEnd`, `IsRange`, `MinDate`/`MaxDate`, `SelectableDayPredicate`, `StartInInputMode`, `DateSelected`, `RangeSelected`, `Dismissed`, `Show(anchor)`, `Close()`. |
| `MdDatePickerField` | inline field that opens the picker. `SelectedDate`, `Format`, `MinDate`/`MaxDate`, `IsClearable`, `ErrorText`, `SelectedDateChanged`. |
| `MdDateRangePickerField` | range field. `StartDate`, `EndDate`, `Format`, `RangeChanged`, `RangeSelected`. |
| `MdCalendarGrid` | embedded month grid. `SelectedDate`, `RangeStart`/`RangeEnd`, `IsRange`, `SelectableDayPredicate`, `DateSelected`. |
| `MdTimePicker` | modal clock/input picker. `SelectedTime`, `Is24Hour`, `StartInInputMode`, `TimeSelected`, `Dismissed`, `Show`/`Close`. |
| `MdTimePickerField` | inline time field. `SelectedTime`, `Is24Hour`, `SelectedTimeChanged`. |
| `MdTimeDial` | clock dial. `Hour`, `Minute`, `Is24Hour`, `Mode` (`Hour`/`Minute`), `TimeChanged`, `HourSelectionCompleted`. |
| `MdColorPicker` | HCT color picker. `SelectedColor`, `ShowHexInput`, `ShowHctSliders`, `ShowPresetSwatches`, `ColorChanged`. |

Static modal helpers (await them):

```csharp
DateTime? date  = await MdDatePicker.PickDateAsync(DateTime.Today, minDate: DateTime.Today);
var range       = await MdDatePicker.PickDateRangeAsync(start: DateTime.Today);
TimeSpan? time  = await MdTimePicker.PickTimeAsync(new TimeSpan(9, 0, 0), is24Hour: true);
Color? color    = await MdColorPicker.PickAsync(anchor, Colors.Teal, "Accent color");
```

For forms, prefer the fields — they render a normal MD3 field and handle open/clear/format:

```csharp
var due = new MdDatePickerField
{
    Label = "Due date",
    Format = "dd MMM yyyy",
    MinDate = DateTime.Today,
    IsClearable = true,
};
```

### 5.4 Navigation

The library ships its **own Material page stack**. Do not use Avalonia
`NavigationPage`/`ContentPage` for Md3 screens: they constrain the chrome to the
built-in page template. Use `MdContentPage` + `MdNavigationPage`, which keep the
app bar, actions, bottom bar and FAB fully Material.

| Control | Purpose and key API |
|---|---|
| `MdContentPage` | page metadata + content. `Title`, `Subtitle`, `NavigationIcon`, `ShowAppBar`, `ShowBackButton`, `AppBarVariant`, `AddAction(icon, onClick)`, `BottomBar`, `FloatingActionButton`, `NavigationAction`, `Navigation`, `Appearing`/`Disappearing`, `OnBackPressed()`. |
| `MdNavigationPage` | own page stack. `SetRoot`, `Push`, `Pop`, `PopToRoot`, `Replace`, `TryGoBack`, `GoBackCommand`, `RefreshChrome`, `NavigationStack`, `CurrentPage`, `CanGoBack`, `StackDepth`, `Transition` (`None`/`Slide`/`Fade`), `PageChanged`; `ApplySafeArea`, `SafeAreaOverride`, `SafeAreaInsets`, `KeyboardHost`. Uses `MdTopAppBar` and `MdMotion` transitions and lifts the page above the soft keyboard. |
| `MdTopAppBar` | top app bar. `Title`, `Subtitle`, `NavigationIcon`, `Variant` (`CenterAligned`/`Small`/`Medium`/`Large`), `NavigationClicked`, `AddAction(icon, onClick)`, `AttachScrollViewer(scroll)`, `IsScrolled`, `CollapseProgress`, `SafeAreaPadding` (filled by `MdNavigationPage`). |
| `MdBottomAppBar` | bottom bar with FAB. `Fab`, `FabIcon`, `FabCommand`, `FabClicked`, `AddAction`, `SafeAreaPadding` (filled by `MdNavigationPage`). |
| `MdNavigationBar` | bottom navigation. `AddDestination(label, icon, selectedIcon, badge)`, `SelectedIndex`, `SelectedItem`, `ItemsSource`, `ShowLabels`, `SelectedIndexChanged`. |
| `MdNavigationRail` | side rail. Same destination API + `Header`. |
| `MdNavigationDrawer` | permanent drawer. `AddItem`, `AddSectionHeader`, `AddDivider`, `Title`/`Headline`, `DrawerWidth`, `SelectedIndex`. |
| `MdModalNavigationDrawer` | modal drawer handle. `Drawer`, `Items`, `IsOpen`, `Show(anchor)`/`Close()`. |
| `MdNavigationSuiteScaffold` | adaptive bar → rail → drawer. `Destinations`/`AddDestination(label, icon, selectedIcon, badge, content)`, `Mode` (`Auto`), `EffectiveMode`, `CompactBreakpoint` (600), `ExpandedBreakpoint` (840), `Body`, `TopAppBar`, `FloatingActionButton`, `RailHeader`, `DrawerHeadline`. |
| `MdTabs` | primary/secondary tabs. `Variant`, `IsScrollable`, `AddTab(text, icon, badge, content)`, `SelectedIndex`, `SelectedItem`, `ItemsSource`, `ItemTemplate`. |
| `MdBreadcrumbBar` | breadcrumb trail. `Add`, `SetPath(params string[])`, `MaxVisibleItems`, `SelectedItem`, `ItemClickCommand`. |
| `MdPagination` | page buttons. `TotalPages`, `CurrentPage`, `MaxVisiblePages`, `ShowFirstLastButtons`, `PageChanged`, `Command`. |
| `MdStepper` | wizard steps. `AddStep(title, subtitle, content, stateOverride)`, `CurrentStep`, `Orientation`, `ShowControls`, `AllowStepTap`, `Next()`, `Previous()`, `StepChanged`, `Completed`. |
| `MdToolbar` | floating/docked action bar. `Variant`, `ColorStyle`, `Orientation`, `AddAction`, `AddControl`, `AddDivider`, `CompanionFab`. |

Custom page stack:

```csharp
var home = new MdContentPage("Inbox")
{
    Subtitle = "12 messages",
    Content = list,
};
home.AddAction(MdIcons.Search, () => Search());

var details = new MdContentPage("Message")
{
    Content = detailsBody,
};
details.AddAction(MdIcons.Reply, () => Reply());

var navigation = new MdNavigationPage(home);
window.Content = navigation;

navigation.Push(details);
navigation.Pop();
```

`MdNavigationPage` supplies the back arrow automatically when `CanGoBack` is true.
It handles `Escape` and `Alt+Left`; `TryGoBack()` dismisses the top overlay first,
then asks the current page via `OnBackPressed()`, then pops. Pages can override
`OnBackPressed()` to consume back (for example, close an internal search state)
before the host pops them. Overlays owned by a page are closed automatically when
the page leaves the stack. Call `RefreshChrome()` after changing a visible page's
title, actions, bottom bar or FAB.

Safe area and soft keyboard are handled by the host:

```csharp
var navigation = new MdNavigationPage(home);

// Real insets come from IInsetsManager on Android/iOS. On desktop you can preview:
navigation.SafeAreaOverride = new Thickness(0, 24, 0, 48);
navigation.KeyboardHost.SimulatedKeyboardHeight = 300;   // preview keyboard inset
```

`ApplySafeArea` (default `true`) distributes the insets to the app bar, bottom bar,
FAB and page content. `KeyboardHost` is the internal `MdKeyboardAvoidingHost`; it
lifts the page above the input pane and scrolls the focused field into view.
Set `ApplySafeArea = false` or `SafeAreaOverride = null` to opt out.

Adaptive shell (desktop/tablet optional):

```csharp
var shell = new MdNavigationSuiteScaffold
{
    Body = pageHost,
    TopAppBar = new MdTopAppBar("Mail") { NavigationIcon = MdIcons.Menu },
    FloatingActionButton = new MdFab(MdIcons.Edit),
    DrawerHeadline = "Mail",
};
shell.AddDestination("Inbox", MdIcons.Inbox, MdIcons.Inbox, "12", inboxPage);
shell.AddDestination("Sent", MdIcons.Send, content: sentPage);
shell.SelectedIndexChanged += (_, i) => { };
```

### 5.5 Containers and layout

| Control | Purpose and key API |
|---|---|
| `MdCard` | MD3 card. `new MdCard(MdCardVariant variant, Control? body, bool interactive)`; `Variant` (`Elevated`/`Filled`/`Outlined`), `IsInteractive`/`IsClickable`, `CardRadius`, `CardPadding`, `Body`, `Clicked`. |
| `MdSurface` | styled surface. `FillColor`, `SurfaceTintColor`, `Radius`, `Elevation`, `BorderStrokeColor`, `BorderStrokeThickness`. |
| `MdExpansionPanel` | expandable panel. `Title`/`Header`, `Subtitle`, `LeadingIcon`, `IsExpanded`, `Body`, `ExpandedChanged`. |
| `MdExpansionPanelList` | accordion list. `SingleExpand`/`AllowMultipleExpanded`, `AddPanel`. |
| `MdSplitView` | resizable master/detail. `Pane`, `DetailContent`, `PaneWidth`, `MinPaneWidth`, `MaxPaneWidth`, `InitialRatio`, `IsPaneOpen`, `IsResizable`, `CompactBreakpoint`, `IsCompact`, `CompactModeChanged`. |
| `MdCarousel` | swipeable card carousel. `AddItem(title, subtitle, icon, accent)`, `Items`, `ItemsSource`, `ItemTemplate`, `Layout` (`Hero`/`CenterAligned`/`MultiBrowse`/`Uncontained`), `SelectedIndex`, `ShowIndicators`, `Next`, `Previous`, `SelectedIndexChanged`, `ItemClicked`. |
| `MdSettingsCard` | settings row. `Header`/`Title`, `Description`, `Icon`, `ActionContent`, `IsActionable`, `Command`, `Clicked`. |
| `MdSettingsExpander` | settings group. `AddItem(title, subtitle, trailingControl, leadingIcon)`, `AddCustomContent`, `IsExpanded`, `HeaderAction`. |
| `MdAvatar` | initials/icon/image avatar. `Initials`, `Icon`, `ImageSource`, `Size`, `Status` (`None`/`Online`/`Busy`/`Away`/`Offline`), `CustomFillColor`, `CustomTextColor`. |
| `MdSafeArea` | safe-area inset container. `Body`, `Edges` (`None`/`Left`/`Top`/`Right`/`Bottom`/`Horizontal`/`Vertical`/`All`), `BasePadding`, `SimulatedInset`, `AppliedInset`, `RefreshInsets()`. |
| `MdKeyboardAvoidingHost` | keyboard-aware container. `Body`, `ExtraBottomSpacing`, `AnimateInset`, `AutoScrollFocusedElement`, `SimulatedKeyboardHeight`, `KeyboardHeight`, `KeyboardHeightChanged`. |
| `MdScrollViewer` | theme-independent scroll host used by Md3 controls and demo pages. |
| `MdWindow` | theme-free desktop window template with a `VisualLayerManager` for overlays. |
| `MdBadge` / `MdBadgedBox` | count/dot badge. `Text`, `Count`, `MaxCount`, `IsDot`; wrapper adds `Target`, `BadgeText`, `BadgeCount`, `ShowBadge`. |
| `MdDivider` | horizontal/vertical divider. `IsVertical`, `InsetStart`, `InsetEnd`. |
| `MdLabel` | typography-aware text. `Role`, `ColorRole`, `Secondary`. |

Layout examples:

```csharp
var hero   = new MdCarousel { Layout = MdCarouselLayout.Hero, Height = 220 };
var browse = new MdCarousel { Layout = MdCarouselLayout.MultiBrowse, Height = 200 };
hero.AddItem("Material 3", "Tonal palettes", MdIcons.Palette);

// Pane starts at 40% of the width. Applied once on first layout; an explicit
// PaneWidth (or a later resize) keeps its pixel width instead of following the ratio.
var split = new MdSplitView { InitialRatio = 0.4 };
```

`Hero` gives the focused card the most space; `CenterAligned` centres it with symmetric
peeks; `MultiBrowse` shows about three equal cards; `Uncontained` centres a wide card
that bleeds past the viewport edges. `InitialRatio` is clamped to 15–85% and to
`MinPaneWidth`/`MaxPaneWidth`; setting `PaneWidth` explicitly disables it.

### 5.6 Lists, tables, trees

| Control | Purpose and key API |
|---|---|
| `MdList` | vertical list (`StackPanel`). `AddItem(MdListItem)`, `AddSubheader`, `AddDivider`, `ItemsSource`, `ItemTemplate`, `ItemFactory`, `ShowDividers`, `DividerInsetStart`. |
| `MdListItem` | list row. `Headline`, `SupportingText`, `Overline`, `LeadingIcon`, `TrailingIcon`, `TrailingText`, `LeadingView`, `TrailingView`, `IsSelected`, `IsInteractive`, `Command`, `Clicked`, `TrailingIconClicked`, `LeadingSwipeAction`, `TrailingSwipeAction`, `Lines` (`OneLine`/`TwoLine`/`ThreeLine`). |
| `MdSwipeAction` | swipe action behind a row. `Label`, `Icon`, `IsDestructive`, `OnTriggered`/`Command`, `BackgroundColor`, `ForegroundColor`, `Triggered`. |
| `MdListSubheader` | section label. `Text`. |
| `MdDataTable` | data grid with sorting/filter/pagination. `AddColumn(header, valueSelector, isNumeric, isSortable, cellTemplate, width)`, `Columns`, `ItemsSource`, `IsSelectable`, `SelectedItem`, `SelectedItems`, `SelectedItemsSource`, `FilterText`, `ShowPagination`, `PageSize`, `CurrentPage`, `AlternatingRowBackground`, `MaxBodyHeight`, `Layout` (`Auto`/`Table`/`Cards`), `CompactBreakpoint` (600), `IsCompact`, `EffectiveLayout`, `RowClicked`, `SelectionChanged`, `ClearSelection()`. Auto switches to cards on hosts narrower than `CompactBreakpoint`. |
| `MdDataColumn` | column definition. `Header`, `PropertyName`/`BindingPath`, `ValueSelector`, `IsNumeric`, `IsSortable`, `CellTemplate`, `CellFactory`, `Width`. |
| `MdTreeView` | hierarchical tree. `Roots`/`Add`, `SelectedNode`, `ShowCheckBoxes`, `CascadeCheckBoxes`, `ShowIndentGuides`, `FilterText`, `ExpandAll`, `CollapseAll`, `GetCheckedNodes`, `SelectedNodeChanged`, `NodeCheckedChanged`. |
| `MdTreeNode` | tree node. `Text`, `Icon`, `BadgeText`, `IsExpanded`, `IsChecked`, `Children`, fluent `Add`. |
| `MdTimeline` | vertical timeline. `Add(title, description, timestamp, icon, status, customContent)`, `Items`, `ItemsSource`, `ItemTemplate`. |
| `MdTimelineItem` | timeline entry. `Title`, `Description`, `Timestamp`, `Icon`, `Status` (`Completed`/`Active`/`Pending`/`Warning`/`Error`), `CustomContent`. |

List example:

```csharp
var list = new MdList { ShowDividers = true };
list.AddSubheader("Today");
list.AddItem(new MdListItem
{
    Headline = "Design review",
    SupportingText = "10:30 – 11:15",
    LeadingIcon = MdIcons.Event,
    TrailingIcon = MdIcons.ChevronRight,
    Command = openCommand,
});
list.AddDivider();
```

Table example:

```csharp
var table = new MdDataTable
{
    ItemsSource = rows,
    IsSelectable = true,
    ShowPagination = true,
    PageSize = 20,
    FilterText = "",
};
table.AddColumn("Name", row => ((Person)row).Name);
table.AddColumn("Role", row => ((Person)row).Role);
table.AddColumn("Score", row => ((Person)row).Score, isNumeric: true);
table.RowClicked += (_, row) => { };
```

`MdDataTable` requires at least one column before it renders rows. Sorting/filtering are
client-side over `ItemsSource`; there is **no virtualization** — page large collections.

### 5.7 Feedback, status, and progress

| Control | Purpose and key API |
|---|---|
| `MdDialog` | modal dialog. See §4.2. |
| `MdBottomSheet` / `MdSideSheet` | sheets. See §4.3. |
| `MdSnackbar` | global snackbar FIFO. See §4.5. |
| `MdBanner` | inline banner. `Message`/`Text`, `Icon`, `PrimaryActionText`, `SecondaryActionText`, `PrimaryActionClicked`, `SecondaryActionClicked`, `Show()`, `Dismiss()`. |
| `MdInfoBar` | severity banner. `Title`, `Message`, `Severity` (`Info`/`Success`/`Warning`/`Error`), `IsOpen`, `IsClosable`, `ActionText`, `ActionClicked`, `Closed`. |
| `MdEmptyState` | empty placeholder. `Title`, `Description`, `Icon`, `ActionText`, `SecondaryActionText`, `ActionClicked`, `SecondaryActionClicked`. |
| `MdLinearProgress` | linear progress. `Value` (0–1), `IsIndeterminate`, `ShowStopIndicator`, `IsWavy`, `WaveAmplitude`, `WaveLength`. |
| `MdCircularProgress` | circular spinner/progress. `Value`, `IsIndeterminate`, `Size`/`Diameter`, `StrokeWidth`, `CustomActiveColor`, `IsWavy`. |
| `MdSegmentedProgressBar` | segmented bar. `Value`, `SegmentCount`, `SegmentGap`, `TrackHeight`. |
| `MdWavyProgressIndicator` | wavy linear progress (subclass of `MdLinearProgress`). |
| `MdLoadingIndicator` | contained loading spinner. `Size`, `IsContained`, `IndicatorColor`. |
| `MdSkeleton` | skeleton placeholder. `Shape` (`Rounded`/`Circle`/`Text`), `Circle(size)`, `Text(width, height)`, `ListRow(avatarSize, lines)`. |
| `MdRefreshIndicator` | pull-to-refresh host. `Body`/`Content`, `IsRefreshing`, `TriggerDistance`, `MaxPullDistance`, `StatusText`, `ArmedText`, `RefreshingText`, `RefreshHandler` (returns `Task`) or `Command`, `RefreshRequested`, `TriggerRefreshAsync()`. |
| `MdRadialGauge` | radial gauge. `Value`, `Minimum`, `Maximum`, `StepSize`, `Unit`, `Label`, `Size`, `TickCount`, `IsInteractive`, `ValueChanged`. |
| `MdSparkline` | mini chart. `Kind` (`Line`/`Bar`/`Donut`), `Values` or `ValuesText` (CSV), `ItemsSource`, `AccentColor`, `CenterLabel`. |
| `MdAvatarGroup` | overlapping avatars. `MaxDisplayed`, `AvatarSize`, `OverlapRatio`, `AddAvatar`, `ItemsSource`. |
| `MdFileDropZone` | file drop/list area. `Title`, `Subtitle`, `AllowMultiple`, `Files`, `FilesSource`, `AddFile(name, sizeBytes, path)`, `RemoveFile`, `BrowseFilesAsync()`, `FileAdded`, `FileRemoved`, `FilesChanged`. |
| `MdChatBubble` | chat message bubble. `Text`, `SenderName`/`Sender`, `Timestamp`, `AvatarInitials`, `IsOutgoing`, `IsRead`. |
| `MdBreadcrumbBar` / `MdPagination` | navigation aids. See §5.4. |

Progress and refresh:

```csharp
var bar = new MdLinearProgress { IsIndeterminate = true };
var spinner = new MdCircularProgress(24, 3) { IsIndeterminate = true };

var refresh = new MdRefreshIndicator
{
    Body = list,
    RefreshHandler = async () =>
    {
        await ReloadAsync();
    },
};
```

### 5.8 Forms helper

`MdFormValidator` wires rules directly to fields and writes `ErrorText`/`HasError`.

```csharp
var name = new MdTextField("Name");
var email = new MdTextField("Email");
var city = new MdTextField("City");

var validator = new MdFormValidator { ValidateOnChange = true };
validator.Field(name).Required().MinLength(2);
validator.Field(email).Required().Email();
validator.Field(city).Required();

bool valid = validator.Validate();
if (!valid)
{
    foreach (var error in validator.Errors)
        Console.WriteLine(error);
}
```

Built-in overloads exist for `MdTextField`, `MdTextArea`, `MdSelect`, `MdComboBox`, `MdPinInput`,
`MdDatePickerField`, `MdDateRangePickerField`, and `MdTimePickerField`.
For any other control use the generic form:

```csharp
var age = new MdNumberBox { Label = "Age", Minimum = 0, Maximum = 120 };

validator.Field(
    valueGetter: () => age.Value.ToString(CultureInfo.InvariantCulture),
    errorSetter: err => age.Suffix = err,
    subscribeChanged: subscribe => age.ValueChanged += (_, _) => subscribe());
```

Rules: `Required`, `MinLength`, `MaxLength`, `Email`, `Numeric`, `Pattern(regex)`,
`Matches(otherGetter)`, `Must(predicate, message)`.

Attached-property validation for AXAML:

```csharp
MdValidation.SetRequired(textField, true);
MdValidation.SetEmail(textField, true);
MdValidation.SetMinLength(textField, 3);
MdValidation.SetPattern(textField, @"^[A-Z].*");
MdValidation.SetRequiredMessage(textField, "Required");
bool ok = MdValidation.ValidateTree(root);
```

`MdValidation` also supports `IsRequired`/`GetIsRequired` aliases, `IsEmail`, `IsNumeric`,
`MinLength`, `MaxLength`, `Pattern`, `Validator`, and message overrides per control.

---

## 6. Recipes

### 6.1 Complete page with app bar, list, and FAB

```csharp
public static Control BuildPage()
{
    var list = new MdList { ShowDividers = true };
    list.AddSubheader("Projects");
    list.AddItem(new MdListItem
    {
        Headline = "Md3.Avalonia",
        SupportingText = "Material 3 in pure C#",
        LeadingIcon = MdIcons.Folder,
        TrailingIcon = MdIcons.ChevronRight,
    });

    var appBar = new MdTopAppBar("Projects")
    {
        NavigationIcon = MdIcons.Menu,
        Variant = MdTopAppBarVariant.Small,
    };
    appBar.AddAction(MdIcons.Search);
    appBar.AddAction(MdIcons.MoreVert);

    var shell = new DockPanel();
    DockPanel.SetDock(appBar, Dock.Top);
    shell.Children.Add(appBar);
    shell.Children.Add(list);

    var fab = new MdFab(MdIcons.Add);
    fab.HorizontalAlignment = HorizontalAlignment.Right;
    fab.VerticalAlignment = VerticalAlignment.Bottom;
    fab.Margin = new Thickness(24);

    var root = new Grid();
    root.Children.Add(shell);
    root.Children.Add(fab);
    return root;
}
```

### 6.2 Theme switcher in a settings page

```csharp
var darkMode = new MdSwitch
{
    IsToggled = MdTheme.Current.IsDark,
    ShowIcons = true,
    OnIcon = MdIcons.DarkMode,
    OffIcon = MdIcons.LightMode,
};
darkMode.Toggled += (_, on) => MdTheme.Mode = on ? MdThemeMode.Dark : MdThemeMode.Light;

var card = new MdSettingsCard
{
    Header = "Appearance",
    Title = "Dark mode",
    Description = "Use the dark color scheme",
    Icon = MdIcons.Palette,
    ActionContent = darkMode,
};
```

### 6.3 Confirm + snackbar with undo

```csharp
private async void OnDeleteClicked(object? sender, EventArgs e)
{
    bool confirmed = await MdDialog.ShowConfirmAsync(
        "Delete item?",
        "You can undo this for a few seconds.",
        "Delete",
        "Cancel",
        MdIcons.Delete);

    if (!confirmed) return;

    DeleteItem();
    MdSnackbar.Show("Item deleted", "Undo", UndoDelete,
        duration: MdSnackbarDuration.Long);
}
```

### 6.4 Form submit

```csharp
var name = new MdTextField("Full name", MdTextFieldVariant.Outlined);
var email = new MdTextField("Email");
var date = new MdDatePickerField { Label = "Start date", IsClearable = true };
var role = new MdSelect("Role", new[] { "Admin", "Editor", "Viewer" });

var validator = new MdFormValidator();
validator.Field(name).Required().MinLength(2);
validator.Field(email).Required().Email();
validator.Field(date).Required("Pick a date");
validator.Field(role).Required("Pick a role");

var submit = new MdButton("Create", MdButtonVariant.Filled);
submit.Clicked += async (_, _) =>
{
    if (!validator.Validate())
    {
        MdSnackbar.Show("Fix the highlighted fields");
        return;
    }

    await api.CreateAsync(new CreateRequest
    {
        Name = name.Text,
        Email = email.Text,
        Start = date.SelectedDate!.Value,
        Role = role.SelectedItem!,
    });
    MdSnackbar.Show("Created");
};
```

### 6.5 Popup anchored to a button

```csharp
var trigger = new MdButton("Filters", MdButtonVariant.Outlined, MdIcons.FilterList);
trigger.Clicked += (_, _) =>
{
    var panel = new StackPanel { Spacing = 8, Width = 240 };
    panel.Children.Add(new MdCheckBox("Archived"));
    panel.Children.Add(new MdCheckBox("Shared"));

    MdOverlay.ShowAnchoredPopup(trigger, new MdSurface
    {
        FillColor = MdTheme.ColorOf(MdColorRole.SurfaceContainerHigh),
        Radius = MdTheme.Current.Shape.Medium,
        Elevation = 3,
        Content = new Border { Padding = new Thickness(12), Child = panel },
    });
};
```

### 6.6 Responsive split layout

```csharp
var split = new MdSplitView
{
    Pane = navigationPanel,
    DetailContent = contentHost,
    PaneWidth = 280,
    MinPaneWidth = 220,
    MaxPaneWidth = 420,
    IsResizable = true,
    CompactBreakpoint = 760,
};
split.CompactModeChanged += (_, compact) =>
{
    split.IsPaneOpen = !compact;
};
```

### 6.7 Command-based MVVM

```csharp
public sealed class MainViewModel
{
    public ICommand SaveCommand { get; }
    public MainViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave);
    }
    // ...
}

var button = new MdButton("Save") { Command = vm.SaveCommand };
var field = new MdTextField("Name") { Text = vm.Name };
field.TextChanged += (_, text) => vm.Name = text;
```

All controls accept `Command`/`CommandParameter`; use the CLR events only for view-local behavior.

---

## 7. Gotchas and limitations

1. **`UseMd3()` is not optional** if you want bundled fonts and Back/Escape overlay dismissal.
   Without it, icon glyphs render as missing characters.
2. **Popup controls are invisible handles.** `MdDialog`, `MdMenu`, `MdPopover`, `MdRichTooltip`,
   `MdBottomSheet`, `MdSideSheet`, `MdDatePicker`, `MdTimePicker`, `MdSearchView`, and
   `MdModalNavigationDrawer` have zero size and display through `MdOverlay`. Adding them to a
   `StackPanel` produces nothing.
3. **Overlay calls need a live TopLevel.** Call `Show()` from a click handler or after the window is
   opened. During `Application.Initialize()` there is no anchor; pass a live control or use
   `MdOverlay.TrackTopLevel`.
4. **`ItemsSource` owns the children.** Setting it clears manual items and rebuilds on every
   collection change. Do not mix `ItemsSource` and `Children`/`Items` for the same control.
5. **`TextChanged` fires for code changes.** `MdTextField`, `MdTextArea`, `MdSearchBar`, `MdComboBox`,
   `MdPinInput`, and validator subscriptions respond to programmatic assignment as well as typing.
6. **`MdSwitch` uses `IsToggled`**, not `IsOn`. (Common mistake when porting from MAUI.)
7. **`MdSnackbar` is global and static.** `Show` clears the pending queue; `Enqueue` appends.
   Only one snackbar is visible per application, regardless of window. When shown from a page inside
   `MdNavigationPage`, it automatically sits above the bottom app bar, FAB and safe-area inset.
8. **`MdTooltip` is a single global handle.** Showing a new tooltip replaces the previous one.
9. **`MdList.ShowDividers` manages only `MdListItem` children.** Items added after the property is set
    get dividers automatically (all but the last); setting it to `false` clears the managed dividers.
    Explicit per-item `ShowDivider = true` is preserved while the list does not manage dividers.
    Subheaders and custom children never receive dividers; `DividerInsetStart` is applied through the list.
10. **`MdList` virtualizes `ItemsSource` inside a `ScrollViewer`.** Only the visible window
    (plus ~480 dp overscan) is realized; `Children` also contains two internal spacer
    borders, so do not treat `Children` as the full item list in that mode. Manual
    `AddItem`/`AddSubheader` lists are never virtualized. Small sources (≤60) and lists
    without a `ScrollViewer` are realized eagerly.
11. **`MdDataTable` needs columns first**, and sorting/filtering/pagination are client-side.
    Rows are appended in chunks of 100 while the body scrolls (`MaxBodyHeight`); pagination
    is the recommended path for very large sources. `Layout = Auto` (default) renders cards
    when the host is narrower than `CompactBreakpoint` (600 dp); set `Table` or `Cards` to
    override. Cards show the first column as a headline, the rest as label/value pairs, and
    sortable columns as chips.
12. **`MdPinInput`/`MdTextField` validation is string-based.** `MdFormValidator.Field(MdNumberBox)`
    has no dedicated overload; use the generic getter/setter form shown in §5.8.
13. **Theme changes are global and immediate.** `MdTheme.Configure` rebuilds brushes and raises
    `Changed`; call it on the UI thread. Use `MdTheme.Observe` for custom-drawn controls instead of
    capturing a palette in a field.
14. **`MdIcon` defaults to `OnSurfaceVariant`** when no `ColorRole`/`Color` is given. Pass
    `ColorRole` or set `UseThemeDefaultColor = false` when embedding icons in colored containers.
15. **The library ships no Avalonia control theme.** Plain `Button`, `TextBox`, `ScrollViewer`, etc.
    have no template unless the host application installs a theme. Use Md3 controls plus
    `MdWindow`/`MdScrollViewer`, or install `Avalonia.Themes.Fluent` yourself.
16. **`Md3Theme` is a `Style`, not a theme dictionary.** Add it to `Window.Styles`/`Application.Styles`;
    it forwards properties to the static theme but does not style arbitrary controls.
17. **Custom-drawn controls (`MdSwitch`, `MdSlider`, `MdCarousel`, `MdTabs`, …) have no `Content`.**
    Put content in their items/body properties instead.
18. **DP/threading:** update `ItemsSource` and call overlay APIs on the UI thread. Background work
    should marshal via `Dispatcher.UIThread`.
19. **Headless tests cover overlays too.** `Tests/Md3.Avalonia.Tests.csproj` uses
    `Avalonia.Headless.XUnit` (xunit v3). `MdNavigationPage` registers a fallback overlay host, so
    dialogs, pickers and sheets can be tested without an Avalonia `OverlayLayer`. The suite has
    233 tests across navigation, list, keyboard, controls (buttons/inputs/selection/pickers/
    feedback/chrome/containers) and a control-creation smoke test. The source under
    `Md3.Avalonia/` remains the source of truth; run the build after changes.
20. **`adb shell input` is not a reliable touch test on every ROM.** Some devices inject
    `MotionEvent`s with `ToolType=Unknown`, which Avalonia ignores; use a real finger or
    `monkey`/instrumentation that emits finger events before blaming the UI. Keyboard
    (`Tab`/`Enter`) and `KEYCODE_BACK` still exercise the app and overlays.
21. **`SimulatedKeyboardHeight` is a global overlay preview while attached.** Every
    `MdKeyboardAvoidingHost` with a non-zero `SimulatedKeyboardHeight` lifts all overlays
    (dialogs, sheets, popovers) above that inset. The value is contributed only while the
    host is in the visual tree, so leaving a preview page restores the full-height overlay;
    the effective value is the maximum across currently attached hosts.

---

## 8. Repository map and build

```
<repo-root>/
├── Md3.sln                     solution: library, tests and all sample hosts
├── CHANGELOG.md                versions, notable changes and SemVer policy
├── Md3.Avalonia/
│   ├── Md3.Avalonia.csproj      net10.0, Avalonia 12.1.4, embedded fonts
│   ├── MdFonts.cs               UseMd3, font collections, XML namespace
│   ├── MdIcons.cs               ~10,800 icon glyph constants
│   ├── MdLocalization.cs        providers, culture switching, Observe, AXAML + keys
│   ├── LocExtension.cs          {md:Loc ...} AXAML markup extension
│   ├── MdLocalizationExtensions.cs  Localize / LocalizeLabel / LocalizePlaceholder
│   ├── Controls/                60 control source files
│   ├── Primitives/
│   │   ├── MdSurface.cs         MdContentView, MdSurface, MdGridExtensions
│   │   ├── MdInkSurface.cs      ripple/state-layer clickable surface
│   │   └── MdIcon.cs            icon TextBlock with font auto-selection
│   ├── Platform/
│   │   ├── MdOverlay.cs         OverlayLayer/fallback host, dismiss stack
│   │   ├── MdInputs.cs          MdNativeEntry/MdNativeEditor, context menu
│   │   ├── MdKeyboard.cs        soft-keyboard helpers
│   │   └── MdTextMetrics.cs     text measurement helpers
│   ├── Theming/
│   │   ├── MdTheme.cs           MdTheme + Md3Theme style
│   │   ├── MdThemeOptions.cs    configuration object
│   │   ├── MdThemeService.cs    injectable theme service
│   │   ├── MdColorScheme.cs     roles, variants, contrast, seed extraction
│   │   ├── MdColors.cs          named seed colors
│   │   ├── MdTokens.cs          shape/elevation/state/density tokens
│   │   ├── MdTypography.cs      15-role type scale + Apply extensions
│   │   ├── MdMotion.cs          durations, easings, animation helpers
│   │   └── ColorUtilities/      HCT/quantizer/color math
│   └── Resources/Fonts/         Roboto + 3 icon fonts
├── Tests/
│   └── Md3.Avalonia.Tests.csproj headless navigation/list/keyboard/overlay tests
├── samples/
│   ├── Demo/
│   │   └── Md3.Demo.csproj      desktop navigation demo (own Md page stack)
│   ├── Demo.Android/
│   │   ├── Demo.Android.csproj  Android host, links ../Demo/Pages.cs
│   │   ├── MainActivity.cs      AvaloniaMainActivity wiring
│   │   └── MobileApp.cs         Android Application + root MdNavigationPage
│   ├── Gallery/                 adaptive control gallery (desktop + shared UI)
│   │   ├── Md3.Gallery.csproj   desktop gallery host
│   │   ├── GalleryApp.cs        shared Application (desktop/web/Android)
│   │   ├── GalleryWindow.cs     desktop MdWindow
│   │   └── Shared/              adaptive shell + one page per purpose group
│   ├── Gallery.Browser/         WebAssembly host (Avalonia.Browser)
│   └── Gallery.Android/         Android gallery host (com.md3.gallery)
├── docs/
│   ├── README.md                documentation index
│   ├── AGENTS.md                library rules and API catalogue
│   ├── HANDOFF.md               session handoff, current status and next steps
│   ├── PLAN.md                  project plan and current status
│   ├── demo/README.md           demo run and resize notes
│   ├── gallery/                 gallery README + screenshots
│   └── handoff/                 verification screenshots (Linux, Windows legacy, Android)
├── tools/linux/md3ui.py         Linux/X11 run/screenshot/click/key/wheel helper
├── tools/win/md3ui.ps1          native Windows run/screenshot/click/key helper
└── ref/                         Avalonia source/reference checkout
```

Build:

```bash
dotnet build Md3.sln -v q --nologo
dotnet build Md3.Avalonia/Md3.Avalonia.csproj -v q --nologo
dotnet build samples/Demo/Md3.Demo.csproj -v q --nologo
dotnet test Tests/Md3.Avalonia.Tests.csproj --nologo
```

On Linux (current development platform, Mint/Ubuntu, X11) and on Windows the Android
host builds with the same SDK, once the Android workload and SDK platform 36 exist:

```bash
dotnet workload install android            # once per SDK feature band
dotnet build samples/Demo.Android/Demo.Android.csproj -v q --nologo
```

The Android host needs `ANDROID_HOME` pointing at an SDK that contains
`platforms/android-36` (plus `build-tools`, `platform-tools`) and a JDK 17+ `JAVA_HOME`.
On Windows the same command is run through the Windows SDK:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" build samples\Demo.Android\Demo.Android.csproj -v q --nologo
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` and all tests passing.

Run the desktop demo:

```bash
dotnet run --project samples/Demo/Md3.Demo.csproj
MD3_SMOKE=1 dotnet run --project samples/Demo/Md3.Demo.csproj
MD3_WINDOW=400x800 MD3_SAFE_AREA=24,48 MD3_KEYBOARD=300 dotnet run --project samples/Demo/Md3.Demo.csproj
MD3_WINDOW=320x640 dotnet run --project samples/Demo/Md3.Demo.csproj
MD3_WINDOW=640x360 MD3_SAFE_AREA=48,0,0,24 dotnet run --project samples/Demo/Md3.Demo.csproj
```

There is a solution at `Md3.sln` containing the library, tests and all sample hosts:
`samples/Demo/` is the desktop harness, `samples/Gallery*` the full control gallery and
`Tests/` the headless test project. `docs/PLAN.md` tracks implementation status.

Control gallery:

```bash
dotnet run --project samples/Gallery/Md3.Gallery.csproj            # desktop
dotnet run --project samples/Gallery.Browser/Gallery.Browser.csproj # WebAssembly server
dotnet build samples/Gallery.Android/Gallery.Android.csproj        # com.md3.gallery APK
```

GUI automation helpers (both click by window-relative pixels of the **client area**, send
keys, scroll under the cursor and write screenshots):

* Linux/X11: `tools/linux/md3ui.py` — `start|stop|shot|click|move|key|type|wheel|info`,
  `--window`, `--safe-area`, `--keyboard`, `--frame`; needs `python3-xlib`, `Pillow` and
  `gnome-screenshot` or `scrot`. Example:
  `python3 tools/linux/md3ui.py start --window 400x800 --safe-area 24,48 --keyboard 300`.
* Windows: `tools/win/md3ui.ps1` — `-Action start|stop|shot|click|key|wheel|info`,
  `-EnvWindow`, `-EnvSafeArea`, `-EnvKeyboard`.
* Android: `adb install -r samples/Demo.Android/bin/Debug/net10.0-android/com.md3.demo-Signed.apk`,
  then `adb shell am start -n com.md3.demo/crc64351746bbe631ada1.MainActivity` and
  `adb exec-out screencap -p > shot.png`. The emulator runs locally on Linux via KVM
  (AVD `Medium_Phone_API_37.0`); stop it with `adb emu kill`.
