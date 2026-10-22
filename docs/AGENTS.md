# Mav — Agent Guide

Material Design 3 component library for **Avalonia 12.1.4** / **.NET 10**, written entirely in C#.
There are no AXAML control templates, no resource dictionaries to merge, and no dependencies
beyond Avalonia itself. Every control is drawn/constructed in code.

This file is the single reference for using the library:
§1–§4 are required reading before writing UI code, §5 is a control lookup catalog,
§6 has end-to-end recipes, §7 lists pitfalls, §8 maps the repository.

> This is the only agent-facing document in `docs/`. Older plans, session handoffs and performance
> analyses were removed; the facts that still matter are kept here. Release history: `CHANGELOG.md`.

---

## 0. Ten rules for agents

1. Call `AppBuilder.UseMav(...)` once at startup. It registers the bundled fonts and the overlay host.
2. Create controls in C# and assign them to `Content`/`Children`; there are no `ControlTheme`s to look up.
3. Use `MavTheme.Configure(...)` (or the `UseMav` callback) for all color/type/shape decisions. Never hard-code MD3 colors.
4. Use `MavIcons.*` constants for icon glyphs. Do not paste raw Unicode glyphs.
5. Use `MavLabel` + `MavTypeRole` for typography, not ad-hoc `FontSize` values.
6. Prefer the control's built-in collection helpers (`AddItem`, `AddTab`, `AddDestination`, `AddColumn`, …).
7. For popups/dialogs/sheets, call `Show(anchor)` while the anchor is attached to a live `TopLevel`.
8. For forms, use the inline picker fields (`MavDatePickerField`, `MavTimePickerField`) and `MavFormValidator`.
9. Do not mix `ItemsSource` with manually added children/items; `ItemsSource` owns the child list.
10. Build with `dotnet build MavUI/MavUI.csproj`; the project targets `net10.0`.
11. Pass an attached control as the overlay anchor (`dialog.Show(this)`). A `null` anchor falls back to the last tracked `TopLevel`; if none exists yet the overlay waits in a pending queue (§4.1).
12. Change the theme once per change, not per control. Headless measurements: switching the theme took ~63 ms for 500 `MavButton` and ~111 ms for 500 `MavTextField`, so batch your changes.
13. For long lists use `ItemsSource` inside a `ScrollViewer` (virtualized) and `MavDataTable` with pagination for very large sources (§7).
14. Shrink the bundle with `-p:MavFonts=no-mdi` when you use classic `MavIcons`; `minimal` breaks them (§1.5).

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
    <ProjectReference Include="..\MavUI\MavUI.csproj" />
  </ItemGroup>
</Project>
```

The library assembly/package is `Mav`, version `0.1.0`.

### 1.2 AppBuilder (required)

```csharp
using Avalonia;
using Mav;              // UseMav extension, MavIcons, MavLocalization
using Mav.Theming;      // MavTheme, MavThemeOptions, enums

public static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseMav(opts =>
            {
                opts.SeedColor = MavColors.Purple;      // any seed color
                opts.Mode = MavThemeMode.System;        // System | Light | Dark
                opts.SchemeVariant = MavSchemeVariant.TonalSpot;
            })
            .LogToTrace();
}
```

`UseMav` does three things:

* calls `MavOverlay.Initialize()` (Back/Escape dismissing for overlays),
* registers the embedded fonts (`fonts:MavFonts`),
* optionally applies the theme after setup (`AfterSetup`).

The library ships **no Avalonia control theme**. Use `MavWindow` for desktop windows and
`MavScrollViewer` instead of `ScrollViewer`; both provide their own templates. On Android the
`MavNavigationPage` overlay fallback keeps dialogs, sheets, menus and pickers working even though
the Android `EmbeddableControlRoot` has no themed `OverlayLayer`. If you install an Avalonia theme
yourself, the normal `OverlayLayer` path is used automatically.

On Android, enable system-bar synchronization from the mobile host:

```csharp
var edgeToEdge = (int)Android.OS.Build.VERSION.SdkInt >= 35; // Android 15+
MavSystemBars.Attach(navigation, edgeToEdge: edgeToEdge);
```

Edge-to-edge is an Android 15+ feature; on older releases request it off and let
`MavSystemBars` paint the bars with the current `Surface` color and icon theme. The helper
also re-applies after the native window is ready, so the first frame does not keep the
default bar color. The fallback overlay host is only hit-test-visible while an overlay is
open, so it never swallows pointer input.

If you prefer to configure later, call `UseMav()` without a callback and then
`MavTheme.Configure(...)` from `App.OnFrameworkInitializationCompleted` or after the main window opens.

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
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

public class MainWindow : MavWindow
{
    public MainWindow()
    {
        Title = "Mav";
        Width = 1000;
        Height = 700;

        var save = new MavButton("Save", MavButtonVariant.Filled, MavIcons.Save);
        save.Clicked += (_, _) => MavSnackbar.Show("Saved");

        Content = new MavScrollViewer
        {
            Padding = new Thickness(24),
            Content = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    new MavLabel
                    {
                        Text = "Dashboard",
                        Role = MavTypeRole.HeadlineMedium,
                    },
                    new MavTextField("Name", placeholder: "Ada Lovelace"),
                    save,
                },
            },
        };
    }
}
```

### 1.4 AXAML usage (optional)

The library exposes the XML namespace `https://where.it/mav` with prefix `mav`.
All controls work in AXAML even though the implementation is C#.

```xml
<mav:MavWindow xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:mav="https://where.it/mav"
              x:Class="MyApp.MainWindow">
  <mav:MavWindow.Styles>
    <mav:MavThemeStyle SeedColor="#6750A4" Mode="System"
                  Variant="TonalSpot" Density="Comfortable" />
  </mav:MavWindow.Styles>

  <mav:MavScrollViewer Padding="24">
    <StackPanel Spacing="16">
      <mav:MavLabel Text="Hello" Role="HeadlineMedium" />
      <mav:MavButton Text="Save" Icon="{x:Static mav:MavIcons.Save}" />
    </StackPanel>
  </mav:MavScrollViewer>
</mav:MavWindow>
```

`MavThemeStyle` is a `Style` that forwards its properties to the static `MavTheme` state, so it is
equivalent to calling `MavTheme.Configure`. You still need `UseMav()` in `Program.cs` for fonts.
The base `Window` from `https://github.com/avaloniaui` has no template without an Avalonia theme,
so prefer `mav:MavWindow` and `mav:MavScrollViewer` in theme-free apps.

### 1.5 Fonts and icons

Bundled fonts:

| Font | Access | Notes |
|---|---|---|
| Material Icons | `MavFonts.MaterialIconsFont` | classic icon set |
| Material Symbols Rounded | `MavFonts.MaterialSymbolsFont` | variable-style symbols |
| Material Design Icons | `MavFonts.MaterialDesignIconsFont` | large community set |
| Roboto | `MavFonts.RobotoFont` | default text font |

* `MavFonts.ResolveIconFont(glyph)` / `MavFonts.ResolveIconTypeface(glyph)` pick the right family by codepoint.
* `MavIcon` does this automatically; normally you only pass `MavIcons.*`.
* `MavIcons` contains ~10,800 constants (e.g. `MavIcons.Home`, `MavIcons.Settings`, `MavIcons.ArrowBack`,
  `MavIcons.Favorite`, `MavIcons.Add`, `MavIcons.Check`, `MavIcons.Close`, `MavIcons.Delete`,
  `MavIcons.Search`, `MavIcons.Menu`, `MavIcons.MoreVert`, `MavIcons.ChevronRight`).
* Use `<mav:MavIcon Glyph="{x:Static mav:MavIcons.Home}" Size="24" />` or `new MavIcon(MavIcons.Home, 24)`.

Icon font sets are chosen at **build time** with the `MavFonts` MSBuild property:

| `MavFonts` | Embedded | `MavUI.dll` |
|---|---|---|
| `all` (default) | Roboto + Symbols + Material Icons + Material Design Icons | ~4.87 MB |
| `no-mdi` | Roboto + Symbols + Material Icons | ~3.69 MB |
| `minimal` | Roboto + Material Symbols only | ~3.35 MB |

```bash
dotnet build MavUI/MavUI.csproj -p:MavFonts=no-mdi
```

`no-mdi` is the safe size reduction for apps that use classic `MavIcons` (the demo).
`minimal` only works for Material Symbols glyphs; classic icon codepoints (`MavIcons.Home`, …)
will fail to create a glyph typeface if the classic font is excluded.

### 1.7 Trimming and AOT

The library sets `IsTrimmable` and `IsAotCompatible` and builds without trim warnings.
Trimmed/AOT apps work, with one caveat: `DisplayMemberPath` and `MavDataColumn.PropertyName`
use reflection on application model types. In trimmed apps either root those properties
(`DynamicDependency`/`rd.xml`) or use `ItemTemplate`/`ItemFactory` and
`MavDataColumn.ValueSelector`, which are trim-safe.

Android Release publish (trimming and AOT are on by default in Release):

```bash
dotnet publish samples/ControlsGallery/ControlsGallery.Android/ControlsGallery.Android.csproj -c Release \
    -p:PublishTrimmed=true -p:MavFonts=no-mdi \
    -p:AndroidPackageFormats=apk -o artifacts/android
```

Measured on the demo: 65.60 MB (no trim/AOT) → 27.71 MB (trimmed+AOT, `no-mdi`).

### 1.6 Localization

Picker dialogs, tooltips, accessibility labels and built-in action buttons use
`MavLocalization`. Apps configure it **once** and never subscribe to events.

#### Provider

Point the library at your resources at startup. Missing keys fall back to the
English built-ins, so you only need to translate what you use:

```csharp
using System.Globalization;
using System.Resources;

// generated .resx designer class
MavLocalization.Current = new MavLocalization.ResourceManagerProvider(
    Strings.ResourceManager);
```

Other providers:

```csharp
// small dictionary / tests
MavLocalization.Current = new MavLocalization.DictionaryProvider(
    new Dictionary<string, string> { ["Ok"] = "Хорошо" });

// custom source (JSON, database, …); return null for a missing key
MavLocalization.Current = new MavLocalization.DelegateProvider(
    (key, culture) => MyCatalog.Lookup(key, culture));

// app resources first, then another provider
MavLocalization.Current = new MavLocalization.ChainedProvider(
    appProvider, new MavLocalization.DefaultProvider());
```

The provider contract is `string? GetString(string key, CultureInfo? culture)`.
Return `null`/empty for a missing key so `MavLocalization.Get(..., fallback)` can
use its fallback. All keys are available as constants in `MavLocalization.Keys`
(`Keys.Ok`, `Keys.SelectDate`, `Keys.PreviousPage`, …).

#### Switching language

