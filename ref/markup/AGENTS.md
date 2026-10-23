# Промпт для AI: адаптация Uno.Extensions.Markup.Generators для Avalonia

## Цель
Адаптировать проект `Uno.Extensions.Markup.Generators` под платформу Avalonia. Результат:
- Рабочий генератор в `./src/AMarkup.Generators`.
- Сгенерированная библиотека расширений для Avalonia-классов в `./src/AMarkup.Controls`.

## Исходные данные
- Источник генератора: `./ref/Uno.Extensions.Markup.Generators` (исходники на C#, Roslyn Source Generator).
- Целевой каталог генератора: `./src/AMarkup.Generators`.
- Целевая библиотека: `./src/AMarkup.Controls`.
- Установлен .NET SDK 10 (допустимо использовать `net8.0` или `net10.0`).

## Предварительный анализ (что нужно понять)

### Что делает генератор
Генератор анализирует сборки с атрибутом `[GenerateMarkupForAssembly]` или классы с `partial` и базовыми типами UI-элементов. Он ищет публичные свойства, у которых есть соответствующее статическое поле `*Property` типа `DependencyProperty` (или его аналога). Для таких свойств генерируются Fluent-расширения (методы с префиксом типа `ButtonMarkup`, `TextBlockMarkup` и т.п.).

### Ключевые файлы и их роль
- `StyleBuilderGenerator.cs` — генерирует методы для свойств стиля (`StyleMarkup`).
- `DependencyPropertyExtensionGenerator.cs` — генерирует расширения для DependencyProperty.
- `DataContextGenerator.cs`, `DataTemplateGenerator.cs`, `AttachedPropertyGenerator.cs` — дополнительные генераторы.
- `IncrementalExtensionsGeneratorBase.cs` — базовая логика (поиск `UIElement`, фильтрация классов, генерация namespace-импортов, сборка файла `.g.cs`).
- `Extensibility/*.cs` (`BrushTypeExtension`, `ThicknessTypeExtension`, `ColorTypeExtension`, `GeometryTypeExtension`, `ResourceDictionaryTypeExtension`, `ImageSourceTypeExtension`, `VisibilityExtensions`) — правила генерации параметров для конкретных типов свойств.
- `GenerationTypeInfo.cs`, `DependencyPropertyExtensionInfo.cs`, `StyleBuilderInfo.cs` — модели данных.
- `.csproj` — проект генератора с зависимостями `Microsoft.CodeAnalysis.CSharp` и `Microsoft.CodeAnalysis.Analyzers`.

### Зависимости Uno, которые нужно заменить
| Uno / Microsoft.UI.Xaml | Avalonia (примерные замены) |
|---|---|
| `Microsoft.UI.Xaml` | `Avalonia` |
| `Microsoft.UI.Xaml.UIElement` | `Avalonia.Controls.Control` или базовый класс `Avalonia.AvaloniaObject` |
| `Microsoft.UI.Xaml.FrameworkElement` | `Avalonia.Controls.Control` |
| `Microsoft.UI.Xaml.DependencyProperty` | `Avalonia.AvaloniaProperty` (базовый тип) или `AvaloniaProperty<T>` |
| `Microsoft.UI.Xaml.Controls.ControlTemplate` | `Avalonia.Controls.ControlTemplate` |
| `Microsoft.UI.Xaml.Data.Binding` | `Avalonia.Data.Binding` |
| `Microsoft.UI.Xaml.Style` | `Avalonia.Style` |
| `Microsoft.UI.Xaml.Media.Brush` / `SolidColorBrush` | `Avalonia.Media.IBrush` / `Avalonia.Media.SolidColorBrush` или `Avalonia.Media.SolidPaint` (зависит от версии Avalonia; уточни актуальные типы пакета) |
| `Windows.UI.Color` | `Avalonia.Media.Color` |
| `Uno.Extensions.Markup` | `AMarkup` (или оставь `AMarkup` в namespace) |
| `Uno.Extensions.Markup.Internals.MarkupExtensionAttribute` | `AMarkup.Internals.MarkupExtensionAttribute` (создай или замени ссылку) |
| `ResourceObserver`, `LoadingObserver` | Аналогов в Avalonia нет; либо удалить ссылки, либо заменить на `Avalonia`-специфичные механизмы. Проверь, нужны ли они для работы генератора или это вспомогательные классы для сгенерированного кода. |
| `DataTemplateHelpers` | Аналог в Avalonia может отличаться; уточни типы и namespace. |

## Стратегия выполнения (пошаговая)

### Этап 1. Копирование и базовая очистка
1. Скопируй `./ref/Uno.Extensions.Markup.Generators` в `./src/AMarkup.Generators`.
2. Удали артефакты сборки (`bin/`, `obj/`, `.vs/`, `.suo`).
3. Переименуй `.csproj`: `AMarkup.Generators.csproj` (или просто измени содержимое, сохранив имя файла для совместимости).

### Этап 2. Замена namespace и имён сборок
- Замени `namespace Uno.Extensions.Markup.Generators;` на `namespace AMarkup.Generators;` во всех `.cs`.
- Замени `namespace Uno.Extensions.Markup.Generators.Extensibility;` на `namespace AMarkup.Generators.Extensibility;`.
- Замени ссылки `global::Uno.Extensions.Markup.` и `global::Uno.Extensions.Markup.Internals.` на `global::AMarkup.` / `global::AMarkup.Internals.` в генерируемом коде (`.cs` генератора и в строках, формируемых `CodeBuilder`).
- Обнови `.csproj`: `<AssemblyName>AMarkup.Generators</AssemblyName>`.
- Проверь наличие `Properties/AssemblyInfo.cs` или отключи генерацию (`GenerateAssemblyInfo` уже `False`).

### Этап 3. Замена типовых ссылок на Avalonia
Выполни массовую замену текстов в исходниках генератора (и в `.csproj` при необходимости):
- `Microsoft.UI.Xaml` → `Avalonia.Controls` или `Avalonia` (в зависимости от контекста). Будь осторожен: некоторые ссылки (`Microsoft.UI.Xaml.Data`, `Microsoft.UI.Xaml.Markup`, `Microsoft.UI.Xaml.Controls.Primitives`) должны быть заменены на соответствующие Avalonia-пространства (`Avalonia.Data`, `Avalonia.Controls`, `Avalonia.Controls.Primitives` или аналоги).
- `UIElement` → `Control` (или `AvaloniaObject`, если это базовый класс для DependencyProperty). В коде `IncrementalExtensionsGeneratorBase.cs` используется проверка базового типа `UIElement`; замени на проверку `Avalonia.AvaloniaObject` или `Avalonia.Controls.Control`, в зависимости от того, что использует Avalonia для DependencyProperty.
- `FrameworkElement` → замени на `Avalonia.Controls.Control` (или `Avalonia.Controls.Panel`, но чаще `Control`). Проверь в `StyleBuilderGenerator.cs` и `DependencyPropertyExtensionGenerator.cs`.
- `DependencyProperty` (тип) → проверь, как называется в Avalonia. Обычно это `AvaloniaProperty` или `AvaloniaProperty<TValue>`. В `IsDependencyProperty` замени строку `Microsoft.UI.Xaml.DependencyProperty` на полностью квалифицированное имя типа в Avalonia (например, `Avalonia.AvaloniaProperty` или `Avalonia.AvaloniaProperty` из `Avalonia` namespace). Уточни актуальное имя из пакета `Avalonia`.
- `Binding`, `Setter`, `ResourceDictionary`, `Style`, `ControlTemplate`, `DataTemplate` — замени namespaces и типы на Avalonia.

### Этап 4. Адаптация расширений (Extensibility)
Каждый файл в папке `Extensibility/` содержит правила генерации параметров для конкретных типов свойств.
- `BrushTypeExtension.cs`: замени `global::Microsoft.UI.Xaml.Media.Brush` на `global::Avalonia.Media.IBrush` (или `Avalonia.Media.Brush`, если такой тип есть). Замени `SolidColorBrush` на `Avalonia.Media.SolidColorBrush` или `Avalonia.Media.SolidPaint`. Замени `Windows.UI.Color` на `Avalonia.Media.Color`. Удали или замени вызовы `XamlBindingHelper.ConvertValue` на аналоги Avalonia (если они существуют; если нет — упростить генерацию или использовать конструктор с параметром цвета).
- `ThicknessTypeExtension.cs`: замени типы на Avalonia (`Avalonia.Thickness`, `Avalonia.Thickness`? Уточни из пакета).
- `CornerRadiusTypeExtension.cs`: аналогично (`Avalonia.CornerRadius`).
- `ColorTypeExtension.cs`: замени `Windows.UI.Color` на `Avalonia.Media.Color`.
- `GeometryTypeExtension.cs`: замени `Microsoft.UI.Xaml.Media.Geometry` на `Avalonia.Media.Geometry`.
- `VisibilityExtensions.cs`: `Visibility` в Avalonia обычно `Avalonia.Visibility`, но типы могут отличаться.
- `ResourceDictionaryTypeExtension.cs`, `ImageSourceTypeExtension.cs`: замени соответствующие типы.

**Важно**: проверь, что сгенерированный код использует существующие в Avalonia типы. Если тип отсутствует в текущей версии пакета, замени на ближайший аналог или упростить генерацию (например, использовать `string` или базовый `object`).

### Этап 5. Адаптация генераторов
- `IncrementalExtensionsGeneratorBase.cs`:
  - Замени `GetTypeByMetadataName("Uno.Extensions.Markup.Generator.GenerateMarkupForAssemblyAttribute")` на имя атрибута Avalonia-генератора (например, `AMarkup.Generator.GenerateMarkupForAssemblyAttribute`). Если такого атрибута нет в проекте Avalonia, можно оставить ссылку на новый атрибут (`AMarkup.Generator.GenerateMarkupForAssemblyAttribute`), который нужно будет создать в библиотеке или в генераторе.
  - Замени импорт namespace (`AddNamespaceImport`) с `Microsoft.UI.Xaml.*` на `Avalonia.*` и `AMarkup`.
  - Замени `UIElement` в проверке базового типа.
  - Замени `IsNotImplemented()` и другие расширения на корректные для Avalonia (если используются специальные методы; проверь `Extensions/SharedSymbolExtensions.cs`).
- `StyleBuilderGenerator.cs`:
  - Проверь генерацию для `ControlTemplate`. В Avalonia это `Avalonia.Controls.ControlTemplate`. Замени строку `global::Microsoft.UI.Xaml.Controls.ControlTemplate` на `global::Avalonia.Controls.ControlTemplate`.
  - Проверь метод `GenerateFrameworkTemplateStyleBuilderExtensions`. Замени `FrameworkTemplate` на соответствующий Avalonia-тип (если такой есть; если нет — удали или упрости).
- `DependencyPropertyExtensionGenerator.cs`:
  - Замени `IsDependencyProperty()` на проверку `AvaloniaProperty` (или конкретного типа).
  - Замени проверки базовых классов (`FrameworkElement`) на Avalonia.
  - Удали или замени ссылки на `ResourceObserver.SetResourceParent` и `LoadingObserver.AddLoadingCallback`, если они не существуют в Avalonia. Если они нужны для работы сгенерированного кода, создай аналоги в `AMarkup` или удали их вызовы из сгенерированного кода.
  - Замени `DataTemplateHelpers` на Avalonia-аналоги или упрости генерацию.

### Этап 6. Создание атрибута и вспомогательных классов
- Создай файл `AMarkup.Generator.GenerateMarkupForAssemblyAttribute.cs` (или аналог) в папке генератора или в отдельном проекте `AMarkup`, чтобы генератор мог находить атрибуты в сборках Avalonia.
- Если `ResourceObserver`, `LoadingObserver`, `MarkupExtensionAttribute`, `IDependencyPropertyBuilder`, `DependencyPropertyBuilder` используются в сгенерированном коде, убедись, что они существуют в проекте `AMarkup` или в сгенерированном коде. При необходимости создай минимальные заглушки или ссылайся на пакеты Avalonia, где они могут быть.

### Этап 7. Настройка проекта и сборка генератора
- Обнови `.csproj`:
  - `<TargetFramework>net8.0</TargetFramework>` или `net10.0`.
  - Добавь пакет `Avalonia` (или `Avalonia.Controls`, `Avalonia.Data`, `Avalonia.Media`) в `<ItemGroup>` как `<PackageReference>`, если генератору нужно анализировать типы Avalonia при компиляции.
  - Убедись, что `LangVersion` не ниже `12.0` (или `14.0`, как было).
- Построй проект генератора (`dotnet build ./src/AMarkup.Generators`). Устрани ошибки компиляции (обычно это несовпадение namespace или отсутствующие типы в `Extensibility`).

### Этап 8. Создание проекта для генерации библиотеки `AMarkup.Controls`
1. Создай каталог `./src/AMarkup.Controls`.
2. Внутри создай `.csproj` (библиотека классов, например `.NET 8` или `.NET 10`).
3. Добавь ссылки:
   - `Avalonia` (полный пакет или `Avalonia.Base` + `Avalonia.Controls`).
   - `AMarkup.Generators` (как `ProjectReference` или `PackageReference`, чтобы генератор запускался при сборке).
4. Добавь атрибут `[assembly: GenerateMarkupForAssembly(typeof(SomeAvaloniaControl))]` или просто добавь ссылку на сборку, содержащую классы Avalonia, и используй механизм генератора (например, `GenerateMarkupForAssemblyAttribute` на сборке, которая содержит классы Avalonia). 
   **Примечание**: если генератор работает через анализ `partial` классов или через `GenerateMarkupForAssembly`, подготовь тестовый класс или используй существующие классы Avalonia из пакета. Для простоты можно создать минимальный `partial` класс в проекте `AMarkup.Controls`, который наследует `Avalonia.Controls.Control`, чтобы генератор сгенерировал хотя бы базовые расширения. Либо укажи генератору анализировать сборку `Avalonia` через атрибут.
5. Добавь в `.csproj` ссылку на генератор (`ProjectReference` на `AMarkup.Generators` с `<OutputItemType>Analyzer</OutputItemType>` и `<ReferenceOutputAssembly>false</ReferenceOutputAssembly>` или через `<PackageReference>`).
6. Выполни сборку (`dotnet build ./src/AMarkup.Controls`). Генератор должен создать `.g.cs` файлы в `obj/` или в `bin/` (в зависимости от конфигурации).

### Этап 9. Извлечение сгенерированного кода в библиотеку
- Найди сгенерированные `.g.cs` в `./src/AMarkup.Controls/obj/` или `./src/AMarkup.Controls/bin/` (или используй `dotnet build -v diag` для поиска).
- Скопируй сгенерированные `.g.cs` в `./src/AMarkup.Controls/` в папку `Generated/` или корень.
- Убедись, что `.csproj` библиотеки включает эти файлы (`<Compile Include="Generated/*.g.cs" />`).
- Удали или отключи генератор из `.csproj` `AMarkup.Controls`, если нужно, чтобы библиотека содержала только сгенерированный код (без повторной генерации при каждой сборке). Либо оставь генератор, но тогда это будет «живая» генерация.
- Убедись, что сгенерированный код компилируется (`dotnet build ./src/AMarkup.Controls`).

### Этап 10. Проверка результата
- Проверь, что `./src/AMarkup.Generators` собирается без ошибок.
- Проверь, что в `./src/AMarkup.Controls` присутствуют `.cs` или `.g.cs` с расширениями для Avalonia (`*Markup` классы, методы с параметрами типа `Avalonia.*`).
- Проверь, что namespace сгенерированного кода соответствует `AMarkup` (или `Avalonia`), а не `Uno`.
- Проверь отсутствие ссылок на `Microsoft.UI.Xaml` или `Uno.Extensions.Markup` в сгенерированном коде.

## Сложные моменты и ловушки

### 1. Различие типов DependencyProperty
В Uno используется `Microsoft.UI.Xaml.DependencyProperty` (статическое поле `*Property` типа `DependencyProperty`). В Avalonia используется `AvaloniaProperty` (обычно `AvaloniaProperty<TValue>` или просто `AvaloniaProperty`). Проверь, что метод `IsDependencyProperty` ищет правильное имя типа. Если в Avalonia поле называется `BackgroundProperty` и имеет тип `AvaloniaProperty`, генератор должен это распознавать.

### 2. Отсутствие `ResourceObserver` и `LoadingObserver`
В Uno сгенерированный код использует `ResourceObserver.SetResourceParent` и `LoadingObserver.AddLoadingCallback`. В Avalonia таких классов нет. Нужно либо:
- Удалить эти вызовы из генератора (в `DependencyPropertyExtensionGenerator.cs` и `StyleBuilderGenerator.cs`).
- Заменить их на аналоги Avalonia (если существуют аналоги для управления ресурсами или загрузкой стилей). В противном случае сгенерированный код не скомпилируется.

### 3. Типы `Brush`, `Color`, `Geometry`, `Thickness`
Avalonia использует свои типы (`Avalonia.Media.Color`, `Avalonia.Media.IBrush`, `Avalonia.Media.SolidColorBrush` или `SolidPaint`). Убедись, что в `Extensibility` используются актуальные типы из пакета `Avalonia`, а не `Windows.UI.Color` или `Microsoft.UI.Xaml.Media.Brush`.

### 4. Namespace сгенерированного кода
Генератор формирует namespace на основе `GenerationTypeInfo.TypeContainingNamespace`. Для Avalonia-классов это будет `Avalonia.Controls`, `Avalonia.Media` и т.д. Убедись, что `.csproj` генератора импортирует правильные namespace (`Avalonia`, `Avalonia.Controls` и т.п.), а не `Microsoft.UI.Xaml`.

### 5. `GenerateMarkupForAssemblyAttribute`
Если в Avalonia-проекте нет атрибута `GenerateMarkupForAssembly`, генератор не найдёт сборки для анализа. Создай атрибут или используй механизм `partial` классов. Уточни, как именно генератор должен находить классы для генерации (через атрибут или через синтаксис `partial`).

### 6. `UIElement` как базовый класс
В Uno проверка базового типа ищет `UIElement`. В Avalonia базовый класс для визуальных элементов — `Visual` или `AvaloniaObject`. Уточни, от какого базового класса должны наследоваться классы для генерации. Возможно, нужно заменить проверку на `Avalonia.AvaloniaObject` или `Avalonia.Visual`.

### 7. Компиляция сгенерированного кода
Даже если генератор работает, сгенерированный код может содержать ссылки на несуществующие типы (`MarkupExtensionAttribute`, `IDependencyPropertyBuilder`, `DependencyPropertyBuilder`). Убедись, что эти типы либо существуют в `AMarkup`, либо удалены/заменены в генераторе.

### 8. Версия пакета Avalonia
Убедись, что в `.csproj` используется совместимая версия `Avalonia` (например, `11.0.0` или `11.1.0` для .NET 8/10). Если типы отсутствуют или переименованы в новой версии, генерация сломается.

## Чего ожидать в результате
- `./src/AMarkup.Generators/` содержит полный исходник генератора с namespace `AMarkup`, ссылками на `Avalonia`, без ссылок на `Uno` или `Microsoft.UI.Xaml`.
- `./src/AMarkup.Controls/` содержит сгенерированные `.cs` файлы (например, `ButtonMarkup.g.cs`, `TextBlockMarkup.g.cs` или аналогичные), содержащие Fluent-методы расширения для Avalonia-контролов.
- Оба проекта компилируются (`dotnet build`).
- В коде нет оставшихся ссылок на `Microsoft.UI.Xaml`, `Uno.Extensions.Markup` (кроме возможных комментариев или документации, которые тоже лучше заменить).

## Команды для проверки результата (рекомендуется выполнить после изменений)
```bash
# Очистка и сборка генератора
rm -rf ./src/AMarkup.Generators/bin ./src/AMarkup.Generators/obj
dotnet build ./src/AMarkup.Generators

# Сборка библиотеки с генерацией
dotnet build ./src/AMarkup.Controls

# Поиск сгенерированных файлов
find ./src/AMarkup.Controls/obj -name "*.g.cs" -o -name "*.Generated*" 2>/dev/null

# Проверка отсутствия ссылок на Uno в сгенерированном коде
grep -rni "Microsoft.UI.Xaml" ./src/AMarkup.Controls/ || echo "OK: нет ссылок на Microsoft.UI.Xaml"
grep -rni "Uno\.Extensions\.Markup" ./src/AMarkup.Controls/ || echo "OK: нет ссылок на Uno"
```

---

**Примечание для AI-исполнителя**: если в процессе адаптации обнаружатся типы Avalonia, отсутствующие в текущей версии пакета, или если `AvaloniaProperty` имеет другую структуру, чем `DependencyProperty`, не пытайся «угадать» — замени на ближайший аналог и задокументируй изменение в комментарии или в файле `CHANGELOG.md` в `./src/AMarkup.Generators`. Основная цель — работоспособный генератор и сгенерированная библиотека, даже если для этого потребуется упростить часть функционала (например, убрать поддержку `ResourceObserver` или `LoadingObserver`).