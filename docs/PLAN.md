# Mav — план разработки

Обновлено: 22 октября 2026  
Статус: этапы 0–12 закрыты полностью (10 — без RTL); desktop (Linux Mint/X11), AVD API 37 и реальное устройство (Android 11, затем Android 15 после обновления прошивки) проверены; библиотека работает без Avalonia control themes, 233 headless-теста, 91 строка каталога контролов (119 публичных Md-классов). NuGet-публикация в план не входит.  
Текущий этап: **все этапы 0–12 закрыты**; раунд 2 оптимизаций также закрыт
полностью, включая финальную проверку (`docs/OPTIMIZATION_PLAN_2.md`).  
Ветка: не создаётся

> Контекст для продолжения: см. `docs/HANDOFF.md`. Рабочая среда — Linux Mint (X11),
> `/home/homo/src/md3`; GUI-проверки — `tools/linux/mavui.py`, Android — локальный
> AVD через `adb`. Старые WSLg-снимки недостоверны; `win-*.png` — историческая
> проверка на Windows (этапы 2–4.5). Этапы 5–12 — рекомендация по приоритету;
> обоснование и порядок — в §4.

## 0. Главное решение

Библиотека **не использует** `Avalonia.Controls.NavigationPage` и
`Avalonia.Controls.ContentPage`.

Вместо них строится собственная навигация на базе Material-контролов:

- `MavContentPage` — страница: заголовок, действия, контент, нижняя панель, FAB.
- `MavNavigationPage` — стек страниц, app bar, back-кнопка, переходы.
- Вся оболочка рисуется контролами Mav, поэтому доступны `MavTopAppBar`,
  `MavBottomAppBar`, `MavFab`, `MavSnackbar`, `MavDialog`, `MavBottomSheet` и т. д.
- Навигация должна работать на desktop сейчас и на мобильных позже.

## 1. Принципы

- [x] Никакого `[Obsolete]`-слоя: библиотека ещё не используется.
- [x] Сначала собственный page-стек, потом мобильная адаптация.
- [x] Desktop-демо — первая площадка для проверки.
- [x] Один публичный API для desktop и mobile, без отдельных «mobile-контролов».
- [x] Все элементы навигационной оболочки — Material Design 3.
- [x] Код библиотеки остаётся pure C#, без AXAML-шаблонов.
- [x] Библиотека не зависит от Avalonia control themes: собственные `MavWindow`,
  `MavScrollViewer` и fallback overlay-host.
- [x] Визуальные выводы делаются только по нативным скриншотам (Linux Mint/X11 и Android).
- [x] Каждое поведение закрывается либо headless-тестом, либо нативной проверкой
  (сейчас 233 headless-теста; см. этапы 9–12 и `docs/OPTIMIZATION_PLAN_2.md`).

## 2. Выполненные этапы (0–4.5)

Историю, детали проверок и исправленные баги см. в `docs/HANDOFF.md` §3–§4;
здесь — только состав работ.

### Этап 0. Анализ и фиксация подхода — готово

- [x] Разбор Avalonia 12 `Page`/`NavigationPage`/`ContentPage`; отказ от встроенных
  page-классов; выбор `MavContentPage` + `MavNavigationPage`.

### Этап 1. Базовая собственная навигация — готово

- [x] `MavContentPage`: заголовок/подзаголовок, ведущая иконка, actions, `BottomBar`,
  `FloatingActionButton`, `Appearing`/`Disappearing`, `OnBackPressed`, `Navigation`.
- [x] `MavNavigationPage`: `SetRoot`/`Push`/`Pop`/`PopToRoot`/`Replace`, `NavigationStack`,
  `CurrentPage`, `CanGoBack`, `PageChanged`, back-иконка, обновление chrome, `MavMotion`.
- [x] Сборка библиотеки и демо, smoke push/pop/pop-to-root.

### Этап 2. Desktop-демо — готово, проверено нативно на Windows

- [x] `samples/Demo/Mav.Demo.csproj`, `UseMav`, home/controls/inbox/detail страницы, overlay-страница.
- [x] Smoke-режим `MAV_SMOKE=1`; нативные скриншоты всех элементов (`docs/handoff/win-*.png`).