```csharp
// also updates CurrentCulture for dates/numbers
MavLocalization.SetCulture(new CultureInfo("ru-RU"));

// UI strings only, keep the formatting culture
MavLocalization.SetCulture(new CultureInfo("ru-RU"), setFormattingCulture: false);
```

`SetCulture` refreshes every observed text automatically. If the culture or the
provider data is changed outside the library, call `MavLocalization.Refresh()`.

#### Application strings

Attach once; no event handlers or manual re-assignment:

```csharp
var title = new TextBlock().Localize("Title", "Control gallery");
var save = new MavButton("Save", MavButtonVariant.Filled).Localize("Save");
var window = new Window().Localize("AppTitle", "My app");

var name = new MavTextField()
    .LocalizeLabel("Name")
    .LocalizePlaceholder("NameHint");
```

`Localize` supports `TextBlock`, `MavButton` and `Window`; `LocalizeLabel` /
`LocalizePlaceholder` handle `MavTextField`. For any other property use the generic
observer:

```csharp
MavLocalization.Observe(statusLabel, label =>
    label.Text = MavLocalization.Get("Status", "Ready"));
```

Observers run immediately, re-run when the provider/culture changes while the
control is attached, and unsubscribe when it leaves the visual tree. Complex
strings can use `MavLocalization.Format("Rows", "{0} rows", count)`.

#### AXAML

The shortest form is the `{mav:Loc ...}` markup extension. It works on any
property and updates itself after `SetCulture` or a provider change:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:mav="https://where.it/mav"
        Title="{mav:Loc AppTitle, Fallback=My app}">
  <StackPanel>
    <TextBlock Text="{mav:Loc Greeting, Fallback=Hello}" />
    <mav:MavButton Text="{mav:Loc Save}" />
    <mav:MavTextField Label="{mav:Loc Name}" />
    <mav:MavTextField Placeholder="{mav:Loc NameHint}" />
    <Button ToolTip.Tip="{mav:Loc MoreOptions}" />
  </StackPanel>
</Window>
```

`Fallback` is optional. No `Target` property is needed: the markup extension
knows the exact target property from XAML.

For scenarios where the key must be changed at runtime, the attached properties
`mav:MavLocalization.Key` / `.Fallback` / `.Target` remain available. `Target=Auto`
(default) maps a `TextBlock` to `Text`, `MavButton` to `Text`,
`Window`/`MavDialog` to `Title`, `MavTextField` to `Label`, `MavSelect` to `Label`,
and `MavSearchBar` to `Placeholder`. Use `Text`, `Label`, `Placeholder`, `Title`
or `Tooltip` when the natural property is not the one you want.

`MavLocalization.Changed` still exists for advanced scenarios, but application
code does not need it. Built-in dialogs read the current language when they open;
dynamic defaults (e.g. a dialog's OK button) are resolved at show time.

---

## 2. Core conventions

### 2.1 Namespaces

```csharp
using Mav;             // MavFonts, MavIcons, MavColors, MavLocalization
using Mav.Controls;    // all UI controls
using Mav.Primitives;  // MavContentView, MavSurface, MavInkSurface, MavIcon
using Mav.Theming;     // MavTheme, tokens, typography, motion
using Mav.Platform;    // MavOverlay, native inputs
```

`MavColors` and `MavThemeStyle` both live in `Mav.Theming`.

### 2.2 `MavContentView` — the single-child base

Most controls derive from `MavContentView`, which is a `Border` with one child:

```csharp
var card = new MavCard(variant: MavCardVariant.Filled)
{
    Content = new MavLabel { Text = "Body" },
    BackgroundColor = MavTheme.ColorOf(MavColorRole.SurfaceContainer),
};
```

* `Content` is the child (same as `Child`).
* `BackgroundColor` is a convenience wrapper over `Background`; transparent means `null`.
* Custom-drawn controls (`MavSwitch`, `MavSlider`, `MavCarousel`, `MavTabs`, `MavSparkline`, …)
  derive from `Control` and override `Render`; they have no `Content` property.

Useful containers:

| Type | Purpose |
|---|---|
| `MavSurface` | rounded fill + optional surface tint + border + MD3 two-layer elevation |
| `MavInkSurface` | clickable surface with ripple, hover/focus/pressed state layer, `Command` |
| `MavSafeArea` | applies safe-area insets (`Edges`, `SimulatedInset`, `BasePadding`) |
| `MavKeyboardAvoidingHost` | shifts content above the soft keyboard and auto-scrolls the focused field |
| `MavScrollViewer` | theme-independent `ScrollViewer`; touch/wheel/keyboard scrolling, no visible scrollbars |
| `MavWindow` | desktop window with a built-in theme-free template and overlay layer |

### 2.3 Events, commands, and property changes

* Every clickable control exposes both a CLR event (e.g. `Clicked`, `Toggled`, `SelectedIndexChanged`)
  and an `ICommand` (`Command` + `CommandParameter`). **The event fires first, then the command.**
* Programmatic property changes can raise change events. In particular `MavTextField.TextChanged`,
  `MavTextArea.TextChanged`, `MavSearchBar.TextChanged`, `MavPinInput.CodeChanged`, and
  `MavFormValidator` subscribed fields fire for both user and code changes. Guard with a flag if needed.
* Use direct collection properties when building UI in code (`MavTabs.Tabs`, `MavStepper.Steps`,
  `MavChipGroup.Chips`, `MavList` children, `MavDataTable.Columns`, …). They are `AvaloniaList<T>`
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

* `MavGridExtensions.Add(child, column, row, columnSpan, rowSpan)` avoids repetitive `Grid.Set*` calls.
* `MavMotion` extension methods (`SetTranslation`, `SetScale`, `SetRotation`) attach a shared
  `TransformGroup` to a `Visual`.
* Use `Spacing` on `StackPanel`s and `ColumnSpacing`/`RowSpacing` on `Grid`s for MD3 rhythm.

### 2.6 Accessibility

Mav controls fill Avalonia automation properties from their visible text, so screen readers
announce meaningful names without extra code:

* buttons use `Text`; icon-only buttons (`MavIconButton`, `MavFab`) use the tooltip when set;
* fields use `Label` as `Name` and the footer (`ErrorText`, else `SupportingText`) as `HelpText`;
* toggles and selection controls expose their `Label` and the matching role
  (`CheckBox`, `RadioButton`, `Slider`, `ComboBox`, `ListItem`, `MenuItem`);
* `MavListItem` exposes `Headline` + `SupportingText`, dialogs/sheets expose `Title`/`Message`;
* `MavValidation.SetIsRequired(control, true)` also sets `IsRequiredForForm`.

Use the helpers when building custom UI:

```csharp
MavAutomation.SetName(control, "Delete project");
MavAutomation.SetHelpText(control, "This action cannot be undone");
MavAutomation.SetRequired(control, true);
MavAutomation.SetControlType(control, AutomationControlType.Button);
```

Explicit `AutomationProperties` values set by the application are never overwritten. Keyboard
activation is built in: Space/Enter on buttons, chips and list/menu items (via `MavInkSurface`),
Space/Enter on switches/checkboxes/radios, and arrow keys on sliders. `MavInkSurface` draws the
MD3 focus ring only for keyboard focus.

Contrast helpers live in `Mav.Theming.MavAccessibility`:

```csharp
double ratio = MavAccessibility.ContrastRatio(theme.Color.OnSurface, theme.Color.Surface);
bool ok = MavAccessibility.MeetsContrast(fg, bg);              // WCAG AA body text (4.5)
bool largeOk = MavAccessibility.MeetsContrast(fg, bg, 3.0);    // large text / UI (3.0)
```

### 2.7 Gesture zones

On Android, horizontal drags must not steal the system back gesture. `MavGestureZones`
reserves the first/last 20 dp of the top level:

```csharp
if (MavGestureZones.ShouldDeferToSystem(this, e))
    return; // let TopLevel.BackRequested / the platform gesture win

bool atEdge = MavGestureZones.IsInSystemEdge(visual, point);            // 20 dp default
bool custom = MavGestureZones.IsInSystemEdge(visual, point, edgeWidth: 28);
```

Built-in draggable controls already honor it: `MavListItem` swipe actions, `MavCarousel`,
`MavSlider`/`MavRangeSlider`, `MavSplitView`, `MavColorPicker`, `MavTimeDial`, `MavRadialGauge`.
Mouse input is not constrained (desktop).

---

## 3. Theming

### 3.1 Configure

```csharp
MavTheme.Configure(
    seed: Color.Parse("#006A6A"),
    mode: MavThemeMode.System,
    contrast: MavContrast.Standard,
    density: MavDensity.Comfortable,
    useSystemAccentColor: false,
    schemeVariant: MavSchemeVariant.TonalSpot,
    motion: MavMotionScheme.Expressive,
    shapeScale: MavShapeScale.Standard);
```

Equivalent object/fluent forms:

```csharp
var options = new MavThemeOptions
{
    SeedColor = MavColors.Teal,
    Mode = MavThemeMode.Dark,
    SchemeVariant = MavSchemeVariant.Vibrant,
    Contrast = MavContrast.Medium,
    Density = MavDensity.Compact,
    Motion = MavMotionScheme.Reduced,
    ShapeScale = MavShapeScale.Expressive,
};
MavTheme.Configure(options);

