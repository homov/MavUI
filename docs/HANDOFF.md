# Handoff: Md3 navigation / UI testing

Обновлено: 22 октября 2026  
Язык общения с пользователем: **русский**.  
Все пути ниже указываются **от корня репозитория** (папка с этим файлом).

Репозиторий **не является git-репозиторием**, ветки и коммиты не создаются.

---

## 1. Что читать в первую очередь

1. Этот файл.
2. `docs/PLAN.md` — актуальный план и статусы этапов.
3. `docs/OPTIMIZATION_PLAN.md` — оптимизации по аудиту, раунд 1 (этапы 1–7 закрыты).
4. `docs/OPTIMIZATION_PLAN_2.md` — оптимизации по аудиту, раунд 2 (все этапы 1–8 закрыты).
5. `docs/AGENTS.md` — правила библиотеки и API контролов.

---

## 1a. Состояние на 22 октября 2026 (для следующей сессии)

Короткая сводка, чтобы продолжить без чтения всей истории. Язык последних сессий — English.

**Готово**
- Этапы 0–12 закрыты. **Раунд 2 оптимизаций** (`docs/OPTIMIZATION_PLAN_2.md`)
  закрыт по этапам 1–7: overlay lifecycle, `_wired*`, app bar/keyboard/theme,
  bulk `MdList`, reconciliation `MdDataTable`, инкрементальный `MdTreeView` и
  text-field hot path; этап 8 (полный прогон, сборки и визуальная проверка)
  закрыт. NuGet-публикация сознательно исключена из плана.
- **233/233 headless-тестов**, сборки библиотеки/демо/Android — `0 Warning(s) 0 Error(s)`.
- Реальное устройство: Xiaomi Redmi Note 4, **Android 15 (SDK 35)** после обновления прошивки,
  1080x1920, 420 dpi. USB: в `/etc/udev/rules.d/51-android.rules` была опечатка `UBSYSTEM`
  → исправлено на `SUBSYSTEM`; после прошивки заново принят RSA-ключ ADB.
- Stage 8 полностью: `MdList` virtualization, `MdDataTable` chunking + **compact/card mode**
  (`Layout=Auto/Table/Cards`, `CompactBreakpoint=600`).
- Stage 11: `Md3Fonts` (`all`/`no-mdi`/`minimal`), `IsTrimmable`/`IsAotCompatible`,
  Release APK 65.60 MB → **27.71 MB** (trimmed+AOT, no-mdi), проверен на телефоне.
- **Control Gallery** (Desktop/Web/Android): новый проект, все контролы по 11 группам,
  адаптивная оболочка (drawer ≥1000 dp, rail 700–999 dp, modal drawer <700 dp).
  См. `docs/gallery/README.md`, скриншоты `docs/gallery/`.
- **Структура (22 Oct)**: все sample-хосты перенесены в `samples/` (`Demo`,
  `Demo.Android`, `Gallery`, `Gallery.Browser`, `Gallery.Android`), документация — в
  `docs/`, добавлен `Md3.sln` со всеми проектами.
- **Overlay fix (22 Oct)**: scrim диалога/листа снова покрывает всё окно.
  `OverlayRootGrid` берёт размер из `TopLevel.ClientSize` и пересчитывает
  measure при изменении окна/контейнера; проверено в галерее (scrim на всё окно).
- Исправлено на реальном устройстве: edge-to-edge только API 35+, пустой overlay-host
  не перехватывает ввод, системные бары применяются после attach.
- **Snackbar fix (22 Oct)**: `Indefinite` всегда показывает close-иконку (`MdIconButton`),
  галерея ставит в очередь конечные сообщения; тест
  `Indefinite_snackbar_has_a_close_button`. Web: очередь/автозакрытие/Dismiss проверены.
- **Android 15 re-check (22 Oct)**: установлен свежий APK; `Open dialog`, `Open bottom sheet`,
  `Open side sheet` (открытие, закрытие × и Back), очередь snackbar (Short → Long →
  автозакрытие) и `Indefinite` с × — всё работает. `adb shell input tap/swipe` на Android 15
  работает. Скриншоты: `docs/gallery/android15-*.png`.
- **Localization 2.0 (22 Oct)**: `MdLocalization` больше не требует подписок на события.
  `Observe`/`Localize` сами обновляют текст при смене провайдера или `SetCulture`;
  добавлены `ResourceManagerProvider`, `ChainedProvider`, `DelegateProvider`, `Keys`,
  `Format`, `Refresh`. В AXAML самый короткий вариант — `{md:Loc Key}` /
  `{md:Loc Key=..., Fallback=...}`; attached-свойства `MdLocalization.Key`,
  `.Fallback`, `.Target` (Text/Label/Placeholder/Title/Tooltip) остались для
  динамической смены ключа. Код приложения не подписывается на события.
  13 новых тестов (`LocalizationTests`), всего 171.
- **Keyboard-preview leak fix (22 Oct)**: после страницы Layout (её `MdKeyboardAvoidingHost`
  с `SimulatedKeyboardHeight = 120`) глобальный overlay-root оставался с нижним отступом
  120 dp: scrim не доходил до низа, а диалог был смещён вверх. Теперь
  `MdKeyboardAvoidingHost` передаёт высоту в `MdOverlay` только пока сам находится
  в визуальном дереве, а при отсоединении пересчитывает максимум по оставшимся хостам.
  Регрессии: `Leaving_a_keyboard_preview_page_restores_the_full_overlay_height`,
  `Dialog_opened_after_a_keyboard_preview_page_is_centered`.

- **Оптимизации (22 Oct)**: `docs/OPTIMIZATION_PLAN.md` закрыт полностью (этапы 1–7):
  кэш дуг/outline, инкрементальные `MdList`/`MdDataTable` без пересоздания строк,
  lifecycle `MdOverlay`, `IMdThemeService : IDisposable`, инвалидация `WidthCache`.
  Добавлено 27 тестов (всего **209** на тот момент), поведение проверено в demo.
- **Оптимизации, раунд 2 (22 Oct)**: `MdOverlay` освобождает content/callbacks и
  сообщает владельцу через `onClosed`; stale handles snackbar/tooltip и
  `IsOpen`/`Closed` у search/dialog/popover синхронизируются при внешнем закрытии.
  Все 13 `_wired*`-контролов используют `MdItemEventWiring` (отписка + отвязка
  parent). `MdTopAppBar` отписывается от `ScrollViewer` при detach,
  `MdKeyboardAvoidingHost` хранит последний focused-контрол слабо, `MdTheme.Observe`
  больше не рутит view. Добавлено 12 регрессий.
- **MdList bulk (22 Oct)**: `HeightIndex` на `List<double>` + Fenwick, O(log n)
  append/tail-remove, один rebuild на middle-изменение. `SyncChildrenToDesired`
  убрал `IndexOf`-скан, dividers не синхронизируются во время realization, а
  одиночные append/tail-remove обновляют только затронутые контролы. Добавлены
  Stopwatch-регрессии на 5000 элементов; итог — **224/224**.
- **MdDataTable reconciliation (22 Oct)**: direct path для append/tail-remove/
  replace без фильтра/сортировки/пагинации, индекс `source item → row`, общий
  `SyncRenderedControls` без `IndexOf`-скана, O(S+N) pruning selection и
  property-подписки только на время attach. `INotifyPropertyChanged` обновляет
  одну строку; sort/filter сохраняются через общий reconciler. Итог — **226/226**.
- **MdTreeView incremental (22 Oct)**: кэш строк по узлу, sync по общему префиксу,
  мемоизированный фильтр за один проход, точечная инвалидация `NodeChanged`/
  `ChildrenChanged`, снятие подписок при удалении поддерева. Selection/scroll/
  expanded state сохраняются; итог — **231/231**.
- **Text field hot path (22 Oct)**: один `ApplyTheme` на обновление состояния и
  ранний выход по кэшу темы/error/focus/floating state; дубли
  `SyncTrailing`/`SyncFooter` удалены. Ширина текста и notch остаются в
  `MdTextMetrics`/`MdFieldChromeCache`; Stopwatch-регрессия на 2000 вводов и
  20 смен темы; итог — **232/232**.