### Этап 3. Доработка навигации — готово

- [x] Back по `Escape`/`Alt+Left`, закрытие overlay перед pop, автозакрытие overlay при
  уходе со страницы, сохранение состояния, защита от повторного push, отмена анимации.
- [x] `MavNavigationTransition` (`None`/`Slide`/`Fade`), `GoBackCommand`/`CanGoBack`,
  `OnBackPressed`, тесты стека.

### Этап 4. Мобильная адаптация — готово (проверено на AVD Pixel 7, API 36)

- [x] Safe area через `IInsetsManager` (`ApplySafeArea`, `SafeAreaOverride`, `SafeAreaInsets`).
- [x] Клавиатура через `MavKeyboardAvoidingHost` (встроен в `MavNavigationPage`).
- [x] `TextInputOptions` для `MavTextField`/`MavTextArea`/`MavSearchBar`/`MavPinInput`/
  `MavNumberBox`/`MavComboBox`/`MavTagInput`.
- [x] Touch-цели ≥ 48 dp; адаптивные `MavDialog`, `MavDatePicker`, `MavTimePicker`;
  ограничение ширины/высоты у `MavMenu`/`MavPopover`; full-screen `MavSearchView`.
- [x] Узкие экраны и landscape: `MAV_WINDOW=320x640`, `360x640`, `640x360` +
  `MAV_SAFE_AREA=48,0,0,24` (скриншоты `win-320-*`, `win-360-*`, `win-land-*`);
  попутно исправлен клэмп `MavPopover`.
- [x] Snackbar над нижней панелью/FAB/safe inset; системный Back на Android.

### Этап 4.5. Независимость от Avalonia control themes — готово

- [x] Убрать `Avalonia.Themes.Fluent`; добавить `MavWindow`, `MavScrollViewer`,
  fallback overlay-host для Android `EmbeddableControlRoot`.
- [x] Разделить Android Back (`BackRequested` вместо `Escape` в `KeyDown`).

## 3. Рекомендуемые этапы (5–12)

### Этап 5. Устойчивость overlay и адаптивность — готово

Решено централизованно: `MavOverlay.Show(..., onViewportChanged)` хранит callback в
`OverlayHandle`, а `OverlayRootGrid` вызывает его после resize окна, `ScalingChanged`,
`InsetsManager.SafeAreaChanged`, `IInputPane.StateChanged` и собственного `SizeChanged`.
Уведомления коалесцируются через `Dispatcher.UIThread.Post(..., Loaded)`. Каждый overlay
пересчитывает только свою геометрию («тихо», без повторного `ScaleIn` и без сброса
выбранного значения); `MavOverlay.SafeAreaOf/SafeAreaBounds` дают insets, `BoundsInOverlay` — координаты
якоря, `MavOverlay.SetSimulatedKeyboardHeight` поднимает overlay в desktop-превью
(`MAV_KEYBOARD`).

- [x] Пересчёт геометрии открытых overlay: `MavPopover`, `MavMenu` (+ сабменю),
  `MavSearchView`, `MavDialog`, `MavTimePicker`, `MavDatePicker`, `MavColorPicker` (через
  `MavDialog`), `MavBottomSheet`, `MavSideSheet`, modal drawer, `MavTooltip`,
  `MavRichTooltip`, `MavComboBox` dropdown, `MavSnackbar`.
- [x] `MavPopover`: ширина и `MaxHeight` пересчитываются при каждом изменении вьюпорта,
  поэтому карточка больше не «залипает» на ширине первого открытия.
- [x] Safe area внутри overlay: insets учитываются при открытии и при их смене
  (вырез, статус-бар, жестовая полоса) — у popover/menu/tooltip/snackbar/диалогов/pickers.
- [x] Soft keyboard и overlay: `IInputPane` и `MAV_KEYBOARD` поднимают overlay-root,
  открытые popover/menu/search view пересчитываются и остаются над IME.
- [x] `MavSearchView`: карточка пересчитывается и по resize, и по клавиатуре, и по insets;
  full-screen вариант использует `MavSafeArea`.
