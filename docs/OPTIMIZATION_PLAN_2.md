# Md3.Avalonia — план оптимизаций, раунд 2

Обновлено: 22 октября 2026  
Статус: все этапы 1–8 закрыты.  
Ограничения: без новых NuGet-пакетов, без BenchmarkDotNet, без symbols/pack,
без AXAML-шаблонов и без `[Obsolete]`-слоя.

## Легенда

- `[x]` — сделано и проверено тестами.
- `[ ]` — запланировано.
- `[~]` — в работе.

## Контекст

Первый раунд (`docs/OPTIMIZATION_PLAN.md`) закрыл кэши геометрии, инкрементальные
`MdList`/`MdDataTable`, lifecycle `MdOverlay`, `IDisposable` у `MdThemeService` и
инвалидацию текстовых кэшей. Повторный аудит нашёл второй слой проблем:

- статические handles `MdSnackbar`/`MdTooltip` остаются живыми после внешнего
  закрытия overlay;
- 13 контролов хранят `_wired*`-множества и не отписываются от удалённых элементов;
- `MdTopAppBar` не отписывается от внешнего `ScrollViewer`, а
  `MdKeyboardAvoidingHost` держит сильную ссылку на последний focused-контрол;
- массовые изменения `MdList`/`MdDataTable`/`MdTreeView` всё ещё имеют квадратичные
  и кубические участки;
- `MdTextField`/`MdTextArea` выполняют лишние `ApplyTheme` на каждый keystroke.

Приоритет — сначала утечки и корректность lifecycle, затем горячие пути.

## Этап 1. Overlay lifecycle и статические handles

Цель: любой overlay, закрытый извне (top-level, `DismissForPage`, прямой
`Dispose`), должен освобождать ссылки на host, content, callbacks и owner, а
владельцы — очищать свои статические/долгоживущие поля.

- [x] `MdOverlay.Show` принимает optional `onClosed` callback; публичный возврат
  остаётся `IDisposable`; `ShowAnchoredPopup` пробрасывает callback.
- [x] `OverlayHandle` переведён на изменяемые поля; в `Dispose`:
  - снять layer с host и очистить дочерние элементы;
  - обнулить `host`, `layer`, `onDismiss`, `onViewportChanged`;
  - вызвать `onClosed` ровно один раз.
- [x] `DisposeOverlaysForTopLevel` и `DismissForPage` обходят снимок handles, чтобы
  callback мог менять статические списки.
- [x] `MdSnackbar`: `onClosed` очищает `_activeHandle`, таймер и очередь, если
  закрытие пришло извне; `Dismiss` обнуляет handle до `Dispose`.
- [x] `MdTooltip`: то же для `_handle`/`_timer`.
- [x] `MdSearchView`, `MdDialog`, `MdPopover`: `onClosed` синхронизирует `IsOpen`,
  отписывает input pane и вызывает `Closed` без повторного события.
- [x] Регрессии:
  - внешний `Dispose` snackbar/tooltip очищает статическое состояние;
  - повторный `Dispose` не вызывает `onClosed` второй раз;
  - закрытие top-level освобождает content и callback;
  - после внешнего закрытия новый snackbar не уходит в мёртвую очередь.
- [x] Полный `dotnet test` (221 тест).

## Этап 2. `_wired*` cleanup

Цель: удалённый из коллекции элемент не должен оставаться в `HashSet` и не
должен получать подписки от контрола.

- [x] Добавить internal `MdItemEventWiring<TItem, TDelegate>`:
  - `Add`, `Remove`, `RemoveRange`, `Clear`;
  - `HandleCollectionChanged` для `Remove`/`Replace`/`Move`/`Reset`;
  - хранение handler’ов, чтобы отписка была реальной;
  - отсоединение logical/visual parent у удалённого `Control`.
- [x] Перевести 13 контролов:
  `MdButtonGroup`, `MdStepper`, `MdTimeline`, `MdTabs`, `MdCarousel`,
  `MdNavigationDrawer`, `MdBreadcrumbBar`, `MdFabMenu`, `MdChipGroup`,
  `MdNavigationBar`, `MdNavigationRail`, `MdRadioGroup`, `MdSegmentedButton`,
  `MdNavigationSuiteScaffold`.
- [x] Регрессии: удалённый элемент освобождается GC после `Remove`/`Reset`;
  повторное добавление снова подписывает; `Replace`/`Move` не плодят handlers.
- [x] Полный `dotnet test` (221 тест).

## Этап 3. App bar и keyboard host

- [x] `MdTopAppBar`: подписка на `ScrollViewer.ScrollChanged` только на время
  attach; `OnDetachedFromVisualTree` отписывает, повторный attach подписывает снова.
- [x] `MdKeyboardAvoidingHost._lastFocused`: слабая ссылка; сбрасывается при detach.
- [x] `MdTheme.Observe`: статическая подписка хранит view через `WeakReference`.
- [x] `MdOverlay` не держит owner/content после закрытия handle.
- [x] Регрессии на closing/detach/re-attach, полный `dotnet test` (221 тест).

## Этап 4. `MdList` bulk changes

- [x] Добавлен `HeightIndex` (`List<double>` + Fenwick): append и tail-remove
  амортизированно O(log n), middle insert/move — один сдвиг и одно дерево.
- [x] `InsertItems`/`RemoveItems` больше не копируют и не пересобирают весь
  массив высот на каждый одиночный `Add`/`Remove`.
- [x] `RealizeAllIncremental`/`RealizeWindow` используют общий
  `SyncChildrenToDesired` без `IndexOf`-скана на каждого ребёнка.
- [x] `Children.CollectionChanged` не пересчитывает dividers во время
  realization; `SyncVirtualDividers` меняет свойство только при отличии.
- [x] Одиночный append/tail-remove в невиртуализированном списке обновляет
  только затронутые контролы (`RealizeAppendedItem`, `RealizeRemovedTail`).
- [x] Сохранены fast path для scroll/selection и инкрементальные тесты
  `MdList`/`MdDataTable`.
- [x] Stopwatch-регрессии: 5000 append в virtualized list (~0.3 s) и plain
  list (~3.4 s), 500 append + tail-remove с проверкой height index.
- [x] Полный `dotnet test` (224 теста).

## Этап 5. `MdDataTable` reconciliation

- [x] Не выполнять O(S×N) поиск строки на каждое изменение; индексировать
  source item → row.
- [x] `Replace`/property change обновлять только затронутую строку, не весь table.
- [x] Сохранить сортировку, фильтр, selection, scroll и compact/card mode.
- [x] Тесты на reuse строк и отсутствие полного rebuild.
- [x] `PruneSelection` переведён на один `HashSet` (`O(S+N)`), direct path для
  append/tail-remove/replace при пустом фильтре/сортировке/пагинации, общий
  `SyncRenderedControls` убрал `IndexOf`-скан на каждого ребёнка.
- [x] Подписки на `INotifyPropertyChanged` элементов живы только на время attach;
  изменение свойства перерисовывает только затронутую строку, а при активной
  сортировке/фильтре пересобирает только её visual и передаёт управление
  обычному reconciler'у.
- [x] Полный `dotnet test` (226 тестов на момент этапа).

## Этап 6. `MdTreeView`

- [x] Убрать полный rebuild при раскрытии/сворачивании и изменении одного узла.
- [x] Фильтр без O(n²): один проход + словарь видимых узлов.
- [x] Сохранить selection/scroll/expanded state.
- [x] Тесты на инкрементальное раскрытие и фильтрацию.
- [x] Добавлен кэш `MdTreeNode → TreeRow`; `SyncStack` переиспользует общий
  префикс, а `NodeChanged`/`ChildrenChanged` инвалидируют только затронутые
  строки. Подписки на узлы снимаются при удалении поддерева и на detach.
- [x] Фильтрация использует мемоизированный обход (`MatchesFilter`), поэтому
  каждый узел обрабатывается один раз; состояние раскрытия сохранено.
- [x] Полный `dotnet test` (231 тест на момент этапа).

## Этап 7. Text field hot path

- [x] `MdTextField`/`MdTextArea`: не вызывать полный `ApplyTheme` 2–4 раза на
  keystroke; обновлять только изменившиеся визуалы.