- **Stage 12 (22 Oct)**: каталог `docs/AGENTS.md` §5 сверен с публичным API (208 типов,
  119 публичных Md-классов, 91 строка каталога): у `MdColorPicker` убраны непубличные
  `Hue`/`HueSelected`.
  Реализованы `MdCarousel.Layout` (`Hero`/`CenterAligned`/`MultiBrowse`/`Uncontained`) и
  `MdSplitView.InitialRatio` (применяется один раз при первой раскладке, явный `PaneWidth`
  приоритетнее); добавлены 9 тестов, всего **182** (на тот момент). Создан `CHANGELOG.md` с политикой
  версий; NuGet-публикация в план не входит. Раскладки карусели проверены нативно.

**Не закончено / следующая сессия**
1. Обязательных этапов нет: раунд 2 оптимизаций закрыт полностью.
2. Опционально: пересобрать Release trimmed/AOT APK галереи и демо после snackbar-правки.

**Полезные команды**

```bash
dotnet build Md3.Avalonia/Md3.Avalonia.csproj -v q --nologo
dotnet test Tests/Md3.Avalonia.Tests.csproj --nologo          # 233/233
MD3_SMOKE=1 dotnet run --project samples/Demo/Md3.Demo.csproj --no-build

dotnet run --project samples/Gallery/Md3.Gallery.csproj                # desktop
dotnet run --project samples/Gallery.Browser/Gallery.Browser.csproj    # WASM (wasm-tools installed)
dotnet build samples/Gallery.Android/Gallery.Android.csproj            # com.md3.gallery
```

GUI-проверки: `MD3_WINDOW_TITLE="Md3 Control Gallery" python3 tools/linux/md3ui.py ...`.
Для галереи в background: `nohup dotnet run --project samples/Gallery/Md3.Gallery.csproj --no-build &`.
Web-сервер печатает `App url: http://127.0.0.1:<port>/`.
Фоновые процессы (desktop gallery, browser server) в конце сессии остановлены.

**Ограничения, важные для автоматизации**
- На **Android 15** `adb shell input tap/swipe` работает (проверено). На старом Android 11
  этот же ROM давал `ToolType=Unknown`, и Avalonia игнорировала такие события; если снова
  столкнёшься — использовать Tab/Enter/Back или реальный палец.
- Synthetic pointer events на Web-canvas ненадёжны для overlay-контента (snackbar close,
  dialog buttons); страничные кнопки работают. Для проверки overlay лучше desktop/X11
  (`md3ui.py`) или реальный браузер.
- `MdSnackbar` глобальный, FIFO; `Indefinite` теперь всегда с ×.

**Файлы, изменённые в этой сессии (кратко)**
- Library: `MdDataTable` (cards), `MdList` (virtualization), `MdSnackbar` (close),
  `MdAutomation`, `MdGestureZones`, `MdAccessibility`, `MdPropertyCache`, `MdSystemBars`,
  `Md3.Avalonia.csproj` (fonts/trim/AOT).
- Gallery: `samples/Gallery/` (shared shell/pages), `samples/Gallery.Browser/`, `samples/Gallery.Android/`,
  `docs/gallery/README.md`, `docs/gallery/`.
- Docs: `docs/PLAN.md` (stage 8/11), `docs/AGENTS.md` (gestures/a11y/fonts/trim/gallery),
  `docs/HANDOFF.md`.

---

## 2. Цель проекта

Сделать мобильную навигационную оболочку для `Md3.Avalonia` на чистом C#/Avalonia,
не используя `Avalonia.Controls.NavigationPage` и `Avalonia.Controls.ContentPage`.

Стек: **Avalonia 12.1.4**, **.NET 10** (`net10.0`).

Основные требования:

- Material Design 3 для всей оболочки;
- мобильный телефон — основная цель, desktop — первая площадка для проверки;
- все контролы Md3 должны оставаться доступными;
- pure C#, без AXAML-шаблонов;
- без `[Obsolete]`-слоя совместимости;
- без зависимости от Avalonia control themes: `MdWindow`, `MdScrollViewer`,
  fallback overlay-host;
- библиотека, демо, Android-хост и тесты собираются без предупреждений и ошибок.

---

## 3. Принятая архитектура

### `MdContentPage`

Файл: `Md3.Avalonia/Controls/MdContentPage.cs`

Страница-модель, наследуется от `MdContentView` (тело — унаследованный `Content`).

Ключевые свойства:

- `Title`, `Subtitle`;
- `NavigationIcon`, `ShowAppBar`, `ShowBackButton`, `AppBarVariant`;
- `AddAction(icon, onClick, tooltip)`;
- `BottomBar` (`MdBottomAppBar`);
- `FloatingActionButton`;
- `NavigationAction` — кастомное действие ведущей кнопки app bar;
- `Navigation` — ссылка на `MdNavigationPage`;
- `OnBackPressed()`, `Appearing`, `Disappearing`, `NavigationClicked`.

### `MdNavigationPage`

Файл: `Md3.Avalonia/Controls/MdNavigationPage.cs`

Собственный стек страниц и общая оболочка:

- `SetRoot`, `Push`, `Pop`, `PopToRoot`, `Replace`;
- `NavigationStack`, `CurrentPage`, `CanGoBack`, `StackDepth`;
- `TryGoBack()`, `GoBackCommand`;
- `Transition` (`None` / `Slide` / `Fade`);
- `RefreshChrome()`;
- `PageChanged`, `CanGoBackChanged`;
- `ApplySafeArea`, `SafeAreaOverride`, `SafeAreaInsets`;
- `KeyboardHost` (`MdKeyboardAvoidingHost`).

Поведение back:

1. закрыть верхний overlay через `MdOverlay.TryDismissTop()`;
2. дать текущей странице обработать back через `OnBackPressed()`;
3. сделать `Pop()`.

На desktop `Escape` и `Alt+Left` обрабатываются в `KeyDown` на уровне `TopLevel`.
На Android системный Back приходит одновременно как `Key.Escape` (маппинг
`Keycode.Back`) и как `TopLevel.BackRequested`. Поэтому на Android `Escape`
в `KeyDown` намеренно не обрабатывается: страница и overlay закрываются только
через `BackRequested`. Иначе одно нажатие закрывало бы overlay/страницу дважды,
и Activity завершалась бы.

### Независимость от Avalonia control themes

Библиотека больше не требует `Avalonia.Themes.Fluent`/`Simple` и не содержит
AXAML-шаблонов. Собственные шаблоны предоставляют:

- `MdWindow` — desktop-окно с `ContentPresenter` и `VisualLayerManager`;
- `MdScrollViewer` — прокрутка через `ScrollContentPresenter` и
  `ScrollGestureRecognizer` (touch/wheel/keyboard, без видимых scrollbar);
- `MdOverlay` — обычный `OverlayLayer`, если он есть, иначе fallback-host
  внутри `MdNavigationPage`.

Android `EmbeddableControlRoot` не имеет `VisualLayerManager` без Avalonia theme,
поэтому `MdNavigationPage` регистрирует собственный overlay-host. На desktop
с `MdWindow` используется штатный `OverlayLayer`.

### Safe area и клавиатура

- `ApplySafeAreaInsets()` читает `IInsetsManager.SafeAreaPadding` (или `SafeAreaOverride`).
- `TopLevel.RenderScaling` на Android сначала может быть `1`, а затем стать `2.625`.
  Страница пересчитывает insets по `TopLevel.ScalingChanged` и после `DispatcherPriority.Loaded`,
  поэтому статус-бар/вырез не превращаются в двойной отступ.
- `TopLevel.AutoSafeAreaPadding` отключён для страницы и для host-контейнера:
  insets применяет сама `MdNavigationPage`.
- Верхний inset уходит в `MdTopAppBar.SafeAreaPadding`.
- Нижний inset уходит в `MdBottomAppBar.SafeAreaPadding`, если нижняя панель есть,
  иначе в нижний `Margin` области страницы.