- [x] Headless-тесты: открытый `MavMenu`/`MavPopover`/`MavDialog`/time/date picker после
  resize окна остаётся внутри вьюпорта (menu 8 dp, popover 12 dp, dialog 24 dp);
  popover растягивается обратно при увеличении окна; popover уходит вверх при
  симулированной клавиатуре.

**Критерии:** при resize/повороте открытый overlay остаётся целым и внутри границ,
выбор сохраняется; `dotnet test` — 31/31; нативно проверено на Linux
(`320x640`, `400x800` + safe area, `640x360` + inset, `--keyboard 300`).

### Этап 6. Android polish и реальное устройство — P0

- [x] Edge-to-edge (Android 15+): согласован `Resources/values/styles.xml`
  (transparent bars) с insets из `MavNavigationPage`; убраны двойные отступы.
- [x] Цвета system bars и иконок: синхронизированы с `MavTheme.Current`
  (`statusBarColor`/`navigationBarColor`, `windowLightStatusBar`,
  `windowLightNavigationBar`) при light/dark и смене seed-цвета.
- [x] Поворот экрана: chrome, safe area и открытые overlay (зависит от этапа 5);
  `Locale`/`LayoutDirection` добавлены в `ConfigurationChanges` `MainActivity.cs`.
- [x] Проверка на AVD `Medium_Phone_API_37.0` (Linux/KVM): safe area, системный Back,
  IME, тёмная тема, свайпы.
- [x] Проверка на реальном устройстве Xiaomi Redmi Note 4 (Android 11, SDK 30):
  установка/запуск, safe area, light/dark, системные бары, поворот, Back,
  стек страниц, `MavList`, snackbar, `MavFabMenu` overlay; touch подтверждён вручную.
- [x] Исправления по итогам реального устройства: edge-to-edge только на Android 15+,
  пустой fallback overlay-host не перехватывает ввод, отложенное применение баров.
- [x] Скриншоты `docs/handoff/android-*.png` (AVD и `android-real-*.png`).

**Критерии:** на устройстве нет двойных отступов, бары совпадают с темой, поворот не ломает
стек и overlay, жесты работают.

### Этап 7. Back-жесты и жестовые конфликты — P1

- [x] Свайп-назад от края (predictive back): `TopLevel.BackRequested` обрабатывается
  `MavNavigationPage`/`MavOverlay`; проверено на AVD и реальном устройстве
  (overlay → страница → Activity). На Android `Escape` в `KeyDown` намеренно не
  обрабатывается.
- [x] Конфликты с системным свайпом: `MavListItem` swipe actions, `MavCarousel`,
  `MavSlider`/`MavRangeSlider`, `MavSplitView`, `MavColorPicker`, `MavTimeDial`,
  `MavRadialGauge`. `MavRefreshIndicator` не конфликтует (вертикальный pull) и уже
  игнорирует press на draggable-потомках.
- [x] Политика: `MavGestureZones` (20 dp от левого/правого края TopLevel) — при touch-
  press в этой зоне контрол не начинает горизонтальный drag и не захватывает pointer,
  жест уходит системе. Mouse не ограничивается (desktop).
- [x] Headless-тесты: `GestureZoneTests` — определение edge-зоны, кастомная ширина,
  slider и carousel игнорируют touch из зоны и работают вне её.

**Критерии:** свайп-назад закрывает overlay → страницу; свайп по элементу списка не
закрывает страницу; слайдеры не «залипают» при жесте от края.

### Этап 8. Виртуализация и производительность — P1

- [x] Виртуализация `MavList`: при `ItemsSource` в `ScrollViewer` окно реализуемых элементов
  вычисляется по вьюпорту (±480 dp overscan); невидимые элементы не создаются, сверху/снизу
  стоят spacer-ы, высоты запоминаются после `Arrange`. Публичный API (`AddItem`,
  `ItemsSource`, `ItemTemplate`, `ItemFactory`, `ShowDividers`, `DividerInsetStart`)
  сохранён; ручной режим без `ItemsSource` по-прежнему держит все `Children`.
  Малые списки (≤60) и списки без `ScrollViewer` реализуются целиком.