// or
MavTheme.Configure(o =>
{
    o.SeedColor = MavColors.Indigo;
    o.Mode = MavThemeMode.Light;
});
```

All configuration is global and rebuilds the palette immediately. Call it on the UI thread.

Convenience mode switches:

```csharp
MavTheme.UseLight();
MavTheme.UseDark();
MavTheme.UseSystemMode();
MavTheme.ToggleDark();
```

### 3.2 `MavTheme` API

| Member | Meaning |
|---|---|
| `MavTheme.Current` | current `MavTheme` snapshot |
| `MavTheme.Current.Color` | `MavColorScheme` with all roles |
| `MavTheme.Current.Typography` | 15-role type scale |
| `MavTheme.Current.Shape` | `MavShapeTokens` (corner radii) |
| `MavTheme.Current.Elevation` | `MavElevationTokens` (levels 0–5, `ToBoxShadows`) |
| `MavTheme.Current.StateLayer` | hover/focus/pressed/dragged/disabled opacities |
| `MavTheme.Current.IsDark` | resolved darkness |
| `MavTheme.ColorOf(role)` / `BrushOf(role)` | cached color/brush lookup |
| `BrushOf(Color)` / `PenOf(Color, thickness)` | cached brushes/pens for custom drawing |
| `MavTheme.Changed` | event fired after every rebuild |
| `MavTheme.Observe(view, (v, theme) => …)` | run now and on every theme change while attached |
| `MavTheme.ApplySeedFromPixels(...)` | extract a seed color from ARGB pixels |
| `MavTheme.ApplySeedFromRgba(...)` | extract a seed color from RGBA bytes |
| `MavTheme.ApplyCustomSchemes(light, dark)` | override both palettes |
| `MavTheme.ClearCustomSchemes()` | return to generated palettes |

`MavTheme.Changed` fires after every rebuild. For MVVM/DI, `MavThemeService.Instance` (`IMavThemeService`)
wraps the same state with `INotifyPropertyChanged` and a `ThemeChanged` event.

**Drawing custom controls:** always read colors from `MavTheme.Current` inside `Render`, or subscribe
with `MavTheme.Observe` and call `InvalidateVisual()`. Cache brushes via `MavTheme.BrushOf`, not `new SolidColorBrush`.

```csharp
public class AccentBar : Control
{
    public override void Render(DrawingContext context)
    {
        var c = MavTheme.Current.Color;
        context.DrawRectangle(MavTheme.BrushOf(c.PrimaryContainer), null,
            new Rect(0, 0, Bounds.Width, Bounds.Height), 8, 8);
    }
}
```

### 3.3 Color roles

`MavColorScheme` exposes Material 3 roles. `Get(role)` and the indexer return the current color;
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

`MavSchemeVariant`: `TonalSpot` (default), `Neutral`, `Vibrant`, `Expressive`, `Monochrome`,
`Fidelity`, `Content`, `Rainbow`, `FruitSalad`.

`MavContrast`: `Standard`, `Medium`, `High`.

`MavColorScheme.FromSeed(seed, dark, contrast, variant)` generates a full palette manually.
`MavColorScheme.FromImagePixels(...)` / `MavColorScheme.ExtractSeedFromPixels(...)` derive a seed
from an image. `MavColorScheme.Blend(a, b, amount)` blends colors.

Custom schemes:

```csharp
var light = MavColorScheme.FromSeed(MavColors.Green, dark: false);
var dark  = MavColorScheme.FromSeed(MavColors.Green, dark: true);
MavTheme.ApplyCustomSchemes(light, dark);
```

### 3.5 Typography

15 roles in `MavTypeRole`:
`DisplayLarge/Medium/Small`, `HeadlineLarge/Medium/Small`, `TitleLarge/Medium/Small`,
`BodyLarge/Medium/Small`, `LabelLarge/Medium/Small`.

```csharp
var title = new MavLabel
{
    Text = "Section",
    Role = MavTypeRole.TitleLarge,
    ColorRole = MavColorRole.Primary,
};

// For any TextBlock:
var text = new TextBlock { Text = "Body copy" };
text.Apply(MavTypeRole.BodyMedium);

// Or apply a full style:
MavTypography.Default.BodyMedium.ApplyTo(text);
```

`MavLabel` is a `TextBlock` with `Role`, `ColorRole`, `Secondary` (uses `OnSurfaceVariant`), and
`HorizontalTextAlignment`. `MavTypography.BrandFont` / `PlainFont` are `init`-only; the default is Roboto.

### 3.6 Shape, elevation, state, density

`MavTheme.Current.Shape` (an `MavShapeTokens`):

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

`MavShapeScale`: `Compact` (×0.72), `Standard` (×1.0), `Expressive` (×1.28).

`MavTheme.Current.Elevation`: `Level0`=0, `Level1`=1, `Level2`=3, `Level3`=6,
`Level4`=8, `Level5`=12. `MavElevationTokens.ToBoxShadows(level)` builds the two-layer shadow.

`MavTheme.Current.StateLayer`: `Hover`=0.08, `Focus`=0.10, `Pressed`=0.12,
`Dragged`=0.16, `DisabledContainer`=0.12, `DisabledContent`=0.38.

`MavDensity`: `Comfortable` (0), `Compact` (-1), `Dense` (-2). It is stored on `MavTheme.Current.ScaleDensity`
for controls that consult it; do not assume every control changes size.

### 3.7 Motion

`MavMotion` constants: `Short1..4` (50/100/150/200 ms), `Medium1..4` (250/300/350/400),
`Long1..4` (450/500/550/600), `ExtraLong1..4` (700/800/900/1000).

Easings: `Linear`, `CubicIn`, `CubicOut`, `CubicInOut`, `SinInOut`, `SpringOut`,
`Standard`, `StandardDecelerate`, `StandardAccelerate`, `EmphasizedDecelerate`,
`EmphasizedAccelerate`.

`MavMotionScheme`: `Standard`, `Expressive` (default), `Reduced`, `None`.
`Reduced` caps durations at ~90 ms; `None` completes instantly.

```csharp
MavMotion.Animate(control, "fade", 0, 1, MavMotion.Medium2, MavMotion.CubicOut,
    v => control.Opacity = v);

MavMotion.FadeIn(control);
MavMotion.ScaleIn(control, from: 0.94);
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

`MavOverlay` uses the current `TopLevel`'s `OverlayLayer`. When the top level has no themed
`OverlayLayer` (Android without an Avalonia theme, or a headless window), it falls back to the
overlay host registered by the nearest `MavNavigationPage`.

* `MavOverlay.Show(anchor, content, scrimColor, onScrimTap, passThroughScrim, onViewportChanged)`
  adds full-window content and returns an `IDisposable` that closes it.
* `onViewportChanged` is invoked after a window resize, `ScalingChanged`,
  `InsetsManager.SafeAreaChanged`, `IInputPane.StateChanged` (soft keyboard) and root
  `SizeChanged`, coalesced through `Dispatcher.UIThread.Post`. Open overlays re-clamp
  themselves without re-running entrance animations; pass it for custom overlay content
  that computes geometry once in `Show`.
* `MavOverlay.ShowAnchoredPopup(anchor, popupCard, preferredWidth, maxHeight, verticalOffset, onDismiss)`
  clamps to safe-area bounds, flips above the anchor when needed and re-places itself on
  viewport changes.
* `MavOverlay.SafeAreaOf(visual)` returns the top level's `IInsetsManager.SafeAreaPadding`;
  `MavOverlay.SafeAreaBounds(visual)` returns the overlay rect without insets.
  `BoundsInOverlay(visual, root)` gives anchor coordinates in overlay space.
* The soft keyboard lifts the overlay root (real `IInputPane`, or
  `MavKeyboardAvoidingHost.SimulatedKeyboardHeight` / `MAV_KEYBOARD` on desktop).
* Escape / hardware Back dismisses the most recent dismissable overlay (`PushDismissable`).
* The anchor may be any attached `Visual`; if it is `null`, the last known `TopLevel` is used.
  If no `TopLevel` has been seen yet (for example in a test before the window is tracked), the overlay
  is parked in an internal pending queue (`PendingShows`, at most 8). A pending overlay is **not** on the
  dismiss stack: `HasActiveOverlay` stays `false` and `TryDismissTop()` returns `false` until a host appears.
  Always pass an attached control in tests.
  Call overlay APIs only after a window/view exists — not in `Application.Initialize()`.
* Overlays opened from a page are dismissed when that page disappears (`MavOverlay.DismissForPage`,
  called by `MavContentPage` on disappearing).
* The fallback host inside `MavNavigationPage` is hit-test invisible while no overlay is open, so it never
  blocks taps on the page. Do not add your own full-window hit-testable root for overlays.
* Closing is animated (`MavMotion`). `Closed` and the removal from the overlay layer happen when the
  animation ends, not synchronously inside `Close()`/`Dismiss()` (for example `MavSideSheet`).

**Popup controls are launcher handles, not layout children.** `MavDialog`, `MavBottomSheet`,
`MavSideSheet`, `MavPopover`, `MavMenu`, `MavRichTooltip`, `MavDatePicker`, `MavTimePicker`,
`MavSearchView`, and `MavModalNavigationDrawer` are zero-size `Control`s whose `Show(anchor)`
method places their UI in the overlay. Do **not** add them to a panel expecting visible content.

```csharp
var dialog = new MavDialog
{
    Title = "Discard changes?",
    Message = "Your edits will be lost.",
    ConfirmText = "Discard",
    DismissText = "Keep editing",
};
dialog.Show(this);               // this = Window/Control currently in the tree
```

If the anchor is detached (e.g. a page that was navigated away), keep a reference to the live
window or call `MavOverlay.TrackTopLevel(liveControl)` first.

### 4.2 Dialogs

Instance API:

```csharp
var dialog = new MavDialog
{
    Icon = MavIcons.Warning,
    Title = "Delete project?",
    Message = "This action cannot be undone.",
    ConfirmText = "Delete",
    DismissText = "Cancel",
    NeutralText = "Archive",
    DismissOnScrimTap = false,
    Body = new MavCheckBox("Also delete files"),
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
await MavDialog.ShowAlertAsync("Saved", "Your changes are now live.", "OK", MavIcons.Check);

bool ok = await MavDialog.ShowConfirmAsync(
    "Publish?", "This will make the page public.", "Publish", "Cancel", MavIcons.Public);

using IDisposable full = MavDialog.ShowFullscreen(
    "Editor", editorControl, "Done", () => Save());
```

`MavDialogService.Instance` exposes `ShowAlertAsync`/`ShowConfirmAsync` without anchor arguments
for view-model code (`IMavDialogService`).

### 4.3 Sheets

```csharp
var sheet = new MavBottomSheet
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

`MavSideSheet` adds `Footer`, `Placement` (`Start`/`End`), `SheetWidth`, `ShowBackButton`,
`BackCommand`, `BackClicked`, `Closed`, and `Show`/`ShowCurrent` overloads.

### 4.4 Menus, popovers, rich tooltips

```csharp
var menu = new MavMenu()
    .AddItem("Rename", MavIcons.Edit, onSelected: Rename)
    .AddItem("Duplicate", MavIcons.ContentCopy, onSelected: Duplicate)
    .AddDivider()
    .AddItem("Delete", MavIcons.Delete, onSelected: Delete);
menu.ItemSelected += (_, item) => { };
menu.Show(anchor);       // or menu.Close();
```

Nested menus: `AddSubmenu("Move to", sub => sub.AddItem("Archive"), MavIcons.DriveFileMove)`.
Each `MavMenuItem` also supports `Command`, `IsSelected`, `IsDivider`, `TrailingText`, and `SubItems`.

Menus and popovers clamp to the current overlay: menus keep an 8 dp edge margin, popovers
12 dp, and both measure their body to pick a natural width, so they stay inside a 320 dp
phone viewport. A tall body scrolls inside the card instead of growing past the overlay.

```csharp
var popover = new MavPopover
{
    Title = "Details",
    Content = detailsPanel,
    Placement = MavPopoverPlacement.Bottom,
    DismissOnOutsideTap = true,
};
popover.Opened += (_, _) => { };
popover.Closed += (_, _) => { };
popover.Show(anchor);    // Toggle(anchor) / Close()
```

`MavRichTooltip`:

```csharp
MavRichTooltip.Show(anchor, "Keyboard shortcut", "Ctrl+K opens search",
    actionText: "Got it", onAction: null);