- Боковые insets — в `Margin` страницы.
- FAB остаётся внутри страницы и не перекрывает системную навигацию.
- Страница обёрнута в `MdKeyboardAvoidingHost`: при открытой клавиатуре контент
  поднимается, а сфокусированное поле прокручивается в видимую область.
- `KeyboardHost.SimulatedKeyboardHeight` позволяет проверить это на desktop.

### Snackbar

`MdSnackbar.Show(...)` сам находит `MdNavigationPage` по anchor и поднимается над
нижней панелью/FAB/safe inset. Высота нижней панели уже включает safe-area padding,
поэтому inset не прибавляется повторно.

### Пересчёт overlay при resize / повороте / клавиатуре

Файл: `Md3.Avalonia/Platform/MdOverlay.cs`

- `MdOverlay.Show(anchor, content, ..., onViewportChanged: Action?)` — каждый overlay
  передаёт callback, который умеет пересчитать свою геометрию; callback хранится в
  `OverlayHandle` и живёт ровно столько же, сколько overlay.
- `OverlayRootGrid` вызывает `MdOverlay.NotifyViewportChanged()` по `SizeChanged`
  корневого грида, `TopLevel.ClientSize` (`PropertyChanged`), `ScalingChanged`,
  `InsetsManager.SafeAreaChanged` и `IInputPane.StateChanged`.
- Уведомления коалесцируются: `Dispatcher.UIThread.Post(..., DispatcherPriority.Loaded)`
  и один проход по всем открытым overlay.
- `MdOverlay.SafeAreaOf(visual)` / `SafeAreaBounds(visual)` дают `Thickness` insets и
  прямоугольник вьюпорта без safe area; `BoundsInOverlay(visual, root)` — координаты якоря.
- `MdOverlay.SetSimulatedKeyboardHeight(...)` дублирует
  `MdKeyboardAvoidingHost.SimulatedKeyboardHeight`, поэтому `MD3_KEYBOARD=300`
  поднимает и страницу, и overlay (используется в демо/скриншотах); на Android
  работает реальный `IInputPane`.
- Пересчёт «тихий»: меняются только `Margin`/`MinWidth`/`MaxWidth`/`MaxHeight`/`Width`
  и положение карточки. `MdMotion.ScaleIn` выполняется только при открытии, выбранные
  значения (время, дата, пункт меню) не сбрасываются.
- Свой `Reflow()`/`PlaceCard()`/`PositionCard()` есть у `MdPopover`, `MdMenu` (+ сабменю),
  `MdDialog`, `MdDatePicker`, `MdTimePicker`, `MdSearchView`, `MdBottomSheet`,
  `MdSideSheet`, modal drawer, `MdComboBox` dropdown, `MdTooltip`, `MdRichTooltip`,
  `MdSnackbar`.
- `MdPopover` пересчитывает `Width`/`MaxHeight` при каждом изменении вьюпорта — это
  снимает «залипание» ширины после resize.
- `MdMenu` пересчитывает `MaxWidth`/высоту, измеряет строки и заново клампит карточку
  по 8 dp; если открыто сабменю, оно тоже пересчитывается, а при откреплённом якоре
  закрывается.

### Overlay lifecycle

- `MdOverlay` запоминает страницу-владельца overlay.
- `MdOverlay.DismissForPage(page)` закрывает dialogs/sheets, открытые со страницы.
- `MdContentPage.NotifyDisappearing()` вызывает `DismissForPage(this)`.

---

## 4. Что уже реализовано и проверено

### Сборка и тесты

Текущая платформа разработки — **Linux Mint (X11)**, рабочая папка `/home/homo/src/md3`.
Windows остаётся альтернативной площадкой и местом, где сняты старые скриншоты.

```bash
dotnet build Md3.Avalonia/Md3.Avalonia.csproj -v q --nologo
dotnet build samples/Demo/Md3.Demo.csproj -v q --nologo
dotnet test Tests/Md3.Avalonia.Tests.csproj --nologo
```

Android-хост собирается локально через Linux SDK (нужны workload `android` и
платформа `platforms/android-36`):

```bash
dotnet workload install android
dotnet build samples/Demo.Android/Demo.Android.csproj -v q --nologo
```

Результат на Linux: `0 Warning(s) 0 Error(s)` для библиотеки, демо и Android-хоста,
**233 теста проходят**. `MD3_SMOKE=1 dotnet run --project samples/Demo/Md3.Demo.csproj`
печатает `MD3_SMOKE_OK` на реальном X11-дисплее.

Тестовый проект: `Tests/Md3.Avalonia.Tests.csproj`

- `Avalonia.Headless.XUnit` 12.1.4 (xunit v3);
- `Microsoft.NET.Test.Sdk`;
- `xunit.v3`, `xunit.runner.visualstudio`;
- `Tests/TestAppBuilder.cs` — headless `Application`;
- `Tests/NavigationTests.cs` — стек, `Replace`, запрет повторного push,
  `OnBackPressed`, `GoBackCommand`, safe area, `TopLevel.BackRequested`,
  fallback overlay-host, `MdScrollViewer`, date/time pickers, клэмп
  `MdMenu`/`MdPopover`/`MdSearchView`/`MdColorPicker` на узком хосте и
  сохранение edge-margin при якоре у правого края; resize при открытом
  `MdMenu`/`MdPopover`/`MdDialog` и pickers (карточка остаётся внутри вьюпорта,
  popover не «залипает» на первой ширине);
- `Tests/ListTests.cs` — dividers;
- `Tests/KeyboardTests.cs` — клавиатурный inset и возврат к полной высоте,
  popover над симулированной клавиатурой;
- `Tests/ButtonTests.cs` — `MdButton`, `MdIconButton`, `MdFab`/`MdExtendedFab`,
  `MdButtonGroup`, `MdSplitButton`;
- `Tests/InputTests.cs` — `MdTextField`, `MdTextArea`, `MdPinInput`,
  `MdNumberBox`, `MdTagInput`, `MdRatingBar`;
- `Tests/SelectionTests.cs` — `MdSwitch`, `MdCheckBox`, `MdRadioGroup`, `MdSlider`,
  `MdSegmentedButton`, `MdChip`, `MdChipGroup`, `MdSelect`, `MdComboBox`;
- `Tests/PickerTests.cs` — `MdDatePicker`, `MdDatePickerField`, `MdTimePicker`,
  `MdColorPicker`;
- `Tests/FeedbackTests.cs` — snackbar FIFO, progress, skeleton, `MdInfoBar`;
- `Tests/ChromeTests.cs` — top/bottom app bar, drawer, tabs, stepper, pagination;
- `Tests/ListTableTreeTests.cs` — `MdList`, `MdDataTable`, `MdTreeView`, `MdCarousel`;
- `Tests/ContainerTests.cs` — `MdCard`, `MdAvatar`, `MdBadge`, `MdDivider`,
  `MdLabel`, `MdSafeArea`;
- `Tests/ValidatorTests.cs` — правила `MdFormValidator` (Required, MinLength/MaxLength,
  Email, Numeric, Pattern, Matches, Must, ClearErrors) и attached `MdValidation` +
  `ValidateTree`;
- `Tests/AccessibilityTests.cs` — автоматические имена/роли/`IsRequiredForForm`,
  сохранение явно заданных приложением значений;
- `Tests/KeyboardAccessibilityTests.cs` — Space/Enter для кнопок/FAB/переключателей,
  стрелки для слайдеров;
- `Tests/ContrastTests.cs` — WCAG-контраст пар ролей для
  `MdContrast.Standard/Medium/High` в light/dark;
- `Tests/OverlayHitTestTests.cs` — пустой fallback overlay-host не перехватывает
  hit-test (регрессия реального Android);
- `Tests/GestureZoneTests.cs` — edge-зона системного back: slider и carousel
  игнорируют touch из первых 20 dp и работают вне её;
- `Tests/VirtualizationTests.cs` — окно реализации `MdList`, скролл, dividers,
  смена `ItemsSource`;