- [x] `MavDataTable`: инкрементальная отрисовка строк чанками по 100 при скролле тела
  (`MaxBodyHeight`); пагинация не менялась; при отсутствии внутреннего viewport страница
  отрисовывается целиком.
- [x] Compact/card-режим `MavDataTable`: `Layout` (`Auto`/`Table`/`Cards`),
  `CompactBreakpoint` (600 dp), `IsCompact`/`EffectiveLayout`. На узком хосте строка
  становится `MavCard` (headline + label/value), сортировка доступна чипами,
  выбор — чекбоксом; при пересечении breakpoint layout пересобирается.
- [x] Убран reflection из горячих путей (`MavDataTable.cs`, `MavSurface.cs`) → `MavPropertyCache`
  (`ConcurrentDictionary` с лимитом 1024).
- [x] Кэш `MavTextMetrics.MeasureWidth` (лимит 2048) — убраны повторные `new FormattedText`.
- [x] Замеры (headless, viewport 300 dp): `MavList` 1000 элементов → 14 реализованных,
  первая раскладка ~29 мс; `MavDataTable` 1000 строк → 100 реализованных, ~25 мс.

**Критерии:** список на 1000+ элементов открывается и скроллится без пересоздания всех
детей; в горячем пути нет reflection; кэши ограничены и не течут.

### Этап 9. Тестовое покрытие контролов — P1, сквозной

- [x] 173 теста: `NavigationTests`, `ListTests`, `KeyboardTests` + новые группы —
  `ButtonTests` (buttons/FAB/icon-button/button-group/split), `InputTests` (text/pin/
  number/tag/rating), `SelectionTests` (switch/checkbox/radio/slider/segmented/chips/
  select/combobox), `PickerTests` (date/time/color), `FeedbackTests` (snackbar/progress/
  skeleton/info bar), `ChromeTests` (app bar/bottom bar/drawer/tabs/stepper/pagination),
  `ListTableTreeTests` (list/table/tree/carousel), `ContainerTests` (card/avatar/badge/
  divider/label/safe area), `ValidatorTests` (`MavFormValidator` rules: Required,
  MinLength/MaxLength, Email, Numeric, Pattern, Matches, Must, ClearErrors; attached
  `MavValidation` + `ValidateTree`), `ControlSmokeTests` (создание и раскладка всех групп
  контролов из `docs/AGENTS.md` §5, включая `MavWindow` как TopLevel).
- [x] Smoke-тест «создать и разложить каждый контрол из `docs/AGENTS.md` §5» — ловит падения
  шаблонов и null-референсы раскладки.
- [x] Правило: каждый исправленный баг получает регрессионный тест
  (пример: `Popover_keeps_its_edge_margin_when_anchored_to_the_right_edge`).

**Критерии:** ≥1 тест на каждый публичный контрол (или явное исключение), прогон < 10 с.
Прогон: 173/173 за ~1 с.

### Этап 10. Accessibility (без RTL) — P2

- [x] `MavAutomation` (`MavUI/Platform/MavAutomation.cs`): единый способ выставлять
  `AutomationProperties.Name`/`HelpText`/`IsRequiredForForm`/`ControlTypeOverride`,
  не перезаписывая явно заданные приложением значения.
- [x] Автоматические имена и роли: `MavButton` и `MavExtendedFab` — текст, `MavIconButton`/`MavFab` —
  tooltip, `MavTextField`/`MavTextArea`/`MavSelect`/`MavComboBox`/`MavNumberBox` — label и
  footer (ошибка > supporting text), `MavCheckBox`/`MavRadioButton` — label,
  `MavSwitch` — `CheckBox`, `MavSlider`/`MavRangeSlider` — `Slider`, `MavChip`/`MavMenuItem` —
  текст, `MavListItem` — headline + supporting text, `MavDialog`/`MavBottomSheet` — title/message.
- [x] `MavValidation.IsRequired` синхронизируется с `AutomationProperties.IsRequiredForForm`.
- [x] Клавиатура: Space/Enter для `MavButton`/`MavIconButton`/`MavFab`/`MavChip`/`MavListItem`
  (через `MavInkSurface`), Space/Enter для `MavSwitch`/`MavCheckBox`/`MavRadioButton`,
  стрелки для `MavSlider`/`MavRangeSlider`; видимый focus-ring уже рисует `MavInkSurface`.