```

Use `MavFlyout.SetMenu(button, menu)` (or `Popover`, `RichTooltip`, `BottomSheet`, `SideSheet`,
`Dialog`, `ModalDrawer`) to open declaratively when a button/chip/list-item/fab is clicked.
This is the recommended AXAML-friendly alternative to manual `Show` calls.

### 4.5 Snackbar

`MavSnackbar` is a **static global FIFO**; only one snackbar is visible at a time.

```csharp
MavSnackbar.Show("Message deleted", "Undo", () => Restore(), duration: MavSnackbarDuration.Short);

// Queue instead of replacing:
MavSnackbar.Enqueue("Uploading…", duration: MavSnackbarDuration.Indefinite);
MavSnackbar.Dismiss();
MavSnackbar.ClearQueue();
int pending = MavSnackbar.QueueCount;
```

* `Show` clears the queue and replaces the current snackbar.
* `Enqueue` appends; if nothing is active it shows immediately.
* Durations: `Short` (3000 ms), `Long` (5500 ms), `Indefinite` (0 = stays until dismissed).
  Indefinite snackbars always render a close icon, even when `showCloseIcon` is not set, so
  they can be dismissed on touch and web where there is no Escape key.
* `MavSnackbarService.Instance` provides the same API for view models (`IMavSnackbarService`).

### 4.6 Simple tooltips

```csharp
MavTooltip.SetText(saveButton, "Save (Ctrl+S)");
MavTooltip.SetPlacement(saveButton, MavTooltipPlacement.Bottom);
MavTooltip.Attach(saveButton, "Save", MavTooltipPlacement.Top);
MavTooltip.Show(anchor, "Copied", durationMs: 1500);
```

`MavTooltip` has one global handle; a new tooltip replaces the visible one.

### 4.7 Search

```csharp
var search = new MavSearchBar
{
    Placeholder = "Search…",
    Text = "",
};
search.SearchSubmitted += (_, query) => RunSearch(query);
search.TextChanged += (_, query) => Filter(query);

MavSearchView view = search.AttachSearchView(new[] { "Inbox", "Archive" }, fullScreen: false);
```

Or use `MavSearchView` directly with `Suggestions`, `SuggestionsSource`, `ResultItemTemplate`,
`RecentSearches`, `QueryChanged`, `QuerySubmitted`, `ResultSelected`, `Show`/`Close`.

---

## 5. Control catalog

### 5.1 Buttons and actions

| Control | Purpose and key API |
|---|---|
| `MavButton` | text/icon button. `new MavButton(text, variant, icon, size)`; `Variant` (`Filled`, `Tonal`, `Elevated`, `Outlined`, `Text`), `Size` (`ExtraSmall`…`ExtraLarge`), `Icon`, `TrailingIcon`, `ButtonShape`/`Shape` (`Round`/`Square`), `IsToggle`, `IsSelected`, `IsLoading`, `Command`, `Clicked`, `SelectedChanged`, `Toggled`. |
| `MavIconButton` | icon-only button. `new MavIconButton(icon, variant, isToggle)`; `SelectedIcon`, `ButtonSize`, `TouchTargetSize` (48 dp by default, the clickable area even when the visual is smaller). |
| `MavFab` | floating action button. `new MavFab(icon, size, color)`; `Size` (`Small`/`Regular`/`Large`), `ColorScheme` (`Primary`/`Secondary`/`Tertiary`/`Surface`). |
| `MavExtendedFab` | FAB with label. `Text`, `Icon`, `IsExpanded`. |
| `MavButtonGroup` | `StackPanel` of buttons. `Kind`/`Variant` (`Standard`/`Connected`), `MultiSelect`, `AddButton`, `SelectionChanged(int)`. |
| `MavSplitButton` | primary action + menu arrow. `Menu` (`MavMenu`), `IsMenuOpen`, `MenuCommand`, `Clicked`, `MenuClicked`. |
| `MavFabMenu` | FAB that expands into labeled actions. `AddItem(label, icon, onClick)`, `Open`/`Close`/`Toggle`, `ItemClicked`, `OpenChanged`. |
| `MavFabMenuItem` | action inside a FAB menu. `Label`, `Icon`, `OnClick`, `Clicked`, `Command`, `CommandParameter`. |

```csharp
var more = new MavIconButton(MavIcons.MoreVert);
more.Clicked += (_, _) => menu.Show(more);

var fab = new MavFab(MavIcons.Add, MavFabSize.Regular, MavFabColor.Primary);
fab.Clicked += (_, _) => CreateItem();

var group = new MavButtonGroup { Kind = MavButtonGroupKind.Connected, MultiSelect = false };
group.AddButton(new MavButton("Day", MavButtonVariant.Outlined));
group.AddButton(new MavButton("Week", MavButtonVariant.Outlined));
```

### 5.2 Text input and selection

| Control | Purpose and key API |
|---|---|
| `MavTextField` | single-line text field. `new MavTextField(label, variant, placeholder, leadingIcon, supportingText)`; `Text`, `Placeholder`, `Variant` (`Filled`/`Outlined`), `LeadingIcon`, `TrailingIcon`, `PrefixText`, `SuffixText`, `ErrorText`, `HasError` (also true when `ErrorText` set), `IsPassword`, `IsReadOnly`, `ShowClearButton`, `MaxLength`, `ShowCounter`/`ShowCharacterCounter`, `ContentType`, `ReturnKeyType`, `AutoCapitalize`, `Completed`, `TrailingIconClicked`, `Focus()`/`Unfocus()`. |
| `MavTextArea` | multiline field. `new MavTextArea(label, variant, placeholder, supportingText, minHeight)`; `MaxLength`, `ShowCounter`, `MinBoxHeight`, `TextChanged`; sends `ReturnKeyType`/newline as text input options. |
| `MavTextContextMenu` | static `Attach(textBox, mode = Auto)` adds the Material copy, cut, paste and select-all menu to a `TextBox` and returns a `MavTextContextFlyout`. |
| `MavPinInput` | PIN/OTP boxes. `Length`, `Code`, `Value`, `Text` (aliases), `IsMasked`, `HasError`, `CodeChanged`, `Completed`, `Clear()`; numeric keyboard and `Done` return key. |
| `MavNumberBox` | numeric stepper. `Value`, `Minimum`, `Maximum`, `Step`, `DecimalPlaces`, `Label`, `Suffix`, `FormatString`, `ValueChanged`, `Increment`, `Decrement`. |
| `MavTagInput` | chip/tag entry. `Tags`, `Label`, `Placeholder`, `MaxTags`, `AllowDuplicates`, `TagAdded`, `TagRemoved`, `TagsChanged`, `AddTag`, `RemoveTag`, `ClearTags`. |
| `MavRatingBar` | star rating. `Value`, `MaxRating`, `AllowHalf`/`AllowHalfStars`, `IsReadOnly`, `IconSize`/`ItemSize`, `ValueChanged`. |
| `MavSwitch` | toggle. **`IsToggled`** (not `IsOn`), `ShowIcons`, `OnIcon`, `OffIcon`, `Toggled(bool)`. |
| `MavCheckBox` | checkbox. `IsChecked`, `IsIndeterminate`, `IsThreeState`, `Label`, `State` (`Unchecked`/`Checked`/`Indeterminate`), `CheckedChanged`, `StateChanged`, `Toggle()`. |
| `MavRadioButton` | radio button. `IsChecked`, `Label`, `Value`, `GroupName`, `Command`. |
| `MavRadioGroup` | `StackPanel` of radios. `SelectedIndex`, `SelectedItem`, `SelectedValue`, `ItemsSource`, `AddOption(label, value, selected)`, `AddRadio`. |
| `MavSlider` | single-value slider. `Minimum`, `Maximum`, `Value`, `Step`, `Divisions`, `Centered`, `ShowValueIndicator`/`ShowLabel`/`ShowValueLabel`, `ValueFormatter`, `ValueChanged`. |
| `MavRangeSlider` | two-thumb slider. `StartValue`/`EndValue` (aliases `LowerValue`/`UpperValue`), `Step`, `Divisions`, `ValueFormatter`. |
| `MavSegmentedButton` | connected segmented control. `MultiSelect`, `AddSegment(text, icon, selected)`, `Segments`, `SelectedIndex`, `SelectedIndices`, `SelectedItem`, `SelectedItemsSource`, `SelectionChanged`. |
| `MavSegment` | segment item. `Text`, `Label`, `Icon`, `IsSelected`. |
| `MavChip` | chip. `new MavChip(text, kind, icon, selected, elevated)`; `Kind` (`Assist`/`Filter`/`Input`/`Suggestion`), `IsSelected`, `IsElevated`, `ShowCloseIcon`/`IsRemovable`, `AvatarView`, `Clicked`, `SelectedChanged`, `CloseClicked`. |
| `MavChipGroup` | chip collection. `AddChip(text, kind, icon, selected, elevated)`, `Chips`, `ItemsSource`, `SingleSelection`, `SelectionRequired`, `Wrap`, `SelectedIndex`, `SelectedItem`, `SelectedIndices`, `SelectedTexts`, `SelectionChanged`. |
| `MavSelect` | dropdown select. `new MavSelect(label, options, variant, leadingIcon)`; `Options`, `SelectedItem` (string), `SelectedObject`, `SelectedIndex`, `ItemsSource`, `DisplayMemberPath`, `ItemFormatter`, `ErrorText`, `IsClearable`, `OpenMenu()`. |
| `MavComboBox` | editable combo box. `new MavComboBox(label, items, variant, leadingIcon)`; `Text`, `Items`, `ItemsSource`, `SelectedItem`, `SelectedIndex`, `IsClearable`, `OpenDropdown(filter)`, `CloseDropdown`, `TextChanged`, `SelectedItemChanged`. |

```csharp
var remember = new MavSwitch { IsToggled = true, ShowIcons = true };
remember.Toggled += (_, on) => Settings.Remember = on;

var role = new MavSelect("Role", new[] { "Admin", "Editor", "Viewer" })
{
    SelectedIndex = 0,
};
role.SelectedIndexChanged += (_, i) => { };

