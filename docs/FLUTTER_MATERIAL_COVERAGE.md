# Покрытие Flutter Material в MavUI

Аудит от **24 октября 2026 года**. Срез исходников MavUI: `6717ee0` (до добавления этого отчёта). Это сравнение **компонентов**, а не попытка считать каждый класс Flutter эквивалентом отдельного контрола Avalonia.

## Краткий ответ

- В официальном кратком каталоге Flutter Material 3 **28 позиций** в шести разделах [1](https://docs.flutter.dev/ui/widgets/material). У всех **28/28 есть базовый функциональный аналог** в `MavUI/Controls`. Это **покрытие названных сценариев**, а не «100% совместимость с Flutter»: API, состояния, анимации и возможности композиции могут различаться.
- За пределами этой страницы Flutter предлагает и другие Material-виджеты и строительные блоки. Существенные расхождения: **тема для отдельного поддерева и стили состояний**, **немодальный и изменяемый по высоте bottom sheet**, **произвольное содержимое/вертикальная или взвешенная раскладка карусели**, **перетаскивание элементов списка для переупорядочивания**, **свайп между вкладками**, **разворачиваемая navigation rail** и **настольная строка меню**. Ниже указаны конкретные исходники и ссылки на Flutter API.
- Исследование выполнено по публичному API и реализации в репозитории, а не по одному лишь совпадению названий. **Попиксельное соответствие, доступность, поведение на каждом хосте и полная эквивалентность Flutter не подтверждались**. CI проверяет MavUI, но не сравнивает его с Flutter.

### Что именно считали

База — страница «Material component widgets» в документации Flutter 3.47 (обновлена 29.09.2026): 5 действий + 3 коммуникации + 5 контейнеров + 6 элементов навигации + 8 элементов выбора + 1 текстовый ввод = **28 позиций** [1](https://docs.flutter.dev/ui/widgets/material). Одна позиция каталога может покрываться несколькими типами MavUI; одна группа Flutter-кнопок, напротив, считается **одной** позицией. «Есть» означает, что в библиотеке есть самостоятельный Material-контрол/сервис для базового использования, а не только возможность нарисовать нечто похожее силами Avalonia. Это не оценка количества опций конструктора, точности MD3-токенов или зрелости тестов.

С Flutter 3.47 Material доступен также как отдельный пакет `material_ui`; переход пока опционален, а исходная версия пакета соответствует замороженному Material API фреймворка [1](https://docs.flutter.dev/release/breaking-changes/material-ui-and-cupertino-ui). Для сценариев **вне** краткого каталога ниже используются официальные каталоги Material 2 [2](https://docs.flutter.dev/ui/widgets/material2), прокрутки [6](https://docs.flutter.dev/ui/widgets/scrolling), ввода [5](https://docs.flutter.dev/ui/widgets/input) и документация конкретных Flutter API. Это не дополнительный процент: состав большого API меняется, а многие виджеты — инфраструктура Flutter, которую на стороне MavUI предоставляет Avalonia.

## Матрица краткого каталога Material 3 (28/28)

Каждая ссылка в столбце MavUI ведёт к реализации в этом репозитории. Знак `✓` означает **наличие базового аналога**, не полное равенство поведения. Значимые отличия раскрыты после таблицы.

| Раздел Flutter | Компонент Flutter | Аналог MavUI | База |
|---|---|---|:---:|
| Действия | Common buttons (filled, tonal, elevated, outlined, text) | [`MavButton`](../MavUI/Controls/MavButton.cs) с `Variant` | ✓ |
| Действия | FloatingActionButton | [`MavFab`](../MavUI/Controls/MavFab.cs) | ✓ |
| Действия | Extended FloatingActionButton | [`MavExtendedFab`](../MavUI/Controls/MavExtendedFab.cs) | ✓ |
| Действия | IconButton | [`MavIconButton`](../MavUI/Controls/MavIconButton.cs) | ✓ |
| Действия | SegmentedButton | [`MavSegmentedButton`](../MavUI/Controls/MavSegmentedButton.cs) | ✓ |
| Коммуникация | Badge | [`MavBadge`](../MavUI/Controls/MavBadge.cs) / [`MavBadgedBox`](../MavUI/Controls/MavBadgedBox.cs) | ✓ |
| Коммуникация | LinearProgressIndicator | [`MavLinearProgress`](../MavUI/Controls/MavLinearProgress.cs) | ✓ |
| Коммуникация | SnackBar | [`MavSnackbar`](../MavUI/Controls/MavSnackbar.cs) | ✓ |
| Контейнеры | AlertDialog | [`MavDialog`](../MavUI/Controls/MavDialog.cs) | ✓ |
| Контейнеры | Bottom sheet | [`MavBottomSheet`](../MavUI/Controls/MavBottomSheet.cs) — **модальный** | ✓ |
| Контейнеры | Card | [`MavCard`](../MavUI/Controls/MavCard.cs), 3 варианта | ✓ |
| Контейнеры | Divider | [`MavDivider`](../MavUI/Controls/MavDivider.cs) | ✓ |
| Контейнеры | ListTile | [`MavListItem`](../MavUI/Controls/MavListItem.cs) | ✓ |
| Навигация | AppBar | [`MavTopAppBar`](../MavUI/Controls/MavTopAppBar.cs), 4 варианта | ✓ |
| Навигация | Bottom app bar | [`MavBottomAppBar`](../MavUI/Controls/MavBottomAppBar.cs) | ✓ |
| Навигация | NavigationBar | [`MavNavigationBar`](../MavUI/Controls/MavNavigationBar.cs) | ✓ |
| Навигация | NavigationDrawer | [`MavNavigationDrawer`](../MavUI/Controls/MavNavigationDrawer.cs) + [`MavModalNavigationDrawer`](../MavUI/Controls/MavModalNavigationDrawer.cs) | ✓ |
| Навигация | Navigation rail | [`MavNavigationRail`](../MavUI/Controls/MavNavigationRail.cs) | ✓ |
| Навигация | TabBar | [`MavTabs`](../MavUI/Controls/MavTabs.cs), primary/secondary | ✓ |
| Выбор | Checkbox | [`MavCheckBox`](../MavUI/Controls/MavCheckBox.cs) | ✓ |
| Выбор | Chip | [`MavChip`](../MavUI/Controls/MavChip.cs), assist/filter/input/suggestion | ✓ |
| Выбор | DatePicker | [`MavDatePicker`](../MavUI/Controls/MavDatePicker.cs), в том числе диапазон | ✓ |
| Выбор | Menu | [`MavMenu`](../MavUI/Controls/MavMenu.cs), вложенные пункты | ✓ |
| Выбор | Radio | [`MavRadioButton`](../MavUI/Controls/MavRadioButton.cs) / [`MavRadioGroup`](../MavUI/Controls/MavRadioGroup.cs) | ✓ |
| Выбор | Slider | [`MavSlider`](../MavUI/Controls/MavSlider.cs) / [`MavRangeSlider`](../MavUI/Controls/MavRangeSlider.cs) | ✓ |
| Выбор | Switch | [`MavSwitch`](../MavUI/Controls/MavSwitch.cs) | ✓ |
| Выбор | TimePicker | [`MavTimePicker`](../MavUI/Controls/MavTimePicker.cs), dial/input | ✓ |
| Текст | TextField | [`MavTextField`](../MavUI/Controls/MavTextField.cs) / [`MavTextArea`](../MavUI/Controls/MavTextArea.cs) | ✓ |

## Что есть сверх этих 28 пунктов

Краткий каталог **не** перечисляет весь Flutter Material: например, каталог Material 2 отдельно показывает `CircularProgressIndicator`, `DataTable`, `ExpansionPanel`, `Stepper`, `Tooltip`, `Scaffold` и `SliverAppBar` [2](https://docs.flutter.dev/ui/widgets/material2). В MavUI также имеются:

| Сценарий Flutter | Имеющийся эквивалент MavUI | Оговорка |
|---|---|---|
| CircularProgressIndicator; RangeSlider | [`MavCircularProgress`](../MavUI/Controls/MavCircularProgress.cs), [`MavRangeSlider`](../MavUI/Controls/MavRangeSlider.cs) | Базовые контролы, не входят в 28. |
| SearchBar / SearchAnchor | [`MavSearchBar`](../MavUI/Controls/MavSearchBar.cs) + [`MavSearchView`](../MavUI/Controls/MavSearchView.cs) | Поле с открытием полноэкранного/компактного поиска и предложениями; композиция и управление маршрутом отличаются от Flutter [6](https://api.flutter.dev/flutter/material/SearchAnchor-class.html). |
| DropdownMenu; Autocomplete | [`MavSelect`](../MavUI/Controls/MavSelect.cs), [`MavComboBox`](../MavUI/Controls/MavComboBox.cs) | В `MavComboBox` есть фильтрация и клавиатурный выбор, но нет произвольного `Autocomplete` с виджетом результата/универсальным options-builder [12](https://api.flutter.dev/flutter/material/DropdownMenu-class.html), [5](https://docs.flutter.dev/ui/widgets/input). |
| DataTable; пагинация | [`MavDataTable`](../MavUI/Controls/MavDataTable.cs), [`MavPagination`](../MavUI/Controls/MavPagination.cs) | Сортировка, фильтр, выделение и пагинация присутствуют; **не** утверждается полная эквивалентность `PaginatedDataTable`/`DataTableSource`. |
| RefreshIndicator | [`MavRefreshIndicator`](../MavUI/Controls/MavRefreshIndicator.cs) | Pull-to-refresh существует; это пункт отдельного каталога прокрутки [6](https://docs.flutter.dev/ui/widgets/scrolling). |
| ExpansionPanel / ExpansionTile; Stepper | [`MavExpansionPanel`](../MavUI/Controls/MavExpansionPanel.cs), [`MavExpansionPanelList`](../MavUI/Controls/MavExpansionPanelList.cs), [`MavStepper`](../MavUI/Controls/MavStepper.cs) | Есть аналоги сценариев; управление состоянием устроено иначе. |
| Tooltip; MaterialBanner | [`MavTooltip`](../MavUI/Controls/MavTooltip.cs), [`MavRichTooltip`](../MavUI/Controls/MavRichTooltip.cs), [`MavBanner`](../MavUI/Controls/MavBanner.cs) | В каталоге Material 3 эти пункты не перечислены. |
| Адаптивная навигация | [`MavNavigationSuiteScaffold`](../MavUI/Controls/MavNavigationSuiteScaffold.cs) | Автоматически выбирает bar/rail/drawer по ширине; это дополнительный готовый сценарий MavUI, не отдельная позиция краткого каталога. |

Есть и компоненты, отсутствующие **именно в кратком каталоге**, но полезные для собственных приложений: [`MavSideSheet`](../MavUI/Controls/MavSideSheet.cs), [`MavSplitButton`](../MavUI/Controls/MavSplitButton.cs), [`MavFabMenu`](../MavUI/Controls/MavFabMenu.cs), [`MavWavyProgressIndicator`](../MavUI/Controls/MavWavyProgressIndicator.cs), [`MavTreeView`](../MavUI/Controls/MavTreeView.cs). Это **не** утверждение, что во Flutter невозможно собрать соответствующий интерфейс.

## Важные отличия от Flutter (не видны в счёте 28/28)

| Сценарий | Flutter | Что обнаружено в MavUI | Вывод |
|---|---|---|---|
| Локальная тема | `ThemeData` и вложенный `Theme` переопределяют тему поддерева [1](https://api.flutter.dev/flutter/material/ThemeData-class.html). | [`MavTheme.Current`](../MavUI/Theming/MavTheme.cs) и [`MavThemeStyle`](../MavUI/Theming/MavThemeStyle.cs) меняют **глобальное статическое** состояние. Цвет/форму можно задавать отдельным контролам, но второго независимого `MavTheme` в одном visual tree нет. | **Архитектурное отличие** для экранов с локальными темами. |
| Стили по состояниям | Flutter `ButtonStyle` задаёт значения отдельно для pressed/hovered/focused/disabled ([API](https://api.flutter.dev/flutter/material/ButtonStyle-class.html)). | [`MavButton`](../MavUI/Controls/MavButton.cs) предлагает варианты, размер и форму, но состояние/цвет внутренних элементов вычисляет закрытый `ApplyTheme`; публичного аналога `WidgetStateProperty` для кнопки нет. | **Базовые виды есть, программный дизайн-тюнинг ограничен**. |
| Bottom sheet | Flutter различает модальный `showModalBottomSheet` и немодальный `showBottomSheet`, а также допускает изменение размеров через `DraggableScrollableSheet` [1](https://api.flutter.dev/flutter/material/showModalBottomSheet.html), [3](https://api.flutter.dev/flutter/material/showBottomSheet.html). | [`MavBottomSheet`](../MavUI/Controls/MavBottomSheet.cs) явно реализован как **modal**; drag вниз закрывает, но нет отдельной persistent/snap-высоты реализации. [`MavSideSheet`](../MavUI/Controls/MavSideSheet.cs) — другая категория. | **Отсутствует немодальный/изменяемый по высоте вариант**. |
| Карусель | `CarouselView` принимает произвольные дочерние виджеты, позволяет weighted/vertical и infinite-режимы [8](https://api.flutter.dev/flutter/material/CarouselView-class.html). | [`MavCarousel`](../MavUI/Controls/MavCarousel.cs) рисует **горизонтальные** карточки `MavCarouselItem`; `ItemTemplate` используется только если создаёт `MavCarouselItem` (иначе берётся `ToString()`). Есть готовые `Hero`/`MultiBrowse`/`Uncontained` и `Next`/`Previous` по кругу, но нет произвольных дочерних контролов и API weighted/vertical/infinite scroll. | **Частичный аналог**, название переоценивает паритет. |
| Вкладки | `TabBarView` + `TabController` синхронизируют индикатор и перелистываемый контент [1](https://api.flutter.dev/flutter/material/TabBarView-class.html), [2](https://api.flutter.dev/flutter/material/TabController-class.html). | [`MavTabs`](../MavUI/Controls/MavTabs.cs) переключает свой контент через `SelectedIndex`, но в коде нет горизонтального drag/swipe по содержимому и отдельного синхронизируемого контроллера. | **Есть TabBar, нет готового аналога всей связки TabBarView/TabController**. |
| Списки с переупорядочиванием | `ReorderableListView` меняет порядок элементов drag-and-drop [4](https://api.flutter.dev/flutter/material/ReorderableListView-class.html). | [`MavList`](../MavUI/Controls/MavList.cs) умеет `ItemsSource`/виртуализацию внутри ScrollViewer; [`MavListItem`](../MavUI/Controls/MavListItem.cs) имеет swipe-actions, но в коде списка нет встроенного reorder. | **Отдельного эквивалента нет**; не путать swipe-actions с перетаскиванием порядка. |
| Navigation rail | Во Flutter есть `extended`, `scrollable`, trailing/leading и настройка ширины [1](https://api.flutter.dev/flutter/material/NavigationRail-class.html). | [`MavNavigationRail`](../MavUI/Controls/MavNavigationRail.cs) строит внутреннюю полосу шириной 80 и предоставляет header и destinations; переключения в extended-режим/внутренней прокрутки списка нет. | **Базовый rail есть, расширенный — нет**. |
| Меню на desktop | `MenuBar` даёт горизонтальную строку каскадных меню с навигацией клавишами и hover [1](https://api.flutter.dev/flutter/material/MenuBar-class.html). | [`MavMenu`](../MavUI/Controls/MavMenu.cs) показывает привязанное всплывающее меню со вложенными пунктами; отдельного визуального `MavMenuBar` нет. | **Popup есть, menu bar отсутствует**. |
| Формы | `Form`/`FormField`/`TextFormField` связывают поля, validation/save/reset и жизненный цикл состояния [1](https://api.flutter.dev/flutter/material/TextFormField-class.html). | [`MavFormValidator`](../MavUI/Controls/MavFormValidator.cs) задаёт правила, показывает ошибки, имеет `Validate`/`ClearErrors`; поля отдельные. Универсального visual `FormField<T>` с групповым save/reset нет. | **Проверка есть, целостного FormField-слоя нет**. |
| App bar при прокрутке | `SliverAppBar` живёт внутри `CustomScrollView` [2](https://docs.flutter.dev/ui/widgets/material2). | [`MavTopAppBar`](../MavUI/Controls/MavTopAppBar.cs) умеет `AttachScrollViewer`/`CollapseProgress`, но это не аналог всей sliver-модели (pinned/floating/stretch с `CustomScrollView`). | **Частичный сценарий**; не считать `SliverAppBar` покрытым только из-за `AppBar`. |
| Интеграция экрана и сообщений | `Scaffold` предоставляет единый layout для app bar, body, FAB, drawer, bottom sheet [1](https://api.flutter.dev/flutter/material/Scaffold-class.html); `ScaffoldMessenger` задаёт scope snackbars ([API](https://api.flutter.dev/flutter/material/ScaffoldMessenger-class.html)). | [`MavNavigationPage`](../MavUI/Controls/MavNavigationPage.cs), [`MavContentPage`](../MavUI/Controls/MavContentPage.cs) и [`MavNavigationSuiteScaffold`](../MavUI/Controls/MavNavigationSuiteScaffold.cs) покрывают большую часть слотов; [`MavSnackbar`](../MavUI/Controls/MavSnackbar.cs) — глобальная очередь, а не messenger, привязанный к поддереву. | **Сценарий собирается, модель композиции/область сообщений отличаются**. |
| Встроенные переводы библиотеки | У `material_ui` собственные Material-localization delegates [1](https://docs.flutter.dev/release/breaking-changes/material-ui-and-cupertino-ui). | [`MavLocalization`](../MavUI/MavLocalization.cs) поддерживает смену языка и провайдеры, но встроенный fallback — английский; `uk/de/fr/it/es` сейчас лежат в **демо**, а не в библиотеке ([пример](../samples/ControlsGallery/ControlsGallery/Localization)). | **Для паритета локализаций нужна отдельная библиотечная поставка переводов**; наличие переводов галереи не решает этот вопрос. |

> Не включаем `Row`, `Column`, `GridView`, `Navigator`, низкоуровневые sliver-классы и `Cupertino` в знаменатель 28: часть из них относится к ядру Flutter или иному дизайн-языку, а на стороне MavUI базовые обязанности несёт Avalonia. Но для прикладного сравнения их сценарии можно исследовать отдельно. В частности, [`MavItems`](../MavUI/Controls/MavItems.cs) создаёт контрол для каждого элемента и не равен ленивому `GridView.builder`; [`MavList`](../MavUI/Controls/MavList.cs) виртуализует режим `ItemsSource` внутри скроллера.

## Практические следующие шаги

1. **Выбрать целевой паритет**: «все виджеты краткого MD3 каталога» уже достигнут на уровне базового наличия. Если цель — Flutter-подобный опыт разработки, приоритетнее не новые имена, а scope темы, полноценные формы и недостающие режимы существующих контролов.
2. **Мобильные сценарии**: немодальный/resize bottom sheet; `CarouselView` с произвольным содержимым и параметрами компоновки; свайп контента вкладок. **Большие коллекции**: reorder для `MavList` с `ItemsSource` и виртуализацией.
3. **Широкие экраны**: extended/scrollable navigation rail, отдельный `MenuBar`. Для сложной прокрутки заранее определить, нужен ли аналог sliver-модели, или достаточно возможностей Avalonia + `MavTopAppBar`.
4. **Перед заявлением о feature parity** сделать воспроизводимую таблицу тестов на mobile/desktop/browser: светлая/тёмная/динамическая тема, клавиатура/мышь/тач, узкая/широкая ширина и safe area, семантика screen reader/фокус, RTL/локали, длинные списки, анимация и reduced motion. Сравнивать конкретные состояния и поведение, а не только число классов. Существующие [`Tests/`](../Tests) и снимки `GalleryVisualTests` проверяют MavUI самостоятельно, не против эталонных Flutter-кадров.

**Итог:** MavUI уже покрывает **28/28 основных названий** в официальном коротком MD3-каталоге Flutter, но это не 100% покрытие всего Material API и тем более не строгий поведенческий паритет. Основная работа для «как во Flutter» — режимы и инфраструктура, а не добавление ещё двадцати похожих кнопок.
