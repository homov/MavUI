# Mav Control Gallery

A full-featured showcase for every control in `Mav`, grouped by purpose and
adaptive across desktop, web (WebAssembly), Android phones and tablets.

## Groups

| Group | Contents |
|---|---|
| Overview | Theme controls, 15 type roles, color roles, shape/elevation, motion |
| Actions | MavButton, MavIconButton, MavFab, MavExtendedFab, MavButtonGroup, MavSplitButton, MavFabMenu |
| Inputs | MavTextField, MavTextArea, MavPinInput, MavNumberBox, MavTagInput, MavSearchBar, MavFormValidator |
| Selection | MavSwitch, MavCheckBox, MavRadioButton/Group, MavSlider, MavRangeSlider, MavSegmentedButton, MavChip/Group, MavSelect, MavComboBox, MavRatingBar |
| Pickers | Date/time/range fields, calendar grid, clock dial, color picker and modal helpers |
| Navigation | App bars, navigation bar/rail/drawer, modal drawer, tabs, breadcrumbs, pagination, stepper, toolbar, contextual bar, adaptive scaffold, nested page stack |
| Layout | Cards, surfaces, labels, dividers, badges, expansion panels, settings, split view, carousel, scroll viewer, avatars, file drop, chat bubbles, safe area, keyboard host, MavWindow |
| Collections | MavList/ListItem/SwipeAction, MavDataTable (table and card modes), MavTreeView, MavTimeline |
| Feedback | Dialogs, sheets, snackbar, banner, info bars, empty state, progress variants, skeleton, refresh, radial gauge, sparkline |
| Overlays | Menu, popover, tooltips, search view, declarative flyouts |
| Theming | Seed/mode/contrast/density/motion/shape, color roles, type scale, elevation, WCAG ratios, live EN/RU localization |

## Adaptive shell

* **≥ 1000 dp** — permanent `MavNavigationDrawer` (desktop/web/tablet landscape).
* **700–999 dp** — `MavNavigationRail` with icons and labels (tablet portrait).
* **< 700 dp** — content only; the app-bar menu opens `MavModalNavigationDrawer` (phone).

The gallery itself uses `MavNavigationPage` + `MavContentPage`, so safe area, system Back,
Escape and overlay ownership work exactly as in a real app.

## Run

### Desktop

```bash
dotnet run --project samples/ControlsGallery/Desktop/ControlsGallery.Desktop.csproj
```

### WebAssembly

```bash
dotnet run --project samples/ControlsGallery/Browser/ControlsGallery.Browser.csproj
# open the printed http://127.0.0.1:<port>/ url
```

Prerequisite: `dotnet workload install wasm-tools` and the `Avalonia.Browser` package.

### Android

```bash
dotnet build samples/ControlsGallery/Android/ControlsGallery.Android.csproj
adb install -r samples/ControlsGallery/Android/bin/Debug/net10.0-android/com.mav.controlsgallery-Signed.apk
adb shell monkey -p com.md3.gallery -c android.intent.category.LAUNCHER 1
```

`com.md3.gallery` needs the Android workload and SDK platform 36. Edge-to-edge is
enabled on Android 15+; older releases use the theme surface color for system bars.

## Screenshots

`docs/gallery/`:

* `desktop-overview.png`, `desktop-navigation.png`, `desktop-theming.png`,
  `desktop-theming-localization.png`, `desktop-inputs.png`,
  `desktop-collections.png`, `desktop-feedback.png`, `desktop-overlays.png`
* `tablet-rail.png`, `phone-shell.png`, `phone-drawer.png`
* `android-phone.png`, `android-tablet-rail.png`, `android-tablet-drawer.png`
* `android15-feedback.png`, `android15-dialog.png`, `android15-bottom-sheet.png`,
  `android15-side-sheet.png`, `android15-snackbar-indefinite.png`
* `browser-overview.png`, `web-feedback-snackbar.png`