var rating = new MavRatingBar { MaxRating = 5, AllowHalf = true };
```

### 5.3 Date, time, and color

| Control | Purpose and key API |
|---|---|
| `MavDatePicker` | modal calendar handle. `SelectedDate`, `RangeStart`/`RangeEnd`, `IsRange`, `MinDate`/`MaxDate`, `SelectableDayPredicate`, `StartInInputMode`, `DateSelected`, `RangeSelected`, `Dismissed`, `Show(anchor)`, `Close()`. |
| `MavDatePickerField` | inline field that opens the picker. `SelectedDate`, `Format`, `MinDate`/`MaxDate`, `IsClearable`, `ErrorText`, `SelectedDateChanged`. |
| `MavDateRangePickerField` | range field. `StartDate`, `EndDate`, `Format`, `RangeChanged`, `RangeSelected`. |
| `MavCalendarGrid` | embedded month grid. `SelectedDate`, `RangeStart`/`RangeEnd`, `IsRange`, `SelectableDayPredicate`, `DateSelected`. |
| `MavTimePicker` | modal clock/input picker. `SelectedTime`, `Is24Hour`, `StartInInputMode`, `TimeSelected`, `Dismissed`, `Show`/`Close`. |
| `MavTimePickerField` | inline time field. `SelectedTime`, `Is24Hour`, `SelectedTimeChanged`. |
| `MavTimeDial` | clock dial. `Hour`, `Minute`, `Is24Hour`, `Mode` (`Hour`/`Minute`), `TimeChanged`, `HourSelectionCompleted`. |
| `MavColorPicker` | HCT color picker. `SelectedColor`, `ShowHexInput`, `ShowHctSliders`, `ShowPresetSwatches`, `ColorChanged`. |

Static modal helpers (await them):

```csharp
DateTime? date  = await MavDatePicker.PickDateAsync(DateTime.Today, minDate: DateTime.Today);
var range       = await MavDatePicker.PickDateRangeAsync(start: DateTime.Today);
TimeSpan? time  = await MavTimePicker.PickTimeAsync(new TimeSpan(9, 0, 0), is24Hour: true);
Color? color    = await MavColorPicker.PickAsync(anchor, Colors.Teal, "Accent color");
```

For forms, prefer the fields — they render a normal MD3 field and handle open/clear/format:

```csharp
var due = new MavDatePickerField
{
    Label = "Due date",
    Format = "dd MMM yyyy",
    MinDate = DateTime.Today,
    IsClearable = true,
};
```

### 5.4 Navigation

The library ships its **own Material page stack**. Do not use Avalonia
`NavigationPage`/`ContentPage` for Mav screens: they constrain the chrome to the
built-in page template. Use `MavContentPage` + `MavNavigationPage`, which keep the
app bar, actions, bottom bar and FAB fully Material.

| Control | Purpose and key API |
|---|---|
| `MavContentPage` | page metadata + content. `Title`, `Subtitle`, `NavigationIcon`, `ShowAppBar`, `ShowBackButton`, `AppBarVariant`, `AddAction(icon, onClick)`, `BottomBar`, `FloatingActionButton`, `NavigationAction`, `Navigation`, `Appearing`/`Disappearing`, `OnBackPressed()`. |
| `MavNavigationPage` | own page stack. `SetRoot`, `Push`, `Pop`, `PopToRoot`, `Replace`, `TryGoBack`, `GoBackCommand`, `RefreshChrome`, `NavigationStack`, `CurrentPage`, `CanGoBack`, `StackDepth`, `Transition` (`None`/`Slide`/`Fade`), `PageChanged`; `ApplySafeArea`, `SafeAreaOverride`, `SafeAreaInsets`, `KeyboardHost`. Uses `MavTopAppBar` and `MavMotion` transitions and lifts the page above the soft keyboard. |
| `MavTopAppBar` | top app bar. `Title`, `Subtitle`, `NavigationIcon`, `Variant` (`CenterAligned`/`Small`/`Medium`/`Large`), `NavigationClicked`, `AddAction(icon, onClick)`, `AttachScrollViewer(scroll)`, `IsScrolled`, `CollapseProgress`, `SafeAreaPadding` (filled by `MavNavigationPage`). |
| `MavBottomAppBar` | bottom bar with FAB. `Fab`, `FabIcon`, `FabCommand`, `FabClicked`, `AddAction`, `SafeAreaPadding` (filled by `MavNavigationPage`). |
| `MavContextualActionBar` | bar for selection mode. `SelectedCount`, `AutoHideWhenEmpty` (default true), `TitleFormat` or `TitleFormatter`, `ClearCommand`, `SelectionCleared`, `AddAction(icon, tooltip, onClicked)`, `AddButton(text, icon, onClicked)`. |
| `MavNavigationBar` | bottom navigation. `AddDestination(label, icon, selectedIcon, badge)`, `SelectedIndex`, `SelectedItem`, `ItemsSource`, `ShowLabels`, `SelectedIndexChanged`. |
| `MavNavigationRail` | side rail. Same destination API + `Header`. |
| `MavNavigationDrawer` | permanent drawer. `AddItem`, `AddSectionHeader`, `AddDivider`, `Title`/`Headline`, `DrawerWidth`, `SelectedIndex`. |
| `MavDrawerItem` | item for `MavNavigationDrawer` and the modal drawer (derives `MavNavigationItemBase`: `Label`, `Icon`, `SelectedIcon`, `BadgeText`). `SectionHeader`, `ShowDividerBefore`, `OnClick`, `Clicked`, `Command`, `CommandParameter`. |
| `MavNavigationDestination` | item for `MavNavigationBar` and `MavNavigationRail` (`Label`, `Icon`, `SelectedIcon`, `BadgeText`, `ShowBadgeDot`). |
| `MavSuiteDestination` | item for `MavNavigationSuiteScaffold`. Same base members plus `Content` (the page body shown for the destination). |
| `MavModalNavigationDrawer` | modal drawer handle. `Drawer`, `Items`, `IsOpen`, `Show(anchor)`/`Close()`. |
| `MavNavigationSuiteScaffold` | adaptive bar → rail → drawer. `Destinations`/`AddDestination(label, icon, selectedIcon, badge, content)`, `Mode` (`Auto`), `EffectiveMode`, `CompactBreakpoint` (600), `ExpandedBreakpoint` (840), `Body`, `TopAppBar`, `FloatingActionButton`, `RailHeader`, `DrawerHeadline`. |
| `MavTabs` | primary/secondary tabs. `Variant`, `IsScrollable`, `AddTab(text, icon, badge, content)`, `SelectedIndex`, `SelectedItem`, `ItemsSource`, `ItemTemplate`. |
| `MavTabItem` | tab item. `Text`, `Icon`, `BadgeText`, `Content`. |
| `MavBreadcrumbBar` | breadcrumb trail. `Add`, `SetPath(params string[])`, `MaxVisibleItems`, `SelectedItem`, `ItemClickCommand`. |
| `MavPagination` | page buttons. `TotalPages`, `CurrentPage`, `MaxVisiblePages`, `ShowFirstLastButtons`, `PageChanged`, `Command`. |
| `MavStepper` | wizard steps. `AddStep(title, subtitle, content, stateOverride)`, `CurrentStep`, `Orientation`, `ShowControls`, `AllowStepTap`, `Next()`, `Previous()`, `StepChanged`, `Completed`. |
| `MavStep` | step item. `Title`, `Subtitle`, `IsOptional`, `Content`, `StateOverride` (`MavStepState`). |
| `MavToolbar` | floating/docked action bar. `Variant`, `ColorStyle`, `Orientation`, `AddAction`, `AddControl`, `AddDivider`, `CompanionFab`. |

Custom page stack:

```csharp
var home = new MavContentPage("Inbox")
{
    Subtitle = "12 messages",
    Content = list,
};
home.AddAction(MavIcons.Search, () => Search());

var details = new MavContentPage("Message")
{
    Content = detailsBody,
};
details.AddAction(MavIcons.Reply, () => Reply());

var navigation = new MavNavigationPage(home);
window.Content = navigation;

navigation.Push(details);
navigation.Pop();
```

`MavNavigationPage` supplies the back arrow automatically when `CanGoBack` is true.
It handles `Escape` and `Alt+Left`; `TryGoBack()` dismisses the top overlay first,
then asks the current page via `OnBackPressed()`, then pops. Pages can override
`OnBackPressed()` to consume back (for example, close an internal search state)
before the host pops them. Overlays owned by a page are closed automatically when
the page leaves the stack. Call `RefreshChrome()` after changing a visible page's
title, actions, bottom bar or FAB.

Safe area and soft keyboard are handled by the host:

```csharp
var navigation = new MavNavigationPage(home);

// Real insets come from IInsetsManager on Android/iOS. On desktop you can preview:
navigation.SafeAreaOverride = new Thickness(0, 24, 0, 48);
navigation.KeyboardHost.SimulatedKeyboardHeight = 300;   // preview keyboard inset
```

`ApplySafeArea` (default `true`) distributes the insets to the app bar, bottom bar,
FAB and page content. `KeyboardHost` is the internal `MavKeyboardAvoidingHost`; it
lifts the page above the input pane and scrolls the focused field into view.
Set `ApplySafeArea = false` or `SafeAreaOverride = null` to opt out.

Adaptive shell (desktop/tablet optional):

```csharp
var shell = new MavNavigationSuiteScaffold
{
    Body = pageHost,
    TopAppBar = new MavTopAppBar("Mail") { NavigationIcon = MavIcons.Menu },
    FloatingActionButton = new MavFab(MavIcons.Edit),
    DrawerHeadline = "Mail",
};
shell.AddDestination("Inbox", MavIcons.Inbox, MavIcons.Inbox, "12", inboxPage);
shell.AddDestination("Sent", MavIcons.Send, content: sentPage);
shell.SelectedIndexChanged += (_, i) => { };
```

### 5.5 Containers and layout

| Control | Purpose and key API |
|---|---|
| `MavCard` | MD3 card. `new MavCard(MavCardVariant variant, Control? body, bool interactive)`; `Variant` (`Elevated`/`Filled`/`Outlined`), `IsInteractive`/`IsClickable`, `CardRadius`, `CardPadding`, `Body`, `Clicked`. |
| `MavSurface` | styled surface. `FillColor`, `SurfaceTintColor`, `Radius`, `Elevation`, `BorderStrokeColor`, `BorderStrokeThickness`. |
| `MavExpansionPanel` | expandable panel. `Title`/`Header`, `Subtitle`, `LeadingIcon`, `IsExpanded`, `Body`, `ExpandedChanged`. |
| `MavExpansionPanelList` | accordion list. `SingleExpand`/`AllowMultipleExpanded`, `AddPanel`. |
| `MavSplitView` | resizable master/detail. `Pane`, `DetailContent`, `PaneWidth`, `MinPaneWidth`, `MaxPaneWidth`, `InitialRatio`, `IsPaneOpen`, `IsResizable`, `CompactBreakpoint`, `IsCompact`, `CompactModeChanged`. |
| `MavCarousel` | swipeable card carousel. `AddItem(title, subtitle, icon, accent)`, `Items`, `ItemsSource`, `ItemTemplate`, `Layout` (`Hero`/`CenterAligned`/`MultiBrowse`/`Uncontained`), `SelectedIndex`, `ShowIndicators`, `Next`, `Previous`, `SelectedIndexChanged`, `ItemClicked`. |
| `MavCarouselItem` | card item. `Title`, `Subtitle`, `Icon`, `AccentColor`. |
| `MavSettingsCard` | settings row. `Header`/`Title`, `Description`, `Icon`, `ActionContent`, `IsActionable`, `Command`, `Clicked`. |
| `MavSettingsExpander` | settings group. `AddItem(title, subtitle, trailingControl, leadingIcon)`, `AddCustomContent`, `IsExpanded`, `HeaderAction`. |
| `MavAvatar` | initials/icon/image avatar. `Initials`, `Icon`, `ImageSource`, `Size`, `Status` (`None`/`Online`/`Busy`/`Away`/`Offline`), `CustomFillColor`, `CustomTextColor`. |
| `MavSafeArea` | safe-area inset container. `Body`, `Edges` (`None`/`Left`/`Top`/`Right`/`Bottom`/`Horizontal`/`Vertical`/`All`), `BasePadding`, `SimulatedInset`, `AppliedInset`, `RefreshInsets()`. |
| `MavKeyboardAvoidingHost` | keyboard-aware container. `Body`, `ExtraBottomSpacing`, `AnimateInset`, `AutoScrollFocusedElement`, `SimulatedKeyboardHeight`, `KeyboardHeight`, `KeyboardHeightChanged`. |
| `MavScrollViewer` | theme-independent scroll host used by Mav controls and demo pages. |
| `MavWindow` | theme-free desktop window template with a `VisualLayerManager` for overlays. |
| `MavBadge` / `MavBadgedBox` | count/dot badge. `Text`, `Count`, `MaxCount`, `IsDot`; wrapper adds `Target`, `BadgeText`, `BadgeCount`, `ShowBadge`. |
| `MavDivider` | horizontal/vertical divider. `IsVertical`, `InsetStart`, `InsetEnd`. |
| `MavLabel` | typography-aware text. `Role`, `ColorRole`, `Secondary`. |

Layout examples:

```csharp
var hero   = new MavCarousel { Layout = MavCarouselLayout.Hero, Height = 220 };
var browse = new MavCarousel { Layout = MavCarouselLayout.MultiBrowse, Height = 200 };
hero.AddItem("Material 3", "Tonal palettes", MavIcons.Palette);