- `Tests/DataTableVirtualizationTests.cs` — чанковая отрисовка строк `MdDataTable`,
  пагинация, отсутствие внутреннего viewport;
- `Tests/DataTableCompactTests.cs` — Auto/Table/Cards, breakpoint 600 dp, переход при
  resize, sort-чипы, label/value карточки, чекбоксы выбора;
- `Tests/VirtualizationBenchmarks.cs` — замеры первой раскладки на 1000 элементов/строк
  (вывод через `ITestOutputHelper`);
- `Tests/ControlSmokeTests.cs` — создание и раскладка всех групп контролов из
  `docs/AGENTS.md` §5 (кнопки, поля, selection, pickers, feedback, chrome, containers);
  `MdWindow` проверяется как отдельный TopLevel.

Headless-окно Avalonia не создаёт `OverlayLayer`, но `MdNavigationPage` теперь
регистрирует fallback overlay-host, поэтому dialog/picker/sheet можно проверять
и в xunit.

### Виртуализация (этап 8)

- `MdList` (`ItemsSource` + `ScrollViewer`): окно видимых элементов считается по
  `TranslatePoint`/`Viewport` с overscan 480 dp; сверху и снизу выставляются spacer-ы,
  чтобы extент соответствовал полной высоте; измеренные высоты кешируются после
  `Arrange`. Невидимые `MdListItem` не создаются. Ручной режим (`AddItem`/`AddSubheader`)
  не менялся — все `Children` остаются на месте.
- Малые источники (≤60) и списки без `ScrollViewer` реализуются целиком (headless-тесты
  и простые страницы ведут себя как раньше).
- `MdDataTable`: строки добавляются чанками по 100 при прокрутке тела, пока
  `MaxBodyHeight` задан; пагинация работает как раньше, без внутреннего viewport
  страница строится целиком.
- `MdDataTable` compact/card: `Layout` = `Auto` (default), `Table`, `Cards`;
  `CompactBreakpoint` = 600 dp; на узком хосте строка рисуется как `MdCard`
  (первая колонка — headline, остальные — label/value), сортировка — чипами,
  выбор — чекбоксом. Layout пересобирается при пересечении breakpoint.
  Проверено: 400x800 — карточки (`linux-table-cards-400.png`), 1180x800 — таблица
  (`linux-table-desktop.png`).
- Замеры (headless, viewport 300 dp): `MdList` 1000 → 14 реализованных, ~29 мс;
  `MdDataTable` 1000 → 100 реализованных, ~25 мс.

### Жестовые зоны (этап 7)

- `Md3.Avalonia/Platform/MdGestureZones.cs` — `IsInSystemEdge`/`ShouldDeferToSystem`
  (20 dp от левого/правого края `TopLevel`).
- При touch-press в этой зоне `MdListItem` swipe actions, `MdCarousel`, `MdSlider`/
  `MdRangeSlider`, `MdSplitView`, `MdColorPicker`, `MdTimeDial`, `MdRadialGauge` не
  начинают горизонтальный drag и не захватывают pointer — жест уходит системе
  (`TopLevel.BackRequested` → overlay → страница). Mouse не ограничивается.
- `MdRefreshIndicator` не конфликтует: вертикальный pull и проверка draggable-потомков.
- Проверено на реальном устройстве: системный Back закрывает overlay, затем страницу;
  тесты — `GestureZoneTests` (edge-детект, slider, carousel).

### Размер, trim и AOT (этап 11)

- `Md3Fonts` (MSBuild): `all` (по умолчанию) / `no-mdi` (без Material Design Icons) /
  `minimal` (Roboto + Material Symbols). Размер `Md3.Avalonia.dll`: 4.87 / 3.69 / 3.35 MB.
  `minimal` не подходит для классических `MdIcons` (демо падает на отсутствующем
  glyphTypeface) — для стандартных иконок используйте `no-mdi`.
- Библиотека: `IsTrimmable=true`, `IsAotCompatible=true`. `MdPropertyCache` аннотирован
  `DynamicallyAccessedMembers`; call sites и `MdSystemBars.ApplyIconTheme` имеют
  обоснованные `UnconditionalSuppressMessage`. `DisplayMemberPath`/`PropertyName`
  в trimmed-приложении требуют rooting свойств модели (или `ValueSelector`/`ItemTemplate`).
- Release-сборка Android по умолчанию включает trimming и AOT:
  `dotnet publish samples/Demo.Android/Demo.Android.csproj -c Release -p:PublishTrimmed=true
  -p:Md3Fonts=no-mdi -p:AndroidPackageFormats=apk -o <dir>`.
  Размеры: baseline без trim/AOT 65.60 MB → trimmed+AOT 29.08 MB (all fonts) →
  27.71 MB (`no-mdi`), т.е. −58%.
- Trimmed+AOT APK установлен на Redmi Note 4 (Android 11): главная страница и
  `MdFabMenu` overlay работают, crash в logcat отсутствует (`android-trimmed-*.png`).

### Accessibility (этап 10, без RTL)

- `Md3.Avalonia/Platform/MdAutomation.cs` — `SetName`, `SetHelpText`, `SetRequired`,
  `SetControlType`. Сгенерированные значения не перезаписывают явные значения приложения.
- Автоматически заполняются: кнопки (`Text`, для icon-only — tooltip), текстовые поля
  (label + footer/error), `MdSelect`/`MdComboBox` (label, роль `ComboBox`),
  `MdNumberBox`/`MdPinInput` (роль `Edit`), `MdCheckBox`/`MdRadioButton`
  (label + роли), `MdSwitch` (роль `CheckBox`), `MdSlider`/`MdRangeSlider` (роль `Slider`),
  `MdChip`/`MdMenuItem` (текст/роль), `MdListItem` (headline + supporting text),
  `MdDialog`/`MdBottomSheet` (title/message).
- `MdValidation.IsRequired` → `AutomationProperties.IsRequiredForForm`.
- Клавиатура: `MdInkSurface` обрабатывает Space/Enter (кнопки, чипы, пункты списка/меню),
  `MdSwitch`/`MdCheckBox`/`MdRadioButton` — Space/Enter, слайдеры — стрелки; focus-ring
  рисуется в `MdInkSurface` только при keyboard-focus.
- `Md3.Avalonia/Theming/MdAccessibility.cs` — `RelativeLuminance`, `ContrastRatio`,
  `MeetsContrast` (WCAG 2.1).
- RTL намеренно не входит в этот этап.

### Smoke-тест

```bash
MD3_SMOKE=1 dotnet run --project samples/Demo/Md3.Demo.csproj --no-build
```

Ожидаемый вывод:

```text
push controls: depth=2, canGoBack=True, command=True
dialog open: overlay=True, depth=2
back closes overlay: overlay=False, depth=2
dialog for page leave: overlay=True
push inbox: depth=3, title=Inbox, overlay=False
pop: depth=2, title=Controls
pop to root: depth=1, title=Md3 Navigation, command=False
MD3_SMOKE_OK
```

### Проверка на Linux Mint (X11, текущая площадка)

Проверено через `tools/linux/md3ui.py` на дисплее `:0` (5120x1440):

- сборка библиотеки, демо и Android-хоста — `0 Warning(s) 0 Error(s)`;
- `MD3_SMOKE=1` — весь ожидаемый вывод, `MD3_SMOKE_OK`;
- окно рисуется без Avalonia theme, шрифты и иконки на месте;
- клик по «Show dialog» открывает `MdDialog`, `Escape` закрывает его;
- клик по «Open list» + колесо мыши прокручивают `MdList`;
- safe area `MD3_SAFE_AREA=24,48` на 400x800 и `48,0,0,24` на 640x360;
- скролл страницы в landscape, контент полностью достижим;
- **resize при открытом overlay**: окно 900x640 → `wmctrl -e` до 720x520,
  открытый `MdPopover` пересчитал карточку и остался у своего якоря
  (`linux-popover-resized.png`);
- 320x640: popover у правого края держит 12 dp edge-margin
  (`linux-phone-popover-clamped.png`);