- [x] Контраст: `MavAccessibility` (`RelativeLuminance`, `ContrastRatio`, `MeetsContrast`) +
  `ContrastTests` для `MavContrast.Standard/Medium/High` в light/dark.
- [x] RTL — сознательно не входит в этот этап (по решению владельца проекта).

**Критерии:** скринридер читает основные контролы; контраст-тест зелёный.
RTL-страница не требуется.

### Этап 11. Размер приложения, trimming и AOT — P2

- [x] Шрифты: MSBuild-свойство `MavFonts` (`all` по умолчанию, `no-mdi`, `minimal`)
  выбирает встраиваемые наборы. Размер `MavUI.dll`: all 4.87 MB, no-mdi 3.69 MB,
  minimal 3.35 MB. `minimal` (Roboto + Material Symbols) подходит только для приложений,
  которые не используют классические `MavIcons`; для стандартного демо безопасен `no-mdi`.
- [x] `IsTrimmable`/`IsAotCompatible` включены; устранены IL2070/IL2072/IL2075:
  `MavPropertyCache` аннотирован `DynamicallyAccessedMembers`, call sites
  (`FormatItemText`, `ResolveByPropertyName`) и `MavSystemBars.ApplyIconTheme` —
  с обоснованными `UnconditionalSuppressMessage`. В hot path reflection не осталось.
- [x] Android: `dotnet publish -c Release -p:PublishTrimmed=true` (в Release
  `RunAOTCompilation` включён по умолчанию) — APK 29.08 MB со всеми шрифтами,
  27.71 MB с `no-mdi`; baseline без trim/AOT — 65.60 MB (−58%). Trimmed+AOT APK
  установлен на реальное устройство, рисует главную и overlay без ошибок.
- Замечание: `DisplayMemberPath`/`MavDataColumn.PropertyName` в trimmed-приложении
  требуют rooting свойств модели; предпочтительны `ItemTemplate`/`ValueSelector`.

**Критерии:** размер артефакта меньше; trimmed/AOT-сборка проходит smoke-тест.
Выполнено: 27.71 MB против 65.60 MB, проверено на Redmi Note 4 (Android 11).

### Этап 12. API-стабильность и документация — готово

- [x] Сверен `docs/AGENTS.md` §5 с публичным API (91 строка каталога, 119 публичных
  Md-классов): у `MavColorPicker` убраны непубличные `Hue`/`HueSelected`, остальные записи совпадают.
- [x] Реализованы оба известных no-op: `MavCarousel.Layout` (`Hero`/`CenterAligned`/
  `MultiBrowse`/`Uncontained`) и `MavSplitView.InitialRatio` (доля ширины при первой
  раскладке; явный `PaneWidth` имеет приоритет). Добавлены примеры в §5.5.
- [x] Политика версий и `CHANGELOG.md`: версия следует SemVer и не привязана к номерам
  этапов; `0.1.0` — первый полный preview. NuGet-публикация сознательно исключена.
- [x] Синхронизированы `docs/AGENTS.md`, `docs/HANDOFF.md`, `docs/PLAN.md`.

**Критерии:** каталог контролов и публичный API совпадают 1:1; no-op отсутствуют;
тесты покрывают новые варианты раскладки (тогда 182/182; сейчас 233/233).

## 4. Порядок и зависимости

| Этап | Приоритет | Зависит от | Размер | Зачем сейчас |
|---|---|---|---|---|
| 5. Overlay: resize/rotation/safe area | P0 | — | M | Прямое продолжение проверки landscape: overlay сейчас не переживает resize |
| 6. Android polish + реальное устройство | P0 | 5 | M | Edge-to-edge и поворот — то, что видит пользователь на телефоне |
| 7. Back-жесты и конфликты | P1 | 5, 6 | M | Без этого свайпы конфликтуют с системным back |
| 8. Виртуализация и производительность | P1 | — | L | Списки без виртуализации не годятся для реальных данных |
| 9. Тестовое покрытие контролов | P1 | сквозной | M | Страховка для всех остальных этапов |
| 10. Accessibility и RTL | P2 | 6 | L | Требование «работает на телефоне» включает a11y и локали |
| 11. Размер, trimming, AOT | P2 | 8 | M | Размер APK и корректность после trim/AOT |
| 12. API и документация | P2 | 10, 11 | M | Стабильный публичный API и документация |