// Pane starts at 40% of the width. Applied once on first layout; an explicit
// PaneWidth (or a later resize) keeps its pixel width instead of following the ratio.
var split = new MavSplitView { InitialRatio = 0.4 };
```

`Hero` gives the focused card the most space; `CenterAligned` centres it with symmetric
peeks; `MultiBrowse` shows about three equal cards; `Uncontained` centres a wide card
that bleeds past the viewport edges. `InitialRatio` is clamped to 15–85% and to
`MinPaneWidth`/`MaxPaneWidth`; setting `PaneWidth` explicitly disables it.

### 5.6 Lists, tables, trees

| Control | Purpose and key API |
|---|---|
| `MavList` | vertical list (`StackPanel`). `AddItem(MavListItem)`, `AddSubheader`, `AddDivider`, `ItemsSource`, `ItemTemplate`, `ItemFactory`, `ShowDividers`, `DividerInsetStart`. |
| `MavListItem` | list row. `Headline`, `SupportingText`, `Overline`, `LeadingIcon`, `TrailingIcon`, `TrailingText`, `LeadingView`, `TrailingView`, `IsSelected`, `IsInteractive`, `Command`, `Clicked`, `TrailingIconClicked`, `LeadingSwipeAction`, `TrailingSwipeAction`, `Lines` (`OneLine`/`TwoLine`/`ThreeLine`). |
| `MavSwipeAction` | swipe action behind a row. `Label`, `Icon`, `IsDestructive`, `OnTriggered`/`Command`, `BackgroundColor`, `ForegroundColor`, `Triggered`. |
| `MavListSubheader` | section label. `Text`. |
| `MavDataTable` | data grid with sorting/filter/pagination. `AddColumn(header, valueSelector, isNumeric, isSortable, cellTemplate, width)`, `Columns`, `ItemsSource`, `IsSelectable`, `SelectedItem`, `SelectedItems`, `SelectedItemsSource`, `FilterText`, `ShowPagination`, `PageSize`, `CurrentPage`, `AlternatingRowBackground`, `MaxBodyHeight`, `Layout` (`Auto`/`Table`/`Cards`), `CompactBreakpoint` (600), `IsCompact`, `EffectiveLayout`, `RowClicked`, `SelectionChanged`, `ClearSelection()`. Auto switches to cards on hosts narrower than `CompactBreakpoint`. |
| `MavDataColumn` | column definition. `Header`, `PropertyName`/`BindingPath`, `ValueSelector`, `IsNumeric`, `IsSortable`, `CellTemplate`, `CellFactory`, `Width`. |
| `MavTreeView` | hierarchical tree. `Roots`/`Add`, `SelectedNode`, `ShowCheckBoxes`, `CascadeCheckBoxes`, `ShowIndentGuides`, `FilterText`, `ExpandAll`, `CollapseAll`, `GetCheckedNodes`, `SelectedNodeChanged`, `NodeCheckedChanged`. |
| `MavTreeNode` | tree node. `Text`, `Icon`, `BadgeText`, `IsExpanded`, `IsChecked`, `Children`, fluent `Add`. |
| `MavTimeline` | vertical timeline. `Add(title, description, timestamp, icon, status, customContent)`, `Items`, `ItemsSource`, `ItemTemplate`. |
| `MavTimelineItem` | timeline entry. `Title`, `Description`, `Timestamp`, `Icon`, `Status` (`Completed`/`Active`/`Pending`/`Warning`/`Error`), `CustomContent`. |

List example:

```csharp
var list = new MavList { ShowDividers = true };
list.AddSubheader("Today");
list.AddItem(new MavListItem
{
    Headline = "Design review",
    SupportingText = "10:30 – 11:15",
    LeadingIcon = MavIcons.Event,
    TrailingIcon = MavIcons.ChevronRight,
    Command = openCommand,
});
list.AddDivider();
```

Table example:

```csharp
var table = new MavDataTable
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

`MavDataTable` requires at least one column before it renders rows. Sorting/filtering are
client-side over `ItemsSource`; there is **no virtualization** — page large collections.

### 5.7 Feedback, status, and progress

| Control | Purpose and key API |
|---|---|
| `MavDialog` | modal dialog. See §4.2. |
| `MavBottomSheet` / `MavSideSheet` | sheets. See §4.3. |
| `MavSnackbar` | global snackbar FIFO. See §4.5. |
| `MavBanner` | inline banner. `Message`/`Text`, `Icon`, `PrimaryActionText`, `SecondaryActionText`, `PrimaryActionClicked`, `SecondaryActionClicked`, `Show()`, `Dismiss()`. |
| `MavInfoBar` | severity banner. `Title`, `Message`, `Severity` (`Info`/`Success`/`Warning`/`Error`), `IsOpen`, `IsClosable`, `ActionText`, `ActionClicked`, `Closed`. |
| `MavEmptyState` | empty placeholder. `Title`, `Description`, `Icon`, `ActionText`, `SecondaryActionText`, `ActionClicked`, `SecondaryActionClicked`. |
| `MavLinearProgress` | linear progress. `Value` (0–1), `IsIndeterminate`, `ShowStopIndicator`, `IsWavy`, `WaveAmplitude`, `WaveLength`. |
| `MavCircularProgress` | circular spinner/progress. `Value`, `IsIndeterminate`, `Size`/`Diameter`, `StrokeWidth`, `CustomActiveColor`, `IsWavy`. |
| `MavSegmentedProgressBar` | segmented bar. `Value`, `SegmentCount`, `SegmentGap`, `TrackHeight`. |
| `MavWavyProgressIndicator` | wavy linear progress (subclass of `MavLinearProgress`). |
| `MavLoadingIndicator` | contained loading spinner. `Size`, `IsContained`, `IndicatorColor`. |
| `MavSkeleton` | skeleton placeholder. `Shape` (`Rounded`/`Circle`/`Text`), `Circle(size)`, `Text(width, height)`, `ListRow(avatarSize, lines)`. |
| `MavRefreshIndicator` | pull-to-refresh host. `Body`/`Content`, `IsRefreshing`, `TriggerDistance`, `MaxPullDistance`, `StatusText`, `ArmedText`, `RefreshingText`, `RefreshHandler` (returns `Task`) or `Command`, `RefreshRequested`, `TriggerRefreshAsync()`. |
| `MavRadialGauge` | radial gauge. `Value`, `Minimum`, `Maximum`, `StepSize`, `Unit`, `Label`, `Size`, `TickCount`, `IsInteractive`, `ValueChanged`. |
| `MavSparkline` | mini chart. `Kind` (`Line`/`Bar`/`Donut`), `Values` or `ValuesText` (CSV), `ItemsSource`, `AccentColor`, `CenterLabel`. |
| `MavAvatarGroup` | overlapping avatars. `MaxDisplayed`, `AvatarSize`, `OverlapRatio`, `AddAvatar`, `ItemsSource`. |
| `MavFileDropZone` | file drop/list area. `Title`, `Subtitle`, `AllowMultiple`, `Files`, `FilesSource`, `AddFile(name, sizeBytes, path)`, `RemoveFile`, `BrowseFilesAsync()`, `FileAdded`, `FileRemoved`, `FilesChanged`. |
| `MavChatBubble` | chat message bubble. `Text`, `SenderName`/`Sender`, `Timestamp`, `AvatarInitials`, `IsOutgoing`, `IsRead`. |
| `MavBreadcrumbBar` / `MavPagination` | navigation aids. See §5.4. |

Progress and refresh:

```csharp
var bar = new MavLinearProgress { IsIndeterminate = true };
var spinner = new MavCircularProgress(24, 3) { IsIndeterminate = true };

var refresh = new MavRefreshIndicator
{
    Body = list,
    RefreshHandler = async () =>
    {
        await ReloadAsync();
    },
};
```

### 5.8 Forms helper

`MavFormValidator` wires rules directly to fields and writes `ErrorText`/`HasError`.

```csharp
var name = new MavTextField("Name");
var email = new MavTextField("Email");
var city = new MavTextField("City");

var validator = new MavFormValidator { ValidateOnChange = true };
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

Built-in overloads exist for `MavTextField`, `MavTextArea`, `MavSelect`, `MavComboBox`, `MavPinInput`,
`MavDatePickerField`, `MavDateRangePickerField`, and `MavTimePickerField`.
For any other control use the generic form:

```csharp
var age = new MavNumberBox { Label = "Age", Minimum = 0, Maximum = 120 };

validator.Field(
    valueGetter: () => age.Value.ToString(CultureInfo.InvariantCulture),
    errorSetter: err => age.Suffix = err,
    subscribeChanged: subscribe => age.ValueChanged += (_, _) => subscribe());
