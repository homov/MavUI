# Mav — план оптимизаций по аудиту

Обновлено: 22 октября 2026  
Статус: этапы 1–7 закрыты; регрессионные тесты и полный прогон выполнены.  
Ограничения: без новых NuGet-пакетов, без BenchmarkDotNet, без symbols/pack.

## Легенда

- `[x]` — сделано и проверено тестами.
- `[ ]` — запланировано.
- `[~]` — в работе.

## Уже сделано (baseline)

- [x] P0: устранены статические реестры `MavTooltip`/`MavFlyout`; обработчики живут на контролах.
- [x] P0: `MavCollectionObserver` привязан к attach/detach, не течёт при повторном подключении.
- [x] P0: overlay освобождает handles и root при закрытии top-level.
- [x] P0: `MavThemeService` реализует `Dispose()` и отписывается от `MavTheme.Changed`.
- [x] P1: `MavList` переведён на Fenwick tree для поиска окна, префиксных сумм и обновления высот.
- [x] P1: `MavDataTable` обновляет selection-визуалы на месте, не вызывая полный `RebuildTable()`.
- [x] P1: brush/pen-кэши `MavTheme` ограничены и вытесняются пакетно, а не очищаются целиком.
- [x] P1: bounded-кэш `FormattedText` в `MavTextMetrics`; используется в горячих контролах.
- [x] P1: value-indicator `MavSlider` рисуется через `PushOpacity`, без alpha-кистей на каждый кадр.
- [x] Регрессии: `Tests/LifetimeTests.cs`, `Tests/ThemeCacheTests.cs`, `Tests/TextLayoutCacheTests.cs`,
  сохранение scroll в `Tests/DataTableVirtualizationTests.cs`.
- [x] Полный прогон: 192 теста, 0 падений; библиотека и демо собираются без warnings/errors.

## Этап 1. Кэш статических дуг и donut-геометрии

Цель: не создавать `StreamGeometry` на каждый `Render`, если входные параметры не менялись.
Кэш — per-control, чтобы не переживать смену `IPlatformRenderInterface`/приложения.

- [x] `MavRadialGauge`: кэш track-arc; active-arc кэшируется последним значением.
- [x] `MavCircularProgress`: кэш track-arc и determinate active-arc; wavy/indeterminate не кэшировать.
- [x] `MavSparkline` (Donut): переиспользовать геометрию сегментов при неизменных значениях/размере.
- [x] Регрессионные тесты: повторный render не создаёт новый `Geometry` при тех же параметрах;
  изменение размера/значения инвалидирует кэш.
- [x] Полный `dotnet test` и визуальная проверка demo.

## Этап 2. Кэш field-chrome геометрии

Цель: убрать аллокации `StreamGeometry` при каждой перерисовке `MavTextField`/`MavTextArea`.

- [x] Вынести построение outline в per-control кэш с ключом размеров и notch.
- [x] Использовать кэш в `FieldChrome` и `AreaChrome`.
- [x] Регрессия: повторный render с теми же размерами не создаёт геометрию; resize/notch меняет ключ.
- [x] Полный `dotnet test` и визуальная проверка.

## Этап 3. Инкрементальные изменения `MavList`

Цель: не вызывать `RebuildFromItemsSource` на каждый `CollectionChanged`.

- [x] Обработать `Add`, `Remove`, `Replace`, `Move` точечно; `Reset` оставить полным rebuild.
- [x] Сохранять scroll/выделение; пересобирать Fenwick tree из массива высот, не пересоздавая контролы.
- [x] Регрессии: add/remove/replace/move сохраняют реализованные элементы, высоты и selection.
- [x] Полный `dotnet test` и визуальная проверка.

## Этап 4. Инкрементальные изменения `MavDataTable`

Цель: не вызывать `ReloadItems` на каждый `CollectionChanged`, если изменилась одна строка.

- [x] Точечно обновлять строки для `Add`/`Remove`/`Replace`/`Move`; `Reset` — полный reload.
- [x] Сохранять сортировку, фильтр, selection и scroll.
- [x] Регрессии: изменение одной строки не пересоздаёт остальные и не сбрасывает scroll.
- [x] Полный `dotnet test` и визуальная проверка.

## Этап 5. Lifecycle overlay

Цель: закрыть остаточные утечки `MavOverlay`.

- [x] Периодически/при изменении удалять из `Hosts` записи с умершими owner/host.
- [x] Привязать `PushDismissable` к top-level и снимать dismissable при закрытии.
- [x] Регрессии: закрытие top-level очищает host-запись; dismiss-stack не растёт после закрытия.
- [x] Полный `dotnet test`.

## Этап 6. API и кэш-инвалидация

Цель: закрыть хвосты аудита.

- [x] `IMavThemeService : IDisposable` (осознанное изменение публичного интерфейса).
- [x] `MavTextMetrics.WidthCache` инвалидируется при смене `FontManager`.
- [x] Регрессии: смена сервиса/шрифта не возвращает устаревшую ширину/текст.
- [x] Полный `dotnet test`.

## Этап 7. Финальная проверка

- [x] Полный `dotnet test` — 0 failed.
- [x] Сборка библиотеки и demo — 0 warnings/errors.
- [x] Визуальная проверка через `tools/linux/mavui.py`.
- [x] Обновить этот файл и `docs/HANDOFF.md` итогами.

## Журнал

- 2026-10-22: план сохранён; baseline отмечен.
- 2026-10-22: этапы 1–2 закрыты. `MavArcCache` используется в gauge/circular progress/donut;
  outline текстовых полей кэшируется в `MavFieldChromeCache` (ключ: размер, толщина штриха, notch).
  Добавлены тесты в `Tests/GeometryCacheTests.cs`; полный прогон — 196 тестов, 0 падений.
- 2026-10-22: этапы 3–6 закрыты. `MavCollectionObserver` умеет отдавать детальные
  `NotifyCollectionChangedEventArgs`; `MavList` и `MavDataTable` обновляются инкрементально
  с переиспользованием контролов. `MavOverlay` чистит мёртвые host-записи и dismissables,
  привязанные к закрытому top-level. `IMavThemeService` наследует `IDisposable`;
  `MavTextMetrics` сбрасывает оба кэша при смене `FontManager`.
  Регрессии: `VirtualizationTests` (list add/remove/replace/move, scroll), 
  `DataTableVirtualizationTests` (row reuse, scroll), `LifetimeTests` (overlay lifecycle),
  `TextLayoutCacheTests` (width cache). Итог: **209 тестов, 0 падений**; библиотека, demo и
  gallery собираются без warnings/errors; визуальная проверка полей/таблицы в demo — ОК.