- `MD3_WINDOW=320x640 MD3_KEYBOARD=300`: `MdSearchView` пересчитал высоту и
  прокручивается над симулированной клавиатурой (`linux-phone-search-keyboard.png`);
- 400x800 + safe area `24,48`: `MdDialog` учитывает top/bottom insets
  (`linux-safe-dialog.png`);
- 640x360 + safe area `48,0,0,24`: `MdTimePicker` перешёл в горизонтальную
  раскладку и полностью помещается (`linux-land-timepicker.png`).

| Файл | Что показывает |
|---|---|
| `linux-home-dark.png` | главная 1180x800, dark |
| `linux-home-light.png` | главная 1180x800, light |
| `linux-dialog.png` | `MdDialog` в overlay |
| `linux-phone-home.png` | 400x800 + safe area `24,48` |
| `linux-land-home.png` | 640x360 + inset слева 48 |
| `linux-land-scrolled.png` | landscape после прокрутки колесом |
| `linux-popover-resized.png` | 720x520 после resize окна: popover пересчитан |
| `linux-phone-popover-clamped.png` | 320x640: popover у правого края, edge-margin 12 dp |
| `linux-phone-search-keyboard.png` | 320x640 + клавиатура 300: search view над IME |
| `linux-safe-dialog.png` | 400x800 + safe area `24,48`: dialog внутри insets |
| `linux-land-timepicker.png` | 640x360 + левый inset: time picker side-by-side |

### Проверка на Windows (legacy, этапы 2–4.5)

Проверено нативно на Windows (DPI 120, scale 1.25, окно `1477x1039` физических px):

- app bar, заголовок/подзаголовок, back-стрелка, action `Save`;
- push/pop/pop-to-root, сохранение состояния страницы;
- `Escape`, `Alt+Left`, закрытие overlay перед pop;
- actions нижней панели (search/settings) — мнимый баг WSLg не подтвердился;
- FAB (docked в `MdBottomAppBar`) и snackbar;
- dialog, bottom sheet, закрытие по `Escape` и кнопкам;
- список Inbox, dividers, detail-страница с actions;
- dark/light тема;
- safe area `MD3_SAFE_AREA=24,48`;
- клавиатурный inset `MD3_KEYBOARD=300`;
- телефонное окно `MD3_WINDOW=400x800`: главная страница переносится в несколько
  рядов, диалог занимает почти всю ширину (адаптивный `MdDialog`);
- узкие экраны и landscape (`MD3_WINDOW=320x640`, `360x640`, `640x360` с
  `MD3_SAFE_AREA=48,0,0,24`): меню, popover, search view, time/date picker,
  color picker, dialog и bottom sheet;
- 48 dp touch-цели: клик по прозрачному padding иконки срабатывает.

Скриншоты (`docs/handoff/`):

| Файл | Что показывает |
|---|---|
| `win-home-final.png` | главная, dark |
| `win-home-light-final.png` | главная, light + snackbar |
| `win-controls-final.png` | страница Controls |
| `win-controls-light-final.png` | Controls в light, snackbar после Save |
| `win-dialog-touch.png` | диалог после перехода на 48 dp |
| `win-touch-padding.png` | клик по padding иконки нижней панели |
| `win-snackbar-safearea2.png` | snackbar над нижней панелью + safe inset |
| `win-keyboard-safearea.png` | safe area + клавиатурный inset |
| `win-phone-home.png` | телефонное окно 400dp, safe area |
| `win-phone-dialog.png` | адаптивный диалог почти на всю ширину |
| `win-phone-controls.png` | форма на 400dp |
| `win-dialog-adaptive.png` | диалог на desktop (центр, max 560) |
| `win-no-theme-final.png` | desktop без Avalonia theme |
| `win-no-theme-final-dialog.png` | overlay на theme-free окне |
| `win-inbox.png`, `win-detail.png`, `win-sheet.png` | список, detail, bottom sheet |
| `win-altleft.png` | back по `Alt+Left` |
| `win-320-home.png`, `win-320-overlays.png`, `win-320-menu.png`, `win-320-popover.png` | 320 dp: главная, страница overlay, меню и popover у правого края |
| `win-360-home.png`, `win-360-overlays.png`, `win-360-menu.png`, `win-360-popover.png`, `win-360-search.png` | 360x640: главная, overlay, меню, popover (с edge-margin), search view |
| `win-360-time.png`, `win-360-date.png`, `win-360-color.png`, `win-360-color-scrolled.png` | 360x640: time/date/color picker и скролл внутри color picker |
| `win-land-home.png`, `win-land-scroll.png`, `win-land-overlays.png` | landscape 640x360 + inset слева: chrome и скролл страницы |
| `win-land-menu.png`, `win-land-popover.png`, `win-land-search.png` | landscape: меню (над якорем), popover, search view |
| `win-land-time.png`, `win-land-date.png`, `win-land-date-scroll.png`, `win-land-color.png`, `win-land-color-scrolled.png` | landscape: pickers (time — горизонтальная раскладка) и их скролл |
| `win-land-dialog.png`, `win-land-sheet.png` | landscape: диалог и bottom sheet |
| `wslg-*.png` | старые WSLg-снимки (не доверять) |

### Android-проверка

Собрано и установлено на локальном AVD `Medium_Phone_API_37.0` (Android 17,
1080x2400, density 420 ≈ 411 dp), запущенном в Linux через KVM. Пакет `com.md3.demo`,
activity `com.md3.demo/crc64351746bbe631ada1.MainActivity`.

Ранее (этап 4) всё то же проверялось на AVD `pixel_7_-_api_36_0`; перенос на Linux
проверен заново: сборка, установка, запуск, safe area и отрисовка главной страницы.

Проверено:

- приложение запускается и рисует `MdNavigationPage` без Avalonia theme;
- safe area: статус-бар и вырез камеры, нижний gesture bar;
- системный Back: overlay закрывается, затем страница; на root Activity
  завершается только по второму Back;
- `MdDialog`, `MdBottomSheet`, snackbar, `MdDatePicker` через fallback overlay-host;
  `MdTimePicker`, `MdMenu` и `MdPopover` используют тот же путь и покрыты headless-тестами/кодом;
- `MdScrollViewer`: swipe-прокрутка формы, app bar остаётся на месте;
- soft keyboard: `TextInputOptions`, Gboard открывается, поле остаётся доступным;
- `MdDatePicker` на 411 dp и на симулированных 360x640 dp (адаптивная ширина,
  вертикальный скролл при нехватке высоты);
- 48 dp touch targets.

#### Этап 6: edge-to-edge, системные бары, поворот (21 октября 2026)

Проверено на том же AVD `Medium_Phone_API_37.0` (Linux/KVM, Android 17):

- **edge-to-edge**: `MdSystemBars.Attach(navigation, edgeToEdge: true)` +
  `styles.xml` (`windowDrawsSystemBarBackgrounds=true`, `statusBarColor`/
  `navigationBarColor=@android:color/transparent`,
  `enforceStatusBarContrast=false`/`enforceNavigationBarContrast=false`).
  Контент рисуется под статус-баром и gesture bar, двойных отступов нет:
  верхний inset уходит в `MdTopAppBar.SafeAreaPadding`, нижний — в
  `MdBottomAppBar.SafeAreaPadding`/`Margin` страницы.
- **системные бары и тема**: `MdSystemBars.Apply()` ставит
  `insets.SystemBarColor = MdTheme.Current.Color.Surface` и `SystemBarTheme`
  (Light/Dark) по `MdTheme.Current.IsDark`; подписка на `MdTheme.Changed`
  держит бары в синхроне при смене темы и seed-цвета. На старте — ресурсы
  `@bool/md3_demo_light_system_bars` (light/night) и
  `@color/md3_demo_window_background` (`#F9F9FF` / `#111318`).
- **поворот экрана** (`adb shell settings put system user_rotation 1`):
  `ConfigurationChanges` включает `Orientation | ScreenSize | UiMode | Density |
  Locale | LayoutDirection`, поэтому Activity не пересоздаётся. Открытый
  `MdDialog` пересчитал геометрию и остался внутри вьюпорта: адаптивная ширина
  вместо «на всю ширину», центрирование, chrome (app bar/bottom bar/FAB)
  перестроился, safe area пересчитана.