- [x] Кэшировать text metrics/notch и переиспользовать при неизменной ширине.
- [x] Stopwatch-регрессия на вводе и смене темы.
- [x] Дублирующиеся `SyncTrailing`/`SyncFooter` удалены; `SyncState` выполняет
  один `ApplyTheme`, а сам `ApplyTheme` пропускается, если тема, error/focus/
  enabled и состояние floating label не изменились.
- [x] Ширина текста и геометрия notch уже кэшируются в `MdTextMetrics` и
  `MdFieldChromeCache`; при неизменной ширине повторные расчёты не создают
  новую геометрию.
- [x] `VirtualizationBenchmarks`: 2000 обновлений текста и 20 переключений темы
  для `MdTextField`/`MdTextArea` укладываются в Stopwatch-бюджет.
- [x] Полный `dotnet test` (232 теста на момент этапа).

## Этап 8. Финальная проверка

- [x] Полный `dotnet test` — 0 failed (233/233).
- [x] Сборка библиотеки, demo и gallery — 0 warnings/errors.
- [x] Визуальная проверка overlay/snackbar/tooltip в demo или gallery.
- [x] Обновить `docs/PLAN.md`, `docs/HANDOFF.md`, `docs/README.md`, `CHANGELOG.md`.

## Журнал

- 2026-10-22: план раунда 2 сохранён; этап 1 переведён в работу.
- 2026-10-22: этапы 1–3 закрыты. `MdOverlay` получил `onClosed` и освобождает
  layer/content/callbacks; snackbar/tooltip/search/dialog/popover синхронизируют
  состояние. Все 13 `_wired*`-контролов переведены на `MdItemEventWiring` с
  отпиской и отвязкой parent. `MdTopAppBar` и `MdKeyboardAvoidingHost` больше не
  удерживают внешние объекты после detach; `MdTheme.Observe` использует weak view.
  Добавлено 12 регрессий, итог — **221 тест, 0 падений**.
- 2026-10-22: этап 4 закрыт. `MdList` переведён на `HeightIndex` с O(log n)
  append/tail-remove, общим `SyncChildrenToDesired` и точечным обновлением
  одиночных append/tail-remove; добавлены Stopwatch-регрессии на 5000 элементов.
  Итог — **224 теста, 0 падений**.
- 2026-10-22: этап 5 закрыт. `MdDataTable` получил индекс визуалов по элементу,
  direct path для append/tail-remove/replace без фильтра/сортировки/пагинации,
  общий sync без `IndexOf`-скана, O(S+N) pruning selection и подписки на
  `INotifyPropertyChanged` только на время attach. Property change обновляет
  одну строку, а sorted/filtered режимы делегируют reconciler'у.
  Итог — **226 тестов, 0 падений**.
- 2026-10-22: этап 6 закрыт. `MdTreeView` кэширует строки по `MdTreeNode`,
  переиспользует общий префикс при раскрытии/сворачивании/фильтрации и
  инвалидирует только изменённый узел. Фильтр считает видимость за один
  мемоизированный обход, подписки на `NodeChanged`/`ChildrenChanged` снимаются
  при удалении поддерева. Добавлено 5 тестов; итог — **231 тест, 0 падений**.
- 2026-10-22: этап 7 закрыт. `MdTextField`/`MdTextArea` выполняют один
  `ApplyTheme` на обновление состояния, а повторный вызов пропускается по
  кэшу темы/error/focus/floating state. Дубли `SyncTrailing`/`SyncFooter`
  удалены; ширина текста и notch по-прежнему кэшируются. Добавлена
  Stopwatch-регрессия на 2000 вводов и 20 смен темы; итог — **232 теста,
  0 падений**.
- 2026-10-22: этап 8 закрыт. Полный прогон — **233/233**; библиотека, demo и
  desktop gallery собраны без warnings/errors. Demo-smoke прошёл
  (`MD3_SMOKE_OK`), в галерее визуально проверены snackbar, простой tooltip и
  overlay-диалоги/меню; добавлена регрессия на подписку нового поддерева `MdTreeView`. Документация и `CHANGELOG.md` синхронизированы.
