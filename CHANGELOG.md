# Changelog

All notable changes to **Mav**.

## Versioning policy

- The library follows [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`.
- The version is **independent of internal implementation stages**. Stage numbers used in
  older entries describe implementation order; they are not part of the package version.
- `0.x` releases are previews: the public API may still change in a minor release.
  After `1.0.0`, breaking public-API changes require a major version bump.
- NuGet publication is intentionally **not** part of the plan. Consume the library as a
  project reference (see `docs/AGENTS.md`). There is therefore no package metadata,
  SourceLink, symbol package or `dotnet pack` step.

## [Unreleased]

### Added

- `MavItems`: a theme-free items host. The library ships no Avalonia theme, so a plain
  `ItemsControl` (a templated control) builds no `ItemsPresenter` and draws nothing; `MavItems`
  is a `Panel` that builds one control per item from `ItemTemplate`, follows
  `INotifyCollectionChanged` and lays items out in a line or in wrapping rows (`Wrap`, `Spacing`).
  The gallery now uses it for colour swatches, the type scale, shape/elevation samples, the
  contrast list and the palette buttons, which were all rendered empty before.

_Tooling (not part of the library API):_ `Tests/MavUI.VisualTests` renders every ControlsGallery
destination, three shell widths and a dark theme frame on headless Skia, checks that no visible
control leaves the window and publishes the PNGs with `build.log` to the `ci/reports` branch;
`tools/read_screenshots.py` fetches them back. Run artifacts are not downloadable from an agent
sandbox (their signed link points at Azure blob storage, which is outside the allowlist).

### Changed

- `MavNavigationDestination`, `MavSuiteDestination` and `MavDrawerItem` share a base class `MavNavigationItemBase` with `Label`, `Icon`, `SelectedIcon` and `BadgeText`. Public names are unchanged. `MavDrawerItem` now also refreshes when `SelectedIcon` changes.

- Incremental `CollectionChanged` handling in `MavList` and `MavDataTable`: `Add`, `Remove`,
  `Replace` and `Move` reuse unaffected controls, preserve selection and scroll offset;
  `Reset` keeps the full rebuild path.
- Arc, donut and outlined text-field geometry is cached per control instead of being rebuilt
  on every render pass.
- `MavOverlay` prunes dead host registrations and releases dismissables bound to a closed
  top-level; `PushDismissable` accepts an optional top-level.
- `IMavThemeService` now implements `IDisposable`. `MavTextMetrics` invalidates both text caches
  when the `FontManager` changes.
- `MavOverlay.Show`/`ShowAnchoredPopup` accept an optional `onClosed` callback that fires once
  after the handle releases its layer and callbacks; `MavSnackbar`/`MavTooltip` use it to clear
  static state when an overlay is closed externally.
- Added the round-2 optimization plan in `docs/OPTIMIZATION_PLAN_2.md` (lifecycle, `_wired*`
  cleanup, app-bar/keyboard detach, bulk list/table/tree, text-field hot path).
- Collection controls now use internal `MavItemEventWiring` for item change events; removed
  and replaced items are unsubscribed and detached from their logical/visual parent.
- `MavList` keeps its Fenwick height index in a growable list: appends and tail removals are
  amortized O(log n), middle edits rebuild once, and realization no longer scans children by
  index. Single append/tail-remove on a non-virtualized list updates only the affected rows;
  divider sync is skipped during realization and changes properties only when needed.
- `MavTopAppBar` subscribes to an external `ScrollViewer` only while attached.
  `MavKeyboardAvoidingHost` keeps its last focused field weakly, and `MavTheme.Observe`
  no longer roots views from its static `Changed` subscription.
- `MavDataTable` indexes rendered rows by source item, takes a direct append/tail-remove/
  replace path for unsorted, unfiltered, non-paginated tables, prunes selection in O(S+N),
  and updates only the affected row when an `INotifyPropertyChanged` item changes.
  Item property subscriptions are attached only while the table is in the visual tree.
- `MavTreeView` caches one row per `MavTreeNode`, reuses the common child prefix on
  expansion/collapse/filter changes, invalidates only changed nodes, and computes filter
  visibility in a single memoized pass. Selection, scroll offset and expanded state are
  preserved.
- `MavTextField`/`MavTextArea` consolidate state updates into one `ApplyTheme` call and skip
  it when the theme, error/focus/enabled state and floating-label state are unchanged.
  Text width and outlined-notch geometry remain cached.

### Removed

- `MavValidation.GetRequired` / `SetRequired` and `GetEmail` / `SetEmail`: duplicates of `GetIsRequired` / `SetIsRequired` and `GetIsEmail` / `SetIsEmail`. Use the `Is*` names.

### Fixed

- `MavTabs` gives its content the remaining height (bar, divider and a star row) instead of
  stopping at the content's desired size, which left a blank area under short pages.
- `MavTextField` and `MavTextArea` keep their floating label in the right place when text is set
  before the control is shown (a prefilled sign-in form, a dialog built offline). Without a top
  level there is no frame clock, so the state is applied at once and re-checked on attach instead
  of leaving the label drawn on top of the text.
- Closing snackbar/tooltip/search/dialog/popover overlays externally (top-level close,
  page teardown or direct handle disposal) now clears their active handles, timers and
  open state instead of leaving stale static/instance references.

## [0.1.0] — 2026-10-22

First complete preview: all planned stages 0–12 are implemented and verified on Linux/X11,
Android (API 37 emulator and a real Android 15 device) and WebAssembly (gallery).

### Added

- 60+ Material Design 3 controls drawn in pure C# — no Avalonia control themes or AXAML
  templates required: buttons/FAB/segmented buttons, text fields and search, selection
  controls, chips, cards, lists and tables, navigation (top/bottom app bars, tabs, rail,
  drawer, breadcrumbs, stepper), dialogs and sheets, date/time/color pickers, progress and
  charts, avatars, badges, carousel, split view, tree view and more.
- Material 3 theming: HCT color engine, seed colors, light/dark/dynamic schemes, contrast
  levels, 15-role typography scale, shape/elevation/state/motion tokens, live theme changes.
- Embedded fonts: Roboto, Material Symbols Rounded, classic Material Icons and
  Material Design Icons in three presets (`all`, `no-mdi`, `minimal`); `MavIcons` exposes
  ~10,800 glyph constants with automatic font selection.
- Overlay system (`MavOverlay`): dialogs, menus, popovers, rich tooltips, bottom/side
  sheets and snackbars with a dismissal stack, owner-page awareness and a fallback host
  for navigation pages.
- Navigation: `MavNavigationPage` with its own page stack, chrome refresh, back gestures,
  safe-area handling and keyboard-aware layout.
- Localization 2.0: providers (`ResourceManager`, `Chained`, `Delegate`), `SetCulture`,
  `Observe`/`Localize`, `Keys`/`Format`, AXAML `{md:Loc}` markup extension and attached
  properties.
- Performance: virtualized `MavList` sources in a `ScrollViewer`, chunked `MavDataTable`
  rows with responsive compact/card mode, trimming and AOT compatibility.
- Adaptive Control Gallery for desktop, browser and Android, plus the desktop/Android demo
  hosts.
- `MavCarousel.Layout`: four distinct layouts — `Hero`, `CenterAligned`, `MultiBrowse` and
  `Uncontained` (previously the property was a no-op).
- `MavSplitView.InitialRatio`: initial pane width as a fraction of the control width; it is
  applied once on the first layout, clamped to `MinPaneWidth`/`MaxPaneWidth`, and an
  explicit `PaneWidth` takes priority (previously the property was unused).
- Accessibility helpers (contrast utilities, automation properties) and edge-zone back
  gesture support.
- This changelog and the versioning policy.

### Changed

- Keyboard insets from `MavKeyboardAvoidingHost.SimulatedKeyboardHeight` are contributed
  only while the host is attached to the visual tree; the effective overlay inset is the
  maximum across attached hosts.
- Overlay root size follows the `TopLevel` client size, so the scrim covers the whole
  window after resize, rotation and safe-area changes.
- The control catalogue in `docs/AGENTS.md` §5 now matches the public API; the private
  `Hue`/`HueSelected` members of the color picker are no longer documented as public API.
- Test counters and stage status are synchronized across `docs/PLAN.md`,
  `docs/AGENTS.md` and `docs/HANDOFF.md` (182 headless tests).

### Fixed

- Dialog and scrim were offset after visiting a page with a simulated keyboard preview:
  the global overlay kept the page's 120 dp inset and the overlay root stayed shorter
  than the window.
- Indefinite snackbars now always show a close button.
- Edge-to-edge is applied only on Android API 35+, system bars are set after attach, and
  an empty overlay host no longer intercepts input.

### Notes

- Target framework: .NET 10; UI framework: Avalonia 12.1.4.
- RTL is deliberately not implemented yet.
- Android builds require the `android` workload and SDK platform 36; see
  `docs/AGENTS.md` §8.