- **системный Back** в landscape: первое нажатие закрыло открытый overlay
  (`MdDialog`), страница осталась на месте — Activity не завершилась.
- **реальное устройство (22 октября 2026)**: Xiaomi Redmi Note 4 (`carbon_mido`),
  Android 11 (SDK 30), 1080x1920, density 420. Установлен тот же APK, проверены
  запуск, отрисовка без Avalonia theme, safe area, светлая/тёмная тема (клавиатурой
  Tab/Enter), системные бары, поворот, Back (overlay → страница → Activity), стек
  страниц, `MdList` (Inbox), snackbar и overlay `MdFabMenu`. Скриншоты
  `android-real-*.png`.

  Исправлено по итогам проверки:
  - **edge-to-edge только на Android 15+**: на Android 11 запрос edge-to-edge
    оставлял прозрачные бары, за которыми висел wallpaper (серый статус-бар).
    `MobileApp` включает его при `Build.VERSION.SdkInt >= 35`, иначе бары
    окрашиваются в `Surface`.
  - **overlay-host не должен перехватывать ввод**: пустой fallback
    `OverlayRootGrid` теперь `IsHitTestVisible=false` и включается только при
    наличии layer'а (иначе он съедал все тапы на Android).
  - **отложенное применение системных баров**: после attach окно ещё не имеет
    финальных атрибутов; `MdSystemBars` повторяет `Apply` на
    `DispatcherPriority.Loaded`.

  Ограничение автоматизации: `adb shell input` на этом ROM генерирует touch-события с
  `ToolType=Unknown`, которые Avalonia игнорирует, поэтому клики проверялись
  клавиатурой (Tab/Enter) и системным Back; **реальный палец проверен владельцем —
  кнопки и оверлеи реагируют**, touch работает.

- **повторная проверка после обновления прошивки (22 октября 2026)**: телефон обновлён
  до **Android 15 (SDK 35)**; `adb shell input tap/swipe` теперь работает. Установлена
  галерея `com.md3.gallery`; проверены edge-to-edge, `Open dialog`, `Open bottom sheet`,
  `Open side sheet` (закрытие × и Back), очередь snackbar и `Indefinite` с ×. Всё работает.
  Скриншоты — `docs/gallery/android15-*.png`.

Скриншоты (`docs/handoff/`):

| Файл | Что показывает |
|---|---|
| `android-real-home.png` | реальное устройство, light, системные бары = Surface |
| `android-real-dark.png` | реальное устройство, dark + snackbar |
| `android-real-landscape.png` | реальное устройство, landscape 1920x1080 |
| `android-real-controls-dark.png` | страница Overlays & pickers, dark |
| `android-real-overlay.png` | `MdFabMenu` overlay на реальном устройстве |
| `android-real-back.png` | Back закрыл overlay, страница осталась |
| `android-home.png` | главная на Android (AVD API 37, Linux/KVM), safe area |
| `android-home-avd.png` | главная, AVD API 37, этап 6 (edge-to-edge, прозрачные бары) |
| `android-dialog-avd.png` | `MdDialog` в fallback overlay-host, AVD API 37 |
| `android-land-dialog.png` | landscape 2400x1080: открытый диалог пересчитан и внутри вьюпорта |
| `android-back-portrait.png` | портрет после Back: overlay закрыт, страница на месте |
| `android-controls.png` | форма Controls (Windows-эмулятор, этап 4) |
| `android-dialog.png` | `MdDialog` (этап 4) |
| `android-datepicker.png` | date picker на 411 dp (этап 4) |
| `android-datepicker-360dp.png` | date picker на 360x640 dp (этап 4) |
| `android-sheet.png` | `MdBottomSheet` (этап 4) |

### Control Gallery (Desktop / Web / Android)

Новое приложение `Gallery` показывает все контролы библиотеки, сгруппированные по назначению
(Overview, Actions, Inputs, Selection, Pickers, Navigation, Layout, Collections, Feedback,
Overlays, Theming).

- **Адаптивная оболочка**: постоянный `MdNavigationDrawer` при ширине ≥1000 dp,
  `MdNavigationRail` при 700–999 dp, модальный drawer из app bar на телефоне (<700 dp).
  Внутри используется `MdNavigationPage`/`MdContentPage`, поэтому safe area, Back,
  Escape и overlay-хост работают как в обычном приложении.
- **Desktop**: `dotnet run --project samples/Gallery/Md3.Gallery.csproj`.
- **Web**: `dotnet run --project samples/Gallery.Browser/Gallery.Browser.csproj` (WebAssembly,
  нужен workload `wasm-tools`).
- **Android**: `dotnet build samples/Gallery.Android/Gallery.Android.csproj`,
  пакет `com.md3.gallery`.
- Проверено: desktop 1320x900, 800x800 (rail), 500x800 (modal drawer);
  Android phone (411 dp), tablet 720 dp (rail), 1080 dp (drawer);
  WebAssembly в браузере. Скриншоты — `docs/gallery/`, описание — `docs/gallery/README.md`.

### Исправленные баги

1. **`MdList.ShowDividers`** — элементы, добавленные после установки свойства,
   не получали dividers. Исправлено: подписка на `Children.CollectionChanged`
   и `SyncChildDividers()`; `ShowDividers=false` очищает управляемые dividers,
   явный `MdListItem.ShowDivider=true` сохраняется.
2. **Demo detail subtitle** — `Message 13 of 12` из-за захвата переменной цикла
   в замыкании. Исправлено per-iteration `index`.
3. **Filled text field на `Filled`-карточке** — сливался фон. Карточка формы
   переведена на `MdCardVariant.Outlined`.
4. **Snackbar + safe area** — нижний inset учитывался дважды (высота нижней панели
   уже включает padding). Исправлено.
5. **Snackbar без anchor** — не поднимался над нижней панелью. Исправлено через
   `SnackbarBottomOffset`.
6. **Двойной safe-area на Android** — `TopLevel.RenderScaling` был `1` при первом
   чтении insets, затем становился `2.625`, из-за чего верхний отступ вырастал
   примерно в 2.6 раза. Исправлено пересчётом по `ScalingChanged` и
   `DispatcherPriority.Loaded`.
7. **Android Back завершал Activity** — `Keycode.Back` маппится в `Key.Escape`,
   поэтому `KeyDown` закрывал overlay/страницу, а затем `BackRequested` видел root
   и завершал Activity. Исправлено: на Android Escape не обрабатывается в
   `KeyDown`, всё идёт через `TopLevel.BackRequested`.
8. **Overlay не работали на Android без Avalonia theme** — у
   `EmbeddableControlRoot` нет `VisualLayerManager`/`OverlayLayer`. Добавлен
   fallback overlay-host внутри `MdNavigationPage` и регистрация в `MdOverlay`.
9. **Дата/время pickers вылезали на маленьком экране** — добавлены адаптивные
   `MinWidth`/`MaxWidth`/`MaxHeight` и `MdScrollViewer` внутри карточки.
10. **Библиотека зависела от Fluent theme** — удалены `FluentTheme`, `Window` и
    `ScrollViewer`; добавлены `MdWindow` и `MdScrollViewer`.
11. **`MdPopover` прижимался к правому краю на узком экране** — `MdSurface` содержит
    `MdScrollViewer`, который просит всю доступную ширину, поэтому после layout карточка
    становилась шире измеренной и вылезала за край: `MaxWidth` не учитывал смещение `x`.
    Исправлено: ширина считается по `bodyStack` (шаблон `MdScrollViewer` появляется только
    после подключения к дереву, поэтому `card.Measure(...)` возвращал 0), затем
    `card.Width = desiredW` фиксируется, а высота ограничивается
    `overlaySize.Height - y - edgeMargin`. Регрессия закрыта тестами
    `Popover_keeps_its_edge_margin_when_anchored_to_the_right_edge` и
    `Menu_keeps_its_edge_margin_when_anchored_to_the_right_edge`.