```

Rules: `Required`, `MinLength`, `MaxLength`, `Email`, `Numeric`, `Pattern(regex)`,
`Matches(otherGetter)`, `Must(predicate, message)`.

Attached-property validation for AXAML:

```csharp
MavValidation.SetIsRequired(textField, true);
MavValidation.SetIsEmail(textField, true);
MavValidation.SetMinLength(textField, 3);
MavValidation.SetPattern(textField, @"^[A-Z].*");
MavValidation.SetRequiredMessage(textField, "Required");
bool ok = MavValidation.ValidateTree(root);
```

`MavValidation` also supports `IsRequired`/`GetIsRequired` aliases, `IsEmail`, `IsNumeric`,
`MinLength`, `MaxLength`, `Pattern`, `Validator`, and message overrides per control.

---

## 6. Recipes

### 6.1 Complete page with app bar, list, and FAB

```csharp
public static Control BuildPage()
{
    var list = new MavList { ShowDividers = true };
    list.AddSubheader("Projects");
    list.AddItem(new MavListItem
    {
        Headline = "Mav",
        SupportingText = "Material 3 in pure C#",
        LeadingIcon = MavIcons.Folder,
        TrailingIcon = MavIcons.ChevronRight,
    });

    var appBar = new MavTopAppBar("Projects")
    {
        NavigationIcon = MavIcons.Menu,
        Variant = MavTopAppBarVariant.Small,
    };
    appBar.AddAction(MavIcons.Search);
    appBar.AddAction(MavIcons.MoreVert);

    var shell = new DockPanel();
    DockPanel.SetDock(appBar, Dock.Top);
    shell.Children.Add(appBar);
    shell.Children.Add(list);

    var fab = new MavFab(MavIcons.Add);
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
var darkMode = new MavSwitch
{
    IsToggled = MavTheme.Current.IsDark,
    ShowIcons = true,
    OnIcon = MavIcons.DarkMode,
    OffIcon = MavIcons.LightMode,
};
darkMode.Toggled += (_, on) => MavTheme.Mode = on ? MavThemeMode.Dark : MavThemeMode.Light;

var card = new MavSettingsCard
{
    Header = "Appearance",
    Title = "Dark mode",
    Description = "Use the dark color scheme",
    Icon = MavIcons.Palette,
    ActionContent = darkMode,
};
```

### 6.3 Confirm + snackbar with undo

```csharp
private async void OnDeleteClicked(object? sender, EventArgs e)
{
    bool confirmed = await MavDialog.ShowConfirmAsync(
        "Delete item?",
        "You can undo this for a few seconds.",
        "Delete",
        "Cancel",
        MavIcons.Delete);

    if (!confirmed) return;

    DeleteItem();
    MavSnackbar.Show("Item deleted", "Undo", UndoDelete,
        duration: MavSnackbarDuration.Long);
}
```

### 6.4 Form submit

```csharp
var name = new MavTextField("Full name", MavTextFieldVariant.Outlined);
var email = new MavTextField("Email");
var date = new MavDatePickerField { Label = "Start date", IsClearable = true };
var role = new MavSelect("Role", new[] { "Admin", "Editor", "Viewer" });

var validator = new MavFormValidator();
validator.Field(name).Required().MinLength(2);
validator.Field(email).Required().Email();
validator.Field(date).Required("Pick a date");
validator.Field(role).Required("Pick a role");

var submit = new MavButton("Create", MavButtonVariant.Filled);
submit.Clicked += async (_, _) =>
{
    if (!validator.Validate())
    {
        MavSnackbar.Show("Fix the highlighted fields");
        return;
    }

    await api.CreateAsync(new CreateRequest
    {
        Name = name.Text,
        Email = email.Text,
        Start = date.SelectedDate!.Value,
        Role = role.SelectedItem!,
    });
    MavSnackbar.Show("Created");
};
```

### 6.5 Popup anchored to a button

```csharp
var trigger = new MavButton("Filters", MavButtonVariant.Outlined, MavIcons.FilterList);
trigger.Clicked += (_, _) =>
{
    var panel = new StackPanel { Spacing = 8, Width = 240 };
    panel.Children.Add(new MavCheckBox("Archived"));
    panel.Children.Add(new MavCheckBox("Shared"));

    MavOverlay.ShowAnchoredPopup(trigger, new MavSurface
    {
        FillColor = MavTheme.ColorOf(MavColorRole.SurfaceContainerHigh),
        Radius = MavTheme.Current.Shape.Medium,
        Elevation = 3,
        Content = new Border { Padding = new Thickness(12), Child = panel },
    });
};
```

### 6.6 Responsive split layout

```csharp
var split = new MavSplitView
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

var button = new MavButton("Save") { Command = vm.SaveCommand };
var field = new MavTextField("Name") { Text = vm.Name };
field.TextChanged += (_, text) => vm.Name = text;
```

All controls accept `Command`/`CommandParameter`; use the CLR events only for view-local behavior.

---

## 7. Gotchas and limitations

1. **`UseMav()` is not optional** if you want bundled fonts and Back/Escape overlay dismissal.
   Without it, icon glyphs render as missing characters.
2. **Popup controls are invisible handles.** `MavDialog`, `MavMenu`, `MavPopover`, `MavRichTooltip`,
   `MavBottomSheet`, `MavSideSheet`, `MavDatePicker`, `MavTimePicker`, `MavSearchView`, and
   `MavModalNavigationDrawer` have zero size and display through `MavOverlay`. Adding them to a
   `StackPanel` produces nothing.
3. **Overlay calls need a live TopLevel.** Call `Show()` from a click handler or after the window is
   opened. During `Application.Initialize()` there is no anchor; pass a live control or use
   `MavOverlay.TrackTopLevel`.
4. **`ItemsSource` owns the children.** Setting it clears manual items and rebuilds on every
   collection change. Do not mix `ItemsSource` and `Children`/`Items` for the same control.
5. **`TextChanged` fires for code changes.** `MavTextField`, `MavTextArea`, `MavSearchBar`, `MavComboBox`,
   `MavPinInput`, and validator subscriptions respond to programmatic assignment as well as typing.
6. **`MavSwitch` uses `IsToggled`**, not `IsOn`. (Common mistake when porting from MAUI.)
7. **`MavSnackbar` is global and static.** `Show` clears the pending queue; `Enqueue` appends.
   Only one snackbar is visible per application, regardless of window. When shown from a page inside
   `MavNavigationPage`, it automatically sits above the bottom app bar, FAB and safe-area inset.
8. **`MavTooltip` is a single global handle.** Showing a new tooltip replaces the previous one.
9. **`MavList.ShowDividers` manages only `MavListItem` children.** Items added after the property is set
    get dividers automatically (all but the last); setting it to `false` clears the managed dividers.
    Explicit per-item `ShowDivider = true` is preserved while the list does not manage dividers.
    Subheaders and custom children never receive dividers; `DividerInsetStart` is applied through the list.
10. **`MavList` virtualizes `ItemsSource` inside a `ScrollViewer`.** Only the visible window
    (plus ~480 dp overscan) is realized; `Children` also contains two internal spacer
    borders, so do not treat `Children` as the full item list in that mode. Manual
    `AddItem`/`AddSubheader` lists are never virtualized. Small sources (≤60) and lists
    without a `ScrollViewer` are realized eagerly.
11. **`MavDataTable` needs columns first**, and sorting/filtering/pagination are client-side.
    Rows are appended in chunks of 100 while the body scrolls (`MaxBodyHeight`); pagination
    is the recommended path for very large sources. `Layout = Auto` (default) renders cards
    when the host is narrower than `CompactBreakpoint` (600 dp); set `Table` or `Cards` to
    override. Cards show the first column as a headline, the rest as label/value pairs, and
    sortable columns as chips.
12. **`MavPinInput`/`MavTextField` validation is string-based.** `MavFormValidator.Field(MavNumberBox)`
    has no dedicated overload; use the generic getter/setter form shown in §5.8.
13. **Theme changes are global and immediate.** `MavTheme.Configure` rebuilds brushes and raises
    `Changed`; call it on the UI thread. Use `MavTheme.Observe` for custom-drawn controls instead of
    capturing a palette in a field.
14. **`MavIcon` defaults to `OnSurfaceVariant`** when no `ColorRole`/`Color` is given. Pass
    `ColorRole` or set `UseThemeDefaultColor = false` when embedding icons in colored containers.
15. **The library ships no Avalonia control theme.** Plain `Button`, `TextBox`, `ScrollViewer`, etc.
    have no template unless the host application installs a theme. Use Mav controls plus
    `MavWindow`/`MavScrollViewer`, or install `Avalonia.Themes.Fluent` yourself.
16. **`MavThemeStyle` is a `Style`, not a theme dictionary.** Add it to `Window.Styles`/`Application.Styles`;
    it forwards properties to the static theme but does not style arbitrary controls.
17. **Custom-drawn controls (`MavSwitch`, `MavSlider`, `MavCarousel`, `MavTabs`, …) have no `Content`.**
    Put content in their items/body properties instead.
18. **DP/threading:** update `ItemsSource` and call overlay APIs on the UI thread. Background work
    should marshal via `Dispatcher.UIThread`.
19. **Headless tests cover overlays too.** `Tests/MavUI.Tests.csproj` uses
    `Avalonia.Headless.XUnit` (xunit v3). `MavNavigationPage` registers a fallback overlay host, so
    dialogs, pickers and sheets can be tested without an Avalonia `OverlayLayer`. The suite has
    about 330 tests across navigation, lists, keyboard, overlays, controls and a control-creation
    smoke test (see §10). The source under
    `MavUI/` remains the source of truth; run the build after changes.
20. **`adb shell input` is not a reliable touch test on every ROM.** Some devices inject
    `MotionEvent`s with `ToolType=Unknown`, which Avalonia ignores; use a real finger or
    `monkey`/instrumentation that emits finger events before blaming the UI. Keyboard
    (`Tab`/`Enter`) and `KEYCODE_BACK` still exercise the app and overlays.
21. **`SimulatedKeyboardHeight` is a global overlay preview while attached.** Every
    `MavKeyboardAvoidingHost` with a non-zero `SimulatedKeyboardHeight` lifts all overlays
    (dialogs, sheets, popovers) above that inset. The value is contributed only while the
    host is in the visual tree, so leaving a preview page restores the full-height overlay;
    the effective value is the maximum across currently attached hosts.
22. **Android Back arrives once, through `TopLevel.BackRequested`.** `Keycode.Back` also maps to
    `Key.Escape`. Custom key handlers must not treat Escape as Back on Android, or one press closes two
    layers and can finish the Activity.
23. **Read safe-area insets after scaling settles.** On Android `TopLevel.RenderScaling` starts at `1` and
    then becomes about `2.625`. Custom layouts that read `SafeAreaPadding` must read it again on
    `ScalingChanged` and on `DispatcherPriority.Loaded`, as `MavNavigationPage` does.
24. **Edge-to-edge only on Android 15 and newer.** On Android 11 transparent system bars show the wallpaper
    behind the status bar. Pass `edgeToEdge: true` to `MavSystemBars.Attach(...)` only when
    `Build.VERSION.SdkInt >= 35`; use `false` on older releases.
25. **Declare configuration changes on the Android Activity.** Without them a rotation recreates the Activity
    and an open dialog is lost. The gallery declares `Orientation | ScreenSize | UiMode | Density | Locale |
    LayoutDirection`, so overlays reflow in place.

---

## 8. Repository map, build and CI

```
<repo-root>/
├── MavUI.sln                        solution: library, Essentials, tests, gallery hosts
├── CHANGELOG.md                     versions, notable changes, SemVer policy
├── MavUI/                           the library (net10.0, Avalonia 12.1.4, embedded fonts)
│   ├── MavUI.csproj                 MavFonts, trimming and AOT properties
│   ├── MavIcons.cs                  ~10,800 icon glyph constants
│   ├── MavFonts.cs                  font families, ResolveIconFont / ResolveIconTypeface
│   ├── MavAppBuilderExtensions.cs   UseMav(...)
│   ├── MavLocalization.cs           providers, culture switching, Observe
│   ├── LocExtension.cs              {mav:Loc ...} AXAML markup extension
│   ├── Controls/                    ~158 files, one public type per file
│   ├── Primitives/                  MavContentView/MavSurface, MavInkSurface (ripple, focus), MavIcon
│   ├── Platform/                    MavOverlay, MavSystemBars, MavKeyboard, MavNativeEntry/Editor,
│   │                                MavTextContextMenu, MavGestureZones, MavAutomation (accessibility),
│   │                                MavTextMetrics, MavLruCache
│   ├── Theming/                     MavTheme, MavThemeOptions, MavThemeService, MavColorScheme,
│   │                                MavTypography, MavMotion, MavTokens (shape, elevation, state, density),
│   │                                MavAccessibility (WCAG), ColorUtilities/
│   └── Resources/Fonts/             Roboto and icon fonts
├── MavUI.Essentials/                separate package: preferences and secure storage (§9.5)
├── Tests/                           MavUI.Tests: xUnit with Avalonia.Headless (§10)
├── samples/ControlsGallery/         one gallery, four sibling projects (§9)
│   ├── ControlsGallery/             shared library: App, adaptive shell, pages, localization
│   ├── ControlsGallery.Desktop/     desktop host
│   ├── ControlsGallery.Android/     Android host, applicationId com.mav.controlsgallery
│   └── ControlsGallery.Browser/     WebAssembly host (Sdk.WebAssembly, own csproj)
├── docs/                            AGENTS.md (this file), NOTES.md, gallery/README.md
├── tools/
│   ├── linux/mavui.py               X11 run, screenshot, click, key, wheel helper
│   ├── win/mavui.ps1                Windows helper (legacy)
│   └── summarize_speedscope.py      summarizes the profiler output of the [profile] CI step
└── .github/workflows/build.yml      CI
```

### Build and test

```bash
dotnet build MavUI.sln -v q --nologo
dotnet build MavUI/MavUI.csproj -v q --nologo
dotnet test Tests/MavUI.Tests.csproj --nologo
dotnet run --project samples/ControlsGallery/ControlsGallery.Desktop/ControlsGallery.Desktop.csproj
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` and all tests passing. If `dotnet` is not
installed in your environment, the CI result is the source of truth.

### CI

Workflow `.github/workflows/build.yml` runs on a **self-hosted Linux runner** for every push that
changes code (not for commits that only touch `docs/**` or `**/*.md`). It builds the library, the
tests, the desktop gallery and `MavUI.Essentials`, runs the tests and commits the build log to
`ci/last-build.log` on the same branch (`[skip ci]`).

Commit-message tags:

* `[heads]` — also builds the Android and Browser targets (`MavUI.Essentials` with
  `IncludeAndroid`/`IncludeBrowser`, the gallery Android and Browser hosts). These need the Android
  and wasm workloads, so they are opt-in.
* `[profile]` — runs the theme-switch profiling step with `dotnet-trace`.
* Manual `workflow_dispatch` runs are possible from the GitHub UI; they are not available from the
  agent sandbox.

### GUI helpers

* Linux/X11: `tools/linux/mavui.py` — `start|stop|shot|click|move|key|type|wheel|info`, with
  `--window`, `--safe-area`, `--keyboard`, `--frame`. Needs `python3-xlib`, `Pillow` and
  `gnome-screenshot` or `scrot`. Example:
  `python3 tools/linux/mavui.py start --window 400x800 --safe-area 24,48 --keyboard 300`.
  `shot --out <path>` writes a screenshot to the given path. Clicks use window-relative pixels of
  the **client area**.
* Windows: `tools/win/mavui.ps1` — `-Action start|stop|shot|click|key|wheel|info`, `-EnvWindow`,
  `-EnvSafeArea`, `-EnvKeyboard`.

---

## 9. Platforms

### 9.1 Desktop (Windows, Linux, macOS)

* Use `MavWindow` for desktop windows. It supplies the presenter and overlay layer the library needs,
  so no Avalonia theme is required.
* `Escape` and `Alt+Left` go back, and `Escape` also closes the topmost overlay (`KeyDown` on the
  `TopLevel`).
* Desktop has no system insets. To preview safe-area or keyboard layouts, set
  `SafeAreaOverride` on the navigation page, or use `MAV_KEYBOARD=<px>` /
  `--keyboard <px>` (sets `SimulatedKeyboardHeight`).

### 9.2 Android

* Minimum API: the gallery host declares 23 (`SupportedOSPlatformVersion`); `MavUI.Essentials`
  declares 21. Build with the Android workload and SDK platform 36.
* No Avalonia theme is needed. `EmbeddableControlRoot` has no `OverlayLayer`, so
  `MavNavigationPage` registers its own overlay host and dialogs, sheets, pickers and menus work
  without extra code.
* Back order: open overlay → page `OnBackPressed()` → `Pop()`; the Activity finishes only on the
  second press at the root. See gotcha 22.
* System bars: `MavSystemBars.Attach(navigation, edgeToEdge)` colors the bars from
  `MavTheme.Current.Color.Surface`, sets light or dark icons from `MavTheme.Current.IsDark`, and
  follows `MavTheme.Changed`. Edge-to-edge only on Android 15+ (gotcha 24).
* Soft keyboard: the real `IInputPane` lifts the page and overlays. Use `MavTextField.ContentType`,
  `ReturnKeyType` and `AutoCapitalize` for the right keyboard.
* Trimmed and AOT Release publish works (measured on a physical device). See §1.7 for
  `DisplayMemberPath` rooting.
* Verified: AVD Android 17 (emulator on Linux/KVM) and a physical device on Android 11, then Android 15
  after an OS update.
* Testing on devices and emulators:
  * `adb shell input tap/swipe` may be ignored on some ROMs (touch `ToolType=Unknown`). Verify with
    keyboard (`Tab`/`Enter`) or `adb shell input keyevent KEYCODE_BACK`, and with a real finger.
  * Rotate the emulator: `adb shell settings put system user_rotation 1`.
  * Launch: `adb shell monkey -p com.mav.controlsgallery -c android.intent.category.LAUNCHER 1`.
  * Screenshot: `adb exec-out screencap -p > shot.png`. Stop the emulator with `adb emu kill`.

### 9.3 Browser (WebAssembly)

* `ControlsGallery.Browser` uses `Sdk.WebAssembly`, so it cannot share a csproj with the other
  hosts. Requires `dotnet workload install wasm-tools`. The run command prints a
  `http://127.0.0.1:<port>/` URL.
* `MavUI.Essentials` preferences use `localStorage` in the browser; this has not been checked in a
  browser yet.
* Secure storage is not wired in the browser and fails closed. It never falls back to plain
  `localStorage`.
* Caution: JS calls such as `JSImport("globalThis.window.localStorage...")` can work in Debug and
  fail in published builds. Verify with a Release publish before relying on them.

### 9.4 Desktop automation

Use `tools/linux/mavui.py` on X11 (see §8). Windows automation (`tools/win/mavui.ps1`) is legacy.

### 9.5 MavUI.Essentials

* Separate project, not part of the `MavUI` package. Targets `net10.0` and `net10.0-windows`;
  Android and Browser targets are added with `-p:IncludeAndroid=true` and `-p:IncludeBrowser=true`.
* `MavPreferences.Default` (`IMavPreferences`) — `JsonFilePreferences` on desktop. Use
  `MavPreferences.SetDefault(...)` to inject another implementation. `MavPreferenceValues` lists the
  supported value types.
* `MavSecureStorage.Default` (`IMavSecureStorage`). Platform code lives in
  `MavUI.Essentials/Platform/{Linux,Windows,Android,Browser}`.
* Status (see `docs/NOTES.md`): Android Keystore and SharedPreferences code builds but is not yet
  verified on a device. Apple (Keychain, `NSUserDefaults`) is deferred until a macOS runner or Mac is
  available.

---

## 10. Testing the library

* Project: `Tests/MavUI.Tests.csproj`, xUnit v3 with `Avalonia.Headless.XUnit`. Use `[AvaloniaFact]`
  for tests that need a UI thread. `InternalsVisibleTo("MavUI.Tests")` gives tests access to internal
  members.
* Host pattern used across the suite:

  ```csharp
  var navigation = new MavNavigationPage(new MavContentPage("Root"));
  var window = new Window { Width = 800, Height = 600, Content = navigation };
  window.Show();
  Dispatcher.UIThread.RunJobs();
  ```

* Overlays are global. `MavOverlay` keeps a static dismiss stack, so every overlay test must clear
  it, even when the test fails:

  ```csharp
  finally
  {
      while (MavOverlay.TryDismissTop()) { }
      window.Close();
  }
  ```

* Pass an attached control as the anchor. A `null` anchor without a tracked `TopLevel` parks the
  overlay in the pending queue (§4.1), and the test then sees no overlay.
* Closing is animated and there is no switch to turn animations off. Assert only facts that are set
  synchronously: `HasActiveOverlay`, `IsOpen`, `TryDismissTop()` results and properties after `Show`.
  Do not assert `Closed` right after `Close()`.
* Assert layout and properties, not pixels. Visual checks are done by the maintainer on a device or
  emulator.
* `*Benchmarks.cs` tests print timings or check loose time limits (for example, `< 5 s`). They catch
  large regressions only and are not precise performance gates.
* Put new tests in a file named after the feature (`Tests/<Feature>Tests.cs`). The control smoke test
  (`ControlSmokeTests.cs`) builds every control in §5, so keep it passing when you add controls.
