# MavUI — план переписывания

Новая библиотека `MavUI` строится по стандартной модели Avalonia:
`ControlTheme` + `ControlTemplate` в AXAML-ресурсах, `DynamicResource` для токенов.
C# используется только для поведения, кастомной раскладки/рисования и API контролов.
Старый `Md3.Avalonia` не изменяется.

**Правило:** любой контрол, который считается MD3-контролом, получает свой класс
`Mav*` — даже если у него нет новых свойств и он просто наследует стандартный
Avalonia-контрол. Стандартные Avalonia-контролы напрямую не темизируем.

Обозначения: `[ ]` — не начато, `[~]` — в работе, `[x]` — готово.

## Этапы

- [x] **0. Каркас.** `MavUI/`, `MavUI.sln`, csproj, пространства имён, сборка.
- [x] **1a. Базовые ресурсы.** Light/Dark `ThemeDictionaries`, цветовые роли,
  типографика, форма, отступы в `Themes/Tokens/ThemeResources.axaml`.
- [x] **1b. Генерация темы.** Перенесены `MavColorScheme`, `ColorUtilities`,
  `MavColors`, `MavTypography`, `MavTokens`, `MavMotion`, `MavAccessibility`,
  `MavThemeOptions`, `MavThemeService`. `MavTheme` генерирует цвета и раскладывает
  их в ресурсы; `MavThemeStyles` подключает `ControlTheme`.
- [~] **1c. Шрифты и иконки.** Шрифты и лицензии перенесены в
  `MavUI/Resources/Fonts`, создан `MavFonts`; осталось перенести `MavIcons`
  и проверить регистрацию на Desktop/Android.
- [x] **2a. MavButton.** Наследует `Button`; добавлены только MD3-свойства
  `Variant` и `Size`. Визуальные состояния — в `ControlTheme`.
- [x] **2b. MavIconButton / MavFab.** Наследуют `Button`; `MavFab.IsExtended`
  даёт extended-форму. Все размеры/цвета/тени — в ресурсах.
- [ ] **2c. Кнопочные группы.** `MavButtonGroup`, `MavSplitButton`, `MavFabMenu`.
- [ ] **3. Поля ввода.** `MavTextField` (floating label — причина отдельного
  класса), `MavTextArea`, `MavPinInput`, `MavNumberBox`, `MavTagInput`,
  `MavSearchBar`, `MavSearchView`.
- [ ] **4. Выбор.** `MavCheckBox`, `MavRadioButton`, `MavRadioGroup`, `MavSwitch`,
  `MavSlider`, `MavRangeSlider`, `MavSegmentedButton`, `MavChip`, `MavChipGroup`,
  `MavSelect`, `MavComboBox`, `MavRatingBar`.
- [ ] **5. Навигация.** `MavTopAppBar`, `MavBottomAppBar`, `MavNavigationBar`,
  `MavNavigationRail`, `MavNavigationDrawer`, `MavModalNavigationDrawer`,
  `MavTabControl`/`MavTabItem`, `MavBreadcrumb`, `MavPagination`,
  `MavStepper`, `MavToolbar`, `MavContextualActionBar`.
- [ ] **6. Страницы и каркас.** `MavContentPage`, `MavNavigationPage`,
  `MavWindow`, `MavNavigationSuiteScaffold`, `MavSafeArea`,
  `MavKeyboardAvoidingHost`, `MavScrollViewer`.
- [ ] **7. Поверхности.** `MavCard`, `MavSurface`, `MavLabel`, `MavDivider`,
  `MavAvatar`, `MavBadge`, `MavExpansionPanel`, `MavSettingsCard`,
  `MavSplitView`, `MavCarousel`, `MavChatBubble`, `MavFileDrop`.
- [ ] **8. Overlay и сообщения.** `MavDialog`, `MavBottomSheet`, `MavSideSheet`,
  `MavSnackbar`, `MavBanner`, `MavInfoBar`, `MavEmptyState`, `MavMenu`,
  `MavMenuItem`, `MavPopover`, `MavTooltip`, `MavRichTooltip`, `MavFlyout`.
- [ ] **9. Коллекции.** `MavList`/`MavListItem`, `MavSwipeAction`,
  `MavDataTable`, `MavTreeView`/`MavTreeViewItem`, `MavTimeline`.
- [ ] **10. Pickers.** `MavDatePicker`/`MavDatePickerField`/`MavCalendar`,
  `MavTimePicker`/`MavTimePickerField`/`MavTimeDial`, `MavColorPicker`.
- [ ] **11. Индикаторы и графика.** `MavProgressBar`, `MavCircularProgress`,
  `MavSegmentedProgress`, `MavWavyProgress`, `MavSkeleton`,
  `MavRefreshIndicator`, `MavRadialGauge`, `MavSparkline`.
- [ ] **12. Платформа и локализация.** `MavOverlay`, `MavInputs`,
  `MavKeyboard`, `MavSystemBars`, `MavAutomation`, `MavGestureZones`,
  `MavPropertyCache`, `MavTextMetrics`, `MavLocalization`, `MavLoc`.
- [ ] **13. Gallery, производительность, тесты.** Новый sample для
  Desktop/Web/Android; профилирование первого кадра и скролла; затем тесты
  и проверка на реальном Android.

## Инвентарь старых тем/шрифтов/цветов

Код переносится в `MavUI`, старые файлы не изменяются:

| Старое | Новое |
|---|---|
| `Md3.Avalonia/Theming/MdColorScheme.cs` | `MavUI/Theming/MavColorScheme.cs` |
| `Theming/ColorUtilities/**` | `MavUI/Theming/ColorUtilities/**` |
| `MdColors.cs`, `MdTokens.cs`, `MdTypography.cs`, `MdMotion.cs` | `MavColors`, `MavTokens`, `MavTypography`, `MavMotion` |
| `MdAccessibility.cs`, `MdThemeOptions.cs`, `MdThemeService.cs` | `MavAccessibility`, `MavThemeOptions`, `MavThemeService` |
| `MdTheme.cs` | `MavTheme` — генерирует цвета и пишет ресурсы |
| `Themes`/ControlTheme (новое) | `MavThemeStyles` + `Themes/Controls/*.axaml` |
| `MdFonts.cs` + `Resources/Fonts/**` | `MavFonts` + `MavUI/Resources/Fonts/**` |
| `Md3.Avalonia/MdIcons.cs` | `MavIcons` (этап 1c) |

## Правила

- Любой MD3-контрол — отдельный `Mav*`-класс. Даже если это тонкая обёртка.
- Наследовать стандартный Avalonia-контрол и не переопределять его базовое
  поведение без необходимости.
- Не дублировать `Background`, `Foreground`, `Padding`, `Content` и другие
  стандартные свойства: они остаются у базового контрола.
- Новые свойства добавлять только там, где есть MD3-семантика, которой нет
  в Avalonia (`Variant`, `Size`, `IsExtended`, floating label и т.п.).
- Все цвета, типографика, радиусы, отступы и тени — именованные ресурсы.
- `MavTheme` только генерирует/обновляет ресурсы; контролы читают их через
  `DynamicResource` и не перестраивают визуальное дерево.
- Где ControlTheme недостаточно (рисование, hit-test, layout) — C#-логика
  без подмены темы.
- Тесты и профилирование — после полной реализации, на этапе 13.
- Старый проект и его галерея не трогаются.