12. **Клавиатурный ресайз `MdSearchView` был не дописан** (не собиралась библиотека:
    `CS0103 _refreshCardSize`). Исправлено: поле `_refreshCardSize`, вызов через него в
    `InputPane.StateChanged`, отписка и очистка в `DetachInputPane()` из `Close()`.
13. **`MdMenu` вылезал на 8 dp за правый край после resize** — клэмп использовал
    `MinWidth` (ширину якоря), а реальная карточка шире из-за длинных подписей.
    Исправлено: `PositionCard` измеряет строки через `MdScrollViewer.Content` и
    клампит карточку по фактической ширине; после первого layout вызывается
    `Dispatcher.Post(Reflow)`, чтобы пересчитать позицию с уже измеренной карточкой.
14. **`MD3_KEYBOARD` поднимал только страницу** — overlay-root не знал о
    симулированной клавиатуре. Исправлено: `SetSimulatedKeyboardHeight` в `MdOverlay`
    (вызывается из `MdKeyboardAvoidingHost`) и пересчёт всех overlay-root'ов.
15. **Серый статус-бар на Android 11 (реальное устройство)** — `edgetoedge`-запрос
    оставлял прозрачные бары, за которыми был wallpaper. Исправлено: edge-to-edge
    включается только на Android 15+ (`Build.VERSION.SdkInt >= 35`), на старых
    версиях бары окрашиваются в `Surface`.
16. **Пустой overlay-host перехватывал тапы** — fallback `OverlayRootGrid` был
    hit-test-видимым без открытых overlay. Исправлено: `IsHitTestVisible=false`,
    пока нет layer'ов; включается по `Children.CollectionChanged`.
17. **Системные бары не окрашивались при старте** — окно ещё не имело финальных
    атрибутов на `AttachedToVisualTree`. Исправлено: повторный `Apply` через
    `DispatcherPriority.Loaded`; regression-тест `OverlayHitTestTests`.
18. **`MdSnackbar` с `Indefinite` не закрывался** — у него не было таймера и
    close-иконки (в галерее «Queue snackbar» ставил в очередь вечный «Uploading…»).
    Исправлено: indefinite snackbar всегда показывает ×; галерея ставит в очередь
    конечные сообщения и получила кнопки Dismiss/Indefinite.
19. **Диалог смещён и scrim не доходил до низа после обхода галереи** — страница
    Layout создаёт `MdKeyboardAvoidingHost` с `SimulatedKeyboardHeight = 120`, и это
    значение навсегда оставалось в глобальном `MdOverlay`: overlay-root получал
    `Margin.Bottom = 120`, из-за чего его высота была 780 вместо 900. Scrim закрывал
    только верхние 780 px, а диалог центрировался в укороченной области. Исправлено:
    хост сообщает высоту только пока прикреплён к визуальному дереву (слабые ссылки,
    максимум по активным хостам); при уходе со страницы overlay возвращается к полной
    высоте. Регрессии — в `KeyboardTests`.

---

## 5. Окружение: Linux Mint (текущее)

Разработка и визуальная проверка идут на **Linux Mint, X11** (`DISPLAY=:0`),
рабочая папка `/home/homo/src/md3`. WSL и Windows больше не используются.

| Компонент | Значение |
|---|---|
| .NET SDK | `10.0.113`, runtime `10.0.13` (`/usr/lib/dotnet`) |
| Workload | `android` установлен **user-local** в `~/.dotnet` (manifest 36.1.118) |
| Android SDK | `ANDROID_HOME=/home/homo/Android/Sdk` |
| SDK-пакеты | `platforms/android-36` (поставлен через `sdkmanager`), `build-tools/36.0.0`, `platform-tools` |
| cmdline-tools | `~/Android/Sdk/cmdline-tools/latest/bin/sdkmanager` |
| JDK | OpenJDK 21 (`JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64`) |
| Эмулятор | AVD `Medium_Phone_API_37.0`, KVM доступен, запускается локально |
| GUI-автоматизация | `tools/linux/md3ui.py` (python-xlib XTEST + gnome-screenshot + Pillow) |

Запуск эмулятора и приложения:

```bash
~/Android/Sdk/emulator/emulator -avd Medium_Phone_API_37.0 -no-snapshot-load \
    -no-audio -no-boot-anim -gpu swiftshader_indirect &
adb wait-for-device shell 'while [ "$(getprop sys.boot_completed)" != 1 ]; do sleep 2; done'
adb install -r samples/Demo.Android/bin/Debug/net10.0-android/com.md3.demo-Signed.apk
adb shell am start -n com.md3.demo/crc64351746bbe631ada1.MainActivity
adb exec-out screencap -p > docs/handoff/android-home.png
adb emu kill
```

### Legacy: WSLg и нативные Windows-скриншоты

WSLg-автоматизация была ненадёжной (`xdotool` считает координаты от внешней рамки,
кадры устаревали, hit-testing не совпадал со скриншотом), поэтому визуальные выводы
делались на нативном Windows. Файлы `docs/handoff/wslg-*.png` — мусор, не доверять.
Файлы `docs/handoff/win-*.png` — историческая проверка на Windows (DPI 120,
окно `1477x1039`); они остаются как справка по этапам 2–4.5, но новые проверки
снимаются на Linux (`linux-*.png`) и Android.

Важно при съёмке на Linux: `gnome-screenshot -w` включает titlebar, поэтому координаты
и кадры расходятся. `tools/linux/md3ui.py` снимает весь экран и обрезает его по
**клиентской** области окна (`XTranslateCoordinates`), поэтому координаты клика и
пиксели скриншота совпадают 1:1.

### Legacy: окружение Windows

Оставлено на случай возврата к Windows-проверке:

- `C:\Program Files\dotnet\dotnet.exe` — .NET SDK `10.0.402`;
- Android SDK: `C:\Users\homo\AppData\Local\Android\Sdk`;
- `adb.exe`: `...\platform-tools\adb.exe`;
- эмулятор: `...\emulator\emulator.exe`.

В WSL `powershell.exe`/`cmd.exe` не в PATH; вызывать по полному пути:

```bash
/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe
/mnt/c/Program Files/dotnet/dotnet.exe
```

---

## 6. GUI-автоматизация

### 6.1 `tools/linux/md3ui.py` — основной инструмент

Запуск, скриншоты, клики, клавиши, ввод текста, прокрутка и информация об окне.
Координаты — **относительно клиентской области окна**, ровно те же пиксели, что и на
скриншоте: помощник снимает весь экран и обрезает его по клиентской области.

```bash
python3 tools/linux/md3ui.py start --no-build
python3 tools/linux/md3ui.py shot  --out docs/handoff/linux-home.png
python3 tools/linux/md3ui.py click --x 779 --y 285
python3 tools/linux/md3ui.py key   --keys Escape
python3 tools/linux/md3ui.py type  --text "hello"
python3 tools/linux/md3ui.py wheel --x 400 --y 300 --delta -600
python3 tools/linux/md3ui.py info
python3 tools/linux/md3ui.py stop
```

`--delta` отрицательный — вниз, положительный — вверх. `shot --frame` включает рамку
окна. `click --repeat N` и `--settle` помогают попасть в анимацию.

Resize открытого окна (проверка пересчёта overlay) делается через `wmctrl`, id окна —
`wmctrl -l | grep "Md3 Navigation Demo"`:

```bash
wmctrl -i -r 0x0520000f -e 0,900,300,720,520   # x,y,w,h в пикселях экрана
python3 tools/linux/md3ui.py shot --out /tmp/popover-after-resize.png
```

Минимальный размер окна демо — 720x520, поэтому для более узкого вьюпорта запускайте
демо сразу с `--window 320x640` (или `400x800`).

Зависимости (уже стоят): `python3-xlib`, `Pillow`, `gnome-screenshot`, `wmctrl`,
`x11-utils`. `xdotool` не нужен.

Параметры мобильного превью:

```bash
python3 tools/linux/md3ui.py start --window 400x800 --safe-area 24,48 --keyboard 300
python3 tools/linux/md3ui.py start --window 640x360 --safe-area 48,0,0,24   # landscape
```