Рекомендуемый первый шаг: **5** (пересчёт overlay при resize → safe area → клавиатура),
затем **6** и **7** в одной сессии на реальном устройстве, параллельно — **9**
(тесты добавляются к каждому изменению).

## 5. Команды

Всё выполняется на Linux Mint (X11) из `/home/homo/src/md3`:

```bash
dotnet build MavUI/MavUI.csproj
dotnet build samples/Demo/Mav.Demo.csproj
dotnet test Tests/MavUI.Tests.csproj
dotnet run --project samples/Demo/Mav.Demo.csproj
MAV_SMOKE=1 dotnet run --project samples/Demo/Mav.Demo.csproj
MAV_SAFE_AREA=24,48 MAV_KEYBOARD=300 dotnet run --project samples/Demo/Mav.Demo.csproj
MAV_WINDOW=400x800 MAV_SAFE_AREA=24,48 dotnet run --project samples/Demo/Mav.Demo.csproj
MAV_WINDOW=320x640 dotnet run --project samples/Demo/Mav.Demo.csproj
MAV_WINDOW=640x360 MAV_SAFE_AREA=48,0,0,24 dotnet run --project samples/Demo/Mav.Demo.csproj
```

GUI-проверка и скриншоты (координаты — клиентская область окна):

```bash
python3 tools/linux/mavui.py start --window 640x360 --safe-area 48,0,0,24
python3 tools/linux/mavui.py shot  --out docs/handoff/linux-land-home.png
python3 tools/linux/mavui.py click --x 335 --y 285
python3 tools/linux/mavui.py key   --keys Escape
python3 tools/linux/mavui.py wheel --x 400 --y 300 --delta -600
python3 tools/linux/mavui.py stop
```

Android (workload `android` + `platforms/android-36`, локальный эмулятор через KVM):

```bash
dotnet build samples/Demo.Android/Demo.Android.csproj
~/Android/Sdk/emulator/emulator -avd Medium_Phone_API_37.0 -no-snapshot-load \
    -no-audio -no-boot-anim -gpu swiftshader_indirect &
adb install -r samples/Demo.Android/bin/Debug/net10.0-android/com.md3.demo-Signed.apk
adb shell am start -n com.md3.demo/crc64351746bbe631ada1.MainActivity
adb exec-out screencap -p > docs/handoff/android-home.png
adb emu kill
# этап 11: trimmed/AOT публикация
dotnet publish samples/Demo.Android/Demo.Android.csproj -c Release \
    -p:PublishTrimmed=true -p:RunAOTCompilation=true
```

## 6. Критерии готовности этапа 5

Закрыты; регрессии — `NavigationTests`, `OverlayHitTestTests`, `KeyboardTests`.
Итоговый прогон на момент этапа 12: **209/209**; после раунда 2 оптимизаций — **233/233**.
Библиотека/демо/Android — `0 Warning(s) 0 Error(s)`.

- [x] Открытый overlay пересчитывает позицию и размер при resize окна и повороте.
- [x] `MavPopover` не «залипает» на ширине, посчитанной при открытии.
- [x] Safe area внутри overlay учитывается на всех сторонах (вырез, жестовая полоса).
- [x] Поле внутри overlay доступно при открытой мягкой клавиатуре.
- [x] Headless-тесты на resize overlay зелёные; полный прогон 209/209 без падений
  (после раунда 2 — 233/233).
- [x] Нативные проверки: `MAV_WINDOW=640x360` + resize вручную, `MAV_SAFE_AREA=48,0,0,24`,
  `MAV_KEYBOARD=300`; скриншоты в `docs/handoff/`.
- [x] Библиотека, демо и Android-хост собираются с `0 Warning(s) 0 Error(s)`.