`--window` → `MD3_WINDOW` (dp, `ШxВ`, минимум `320x320`), `--safe-area` →
`MD3_SAFE_AREA`, `--keyboard` → `MD3_KEYBOARD` (`0` отключает симуляцию IME).

Перед пересборкой запущенное демо лучше остановить (`stop`): Linux не блокирует
`Md3.Avalonia.dll`, но окно останется на экране и запутает скриншоты.

### 6.2 `tools/win/md3ui.ps1` — legacy

Windows-помощник с тем же набором действий, координаты — от клиентской области
(физические пиксели). Нужен только при работе на Windows:

```powershell
powershell -File tools\win\md3ui.ps1 -Action start -EnvWindow "400x800" -EnvSafeArea "24,48"
powershell -File tools\win\md3ui.ps1 -Action click -X 335 -Y 393
powershell -File tools\win\md3ui.ps1 -Action key   -Keys "{ESC}"
powershell -File tools\win\md3ui.ps1 -Action stop
```

Важно: перед пересборкой остановить процесс, иначе Windows держит `Md3.Avalonia.dll`
и сборка падает с `MSB3021`.

---

## 7. Следующие задачи по приоритету

Этапы 0–12 и оптимизации 1–7 закрыты; обязательных задач нет. Остались только
необязательные проверки и решения:

1. **Release trimmed/AOT APK** демо и галереи — пересобрать после snackbar-правок и
   оптимизаций, установить и прогнать smoke:
   ```bash
   dotnet publish samples/Demo.Android/Demo.Android.csproj -c Release \
       -p:PublishTrimmed=true -p:RunAOTCompilation=true
   dotnet publish samples/Gallery.Android/Gallery.Android.csproj -c Release \
       -p:PublishTrimmed=true -p:RunAOTCompilation=true
   ```
   Debug-сборка `Demo.Android` после оптимизаций уже проходит (`0/0`).
2. **Нативная проверка новых оптимизаций** — Android (AVD/устройство) и WASM-галерея:
   инкрементальные `MdList`/`MdDataTable` и закрытие top-level с открытым dismissable.
   Сейчас покрыто headless-тестами (233/233), desktop-demo и smoke-тестом.
3. **RTL** — сознательно отложен владельцем (этап 10 закрыт без него); нужен отдельный
   продуктовый этап, если появится требование.
4. **NuGet-публикация** — по политике проекта исключена из плана.

Android 15 (SDK 35) проверен на реальном устройстве: галерея, edge-to-edge, все три
modal-поверхности и snackbar работают. Отдельных Android-задач, кроме перечисленных
опциональных проверок, не осталось.

---

## 8. Важные ограничения и правила

- Отвечать пользователю на языке его последнего сообщения (последние сессии — **русский**;
  исторические заметки ниже тоже по-русски).
- Не создавать ветку и не делать коммиты.
- Рабочая среда — **Linux Mint, X11**; GUI-проверки через `tools/linux/md3ui.py`,
  Android — на локальном AVD через `adb`.
- Не использовать `Avalonia.Controls.NavigationPage`/`ContentPage` для Md3-экранов.
- Не добавлять `[Obsolete]`-совместимость.
- Не заменять `MdNavigationPage.Content` снаружи: он сам строит внутренний layout.
- После изменения `Title`, `Subtitle`, actions, `BottomBar` или
  `FloatingActionButton` у видимой страницы вызывать `RefreshChrome()`.
- `ItemsSource` владеет дочерними элементами; не смешивать с ручными `Children`.
- Overlay-контролы — handles, их не надо добавлять в `StackPanel`.
- `MdSnackbar` и `MdTooltip` глобальные.
- `MdSwitch` использует `IsToggled`, не `IsOn`.
- `MdIconButton` по умолчанию имеет touch-цель 48 dp (`TouchTargetSize`).
- `MdTextField` умеет `ContentType`, `ReturnKeyType`, `AutoCapitalize`.
- `ApplySafeArea=false` отключает авто-insets; `SafeAreaOverride=null` — реальные insets.

---

## 9. Быстрая карта файлов

| Путь | Назначение |
|---|---|
| `Md3.sln` | solution: библиотека, тесты и все sample-хосты |
| `Md3.Avalonia/Controls/MdContentPage.cs` | модель страницы, actions, bottom bar, FAB, lifecycle |
| `Md3.Avalonia/Controls/MdNavigationPage.cs` | стек, chrome, back, transitions, safe area, keyboard host, overlay host |
| `Md3.Avalonia/Controls/MdWindow.cs` | окно с собственным шаблоном и `VisualLayerManager` |
| `Md3.Avalonia/Controls/MdScrollViewer.cs` | scroll без Avalonia theme |
| `Md3.Avalonia/Controls/MdKeyboardAvoidingHost.cs` | soft keyboard inset + auto-scroll |
| `Md3.Avalonia/Controls/MdSnackbar.cs` | глобальный snackbar + bottom offset |
| `Md3.Avalonia/Controls/MdList.cs` | список и синхронизация dividers |
| `Md3.Avalonia/Controls/MdButton.cs` | buttons, icon buttons (touch target), FAB |
| `Md3.Avalonia/Controls/MdTextField.cs` | поле + `TextInputOptions` |
| `Md3.Avalonia/Controls/MdInputsExtra.cs` | `MdNumberBox`, `MdTagInput` и др. |
| `Md3.Avalonia/Platform/MdOverlay.cs` | overlay host, dismiss stack, owner page |
| `Md3.Avalonia/Theming/MdMotion.cs` | анимации и transitions |
| `samples/Demo/MainWindow.cs` | окно, smoke-тест, `MD3_SAFE_AREA`, `MD3_KEYBOARD` |
| `samples/Demo/Pages.cs` | Home/Controls/Inbox/Detail |
| `samples/Demo.Android/` | Android host, `MainActivity`, `MobileApp` |
| `samples/Gallery/` | адаптивная галерея контролов (desktop + shared shell/pages) |
| `samples/Gallery.Browser/` | WebAssembly host (`Avalonia.Browser`) |
| `samples/Gallery.Android/` | Android host галереи, `com.md3.gallery` |
| `docs/gallery/` | скриншоты галереи (desktop/tablet/phone/browser) |
| `Md3.Avalonia/Platform/MdGestureZones.cs` | 20 dp edge-зона системного Back |
| `Md3.Avalonia/Platform/MdAutomation.cs` | AutomationProperties helper |
| `Md3.Avalonia/Platform/MdSystemBars.cs` | Android system bars + edge-to-edge |
| `Md3.Avalonia/Platform/MdPropertyCache.cs` | кэш PropertyInfo (trim/AOT) |
| `Md3.Avalonia/Theming/MdAccessibility.cs` | WCAG contrast helpers |
| `Md3.Avalonia/Controls/MdDataTable.cs` | таблица + compact/card mode |
| `Tests/` | headless-тесты (233) |
| `CHANGELOG.md` | версии и изменения (SemVer, не привязан к этапам плана) |
| `tools/linux/md3ui.py` | Linux/X11 запуск/скриншоты/клики/клавиши/скролл (основной) |
| `tools/win/md3ui.ps1` | то же для Windows (legacy) |
| `docs/` | README, `AGENTS.md`, `HANDOFF.md`, `PLAN.md`, gallery/handoff screenshots |

---

## 10. Первое сообщение новой сессии

> Продолжаем проект Md3.Avalonia. Прочитай `docs/HANDOFF.md` (особенно §1a), `docs/PLAN.md` и `docs/AGENTS.md`.
> Работаем из `/home/homo/src/md3` на Linux Mint (X11), GUI-проверки — через
> `tools/linux/md3ui.py`, Android — через `adb` (AVD или подключённый телефон). Сначала собери
> библиотеку, демо и галерею, прогони тесты (сейчас 233/233). Все плановые этапы 0–12 закрыты;
> NuGet в план не входит. Android 15 (SDK 35) уже проверен на реальном устройстве, включая
> side sheet и snackbar.
