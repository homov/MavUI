# Миграция HeatController (Xamarin.Forms) → HC (MavUI)

Статус: **план, ожидает решений** (см. раздел 2). Код не начат.

Источник: `ref/HeatController6` (Xamarin.Forms 5, Syncfusion, Autofac, Refit, CommunityToolkit.Mvvm).
Цель: `migrated/HC` — один кроссплатформенный проект на MavUI (Avalonia) для Android и Windows.

---

## 1. Что есть в исходнике

| Часть | Объём | Замечания |
|---|---|---|
| `HeatCeram.Api` (netstandard2.0, Refit) | ~740 строк, 2 клиента | `CloudClient` (`/stinexapiv1`, HTTPS), `LocalClient` (`10.10.10.1:10893`, HTTP, мастер Wi-Fi) |
| `HeatController` (Xamarin.Forms) | 23 XAML-страницы, 35 XAML, ~20 ViewModel, ~870 строк сервисов | Syncfusion (SfGauge, SfLinearProgressBar, SfTabView, SfTextInputLayout), FFImageLoading, кастомные контролы (ExtendedPicker, FontIcon, TemperatureEdit, HourEditControl, VisualSlider) |
| `HeatController.Android` | Wi-Fi: `WifiNetworkService`, `WiFiService` | Только Android: подключение к сети устройства, suggestions, настройки Wi-Fi |
| Локализация | 8 файлов `.resx` по 142 строки | cs, de, hu, it, pl, ro, uk + en по умолчанию. Русского нет. В миграции только en и uk |
| ТЗ | `support/ТЗ.docx` (15.04.2024), `support/ТЗ_25_07_v_2.pdf` (25.07.2024) | 9 пунктов в первом, 5 разделов во втором |
| API-спеки | `support/API.yaml`, `support/API_Doc_2.yaml` | OpenAPI 3.0.2, около 45 операций |

Прочие находки, которые влияют на миграцию:

* **Пароль пользователя хранится открытым текстом** в `Preferences` (`SettingService.UserPassword`, 8 использований). Переносим в `MavSecureStorage`.
* **`RespApiWrap` создаётся 35 раз**, у каждого свой `HttpClient`. Это источник ошибки из ТЗ «Cannot access a disposed object» (ТЗ 2024, п. 4). Нужен один клиент на время жизни приложения.
* **`new LocalClient()` внутри мастера добавления** (2 вызова): каждый раз новый `HttpClient`.
* **39 строк закомментированного кода** в `AddDeviceSteps.cs`, файл `DevicePageViewModel.cs.bak`, `TestPage.xaml`. Не переносим.
* **Файл `support/info.txt` содержит тестовые логин и пароль** в открытом виде. Не копируем в `migrated/`, рекомендуем удалить из репозитория.
* Newtonsoft не используется, сериализация через `System.Text.Json`.

---

## 2. Решения (подтверждены пользователем)

| # | Вопрос | Решение |
|---|---|---|
| 1 | Тестовый хост API | Адрес в настройках. По умолчанию **продакшен** `api.heatceram.com.ua`. Переключение на `apitest` одной настройкой. |
| 2 | Пункты ТЗ 2024/2025 | **Пока не трогаем.** Этап 9 откладывается. Переносим текущий функционал. |
| 3 | Wi-Fi-мастер на Windows | **Скрыт.** На Windows добавление устройства недоступно. Android-реализация переносится. |
| 4 | Языки | **en и uk.** Остальные (cs, de, hu, it, pl, ro) не переносим. |

Допущения (без возражений пользователя, при необходимости уточним):

* Путь `migrated/HC` и `migrated/docs` — внутри репозитория `MavUI`.
* Один `csproj` с целями `net10.0-android` и `net10.0` (Windows, Desktop). Отличается от `ControlsGallery`, где вы отказались от multi-target. Причина: вы явно просили «проект один». Если нужно два `csproj`, скажите.
* Namespace `Hc`, проект `HC`.
* UI на XAML (AXAML), MVVM через `CommunityToolkit.Mvvm` (как в исходнике).
* DI: Autofac заменяем на ручную композицию в `Services/AppServices.cs` (меньше зависимостей).
* Проверка сертификата в `HttpClientFactory` (`ServerCertificateCustomValidationCallback = true`) остаётся как есть, с комментарием о риске. Изменений нет.

---

## 3. Структура целевого проекта

```
migrated/
├── HC/
│   ├── HC.sln
│   ├── HC/                      один проект (Android + Windows)
│   │   ├── HC.csproj            TFM: net10.0-android; net10.0; Avalonia; ProjectReference MavUI, MavUI.Essentials
│   │   ├── Program.cs           Desktop-вход: окно ~390x844 dp (телефон) для быстрого визуального теста
│   │   ├── Android/             MainActivity, манифест, иконки, Wi-Fi-реализация (только Android)
│   │   ├── App.axaml(.cs)       корень приложения, тема MavTheme
│   │   ├── Api/
│   │   │   ├── Cloud/           ICloudApi (Refit), модели запросов/ответов, ошибки
│   │   │   └── Local/           ILocalDeviceApi (Refit, 10.10.10.1:10893)
│   │   ├── Services/            сессия, устройства, расписания, локации, настройки, язык, Wi-Fi-интерфейс
│   │   ├── Pages/               XAML-страницы и их ViewModel (Login, Devices, Device, Schedule, Settings, ...)
│   │   ├── Models/              доменные модели и маппинг из API
│   │   ├── Localization/        по одному файлу на язык: En.cs, Uk.cs
│   │   └── Resources/           шрифты, изображения
│   └── HC.Tests/                тесты (headless, Avalonia.Headless) и маппинг API
└── docs/
    ├── PLAN.md                  этот документ
    ├── NOTES.md                 заметки (открытые вопросы, решения)
    └── API_MAPPING.md           соответствие операций API и методов сервисов
```

Требования, которые следуют из ваших правил:

* Namespace корня: `Hc`. Например, `Hc.Api.Cloud`, `Hc.Services`, `Hc.Pages`.
* Сервисы в `Services/`, страницы и модели в `Pages/` и `Models/`.
* Облачный и локальный API разделены по своим сервисам: `Services/CloudService` (облако) и `Services/LocalDeviceService` (локальный API устройства).
* Локализация: `Localization/`, по одному файлу на язык.

---

## 4. Замены компонентов

| Xamarin / исходник | MavUI | Примечание |
|---|---|---|
| `SfCircularGauge` | `MavRadialGauge` | |
| `SfLinearProgressBar` | `MavLinearProgress` | |
| `SfTabView` | `MavTabs` (`MavTabItem`) | |
| `SfTextInputLayout` + `Entry` | `MavTextField` | Правило библиотеки: TextField не рефакторить сверх задачи |
| `Switch` | `MavSwitch` (`IsToggled`) | |
| `Button` / кнопки | `MavButton`, `MavIconButton` | |
| `ContentPage` + `NavigationPage` | `MavContentPage` + `MavNavigationPage` | |
| Popup-страницы (`*EditPopup`) | `MavDialog`, `MavBottomSheet` | |
| `ExtendedPicker` | `MavSelect` / `MavComboBox` | |
| Нижняя панель действий (если была) | `MavToolbar` (докированная) | MD3 Expressive: нижняя App Bar заменена |
| `FontIcon` + `MaterialDesignIcons.ttf` | `MavIcon` + `MavIcons` | Иконки уже встроены в MavUI |
| `DisplayAlert` / `MessageBoxService` | `MavDialogService` | |
| Snackbar-подобные уведомления | `MavSnackbarService` | |
| `Xamarin.Essentials` (Preferences, SecureStorage) | `MavUI.Essentials` | |
| Нижняя навигация с 3 вкладками, `SfTabView` | `MavNavigationBar` на телефоне, `MavNavigationRail` на широких экранах | Тренд MD3 Expressive, см. 4a |
| `FFImageLoading` | `Avalonia` `Image` + кэш ресурсов | Картинки устройств из `ImageBaseUrl` |
| `Separator`, `CardView`, `TemperatureEdit`, `HourEditControl`, `VisualSlider` | `MavDivider`, `MavCard`, `MavNumberBox`, `MavTimeDial`, `MavSlider` | Если какого-то поведения нет, сначала проверяем библиотеку; новые контролы обсуждаем |
| `Autofac` | ручная композиция | |

Замены с зазором (проверим на этапе 3): тонкие поля «температура/часы» (`TemperatureEdit`, `HourEditControl`) и `VisualSlider`.

---

## 4a. UI и дизайн (MD3 Expressive)

Цель: сохранить сценарии и проверенную логику исходника, но переосмыслить интерфейс под актуальный Material 3 Expressive (Google, май 2025 и последующие обновления). Это не копия экранов 1:1.

### 4a.1 Тренды MD3, которые применяем

Источники: [M3 Expressive, building guide](https://m3.material.io/blog/building-with-m3-expressive), [обзор компонентов M3E](https://saglitz.com/design/material-3), [flexible navigation bar и отказ от drawer на телефоне](https://9to5google.com/2025/05/14/material-3-expressive-navigation/), [vertical menus](https://9to5google.com/2025/11/22/material-3-expressive-menus/), [Compose Material3 1.5 и стабилизация компонентов](https://bytevikas.com/blog/compose-material3-1-5-expressive-stable-apis.html).

| Тренд | Что это значит для HC | Компонент MavUI |
|---|---|---|
| Короткая навигация вместо высокой нижней панели | На телефоне нижняя навигация из 3 пунктов (Группы, Устройства, Настройки). Высоту и «короткий» вариант уточнить в `MavNavigationBar` при реализации (отдельного flexible-варианта в библиотеке нет) | `MavNavigationBar` |
| Drawer на телефоне — deprecated, на широких экранах — rail | Windows ≥ 840 dp и планшеты: rail, на телефоне без drawer | `MavNavigationSuiteScaffold`, `MavNavigationRail` |
| Нижняя App Bar заменяется докированной/плавающей панелью инструментов | Действия страницы устройства (режим, расписание, удалить) — в `Toolbar` | `MavToolbar` (вместо `MavBottomAppBar`) |
| Button groups и split button | Выбор режима (Ручной / Расписание / Выкл.) одной группой; «Сохранить» со стрелкой вариантов | `MavButtonGroup`, `MavSplitButton` |
| FAB menu вместо speed-dial | «+» на вкладке устройств: «Добавить устройство», «Создать группу» | `MavFabMenu` |
| Loading indicator для ожиданий до 5 с | Пока ждём ответ `set_heat_state` (ТЗ 2024 п. 3 — блокировка UI) показываем морфящийся индикатор на карточке, а не спиннер на весь экран | `MavLoadingIndicator` |
| Волнистый прогресс для длительных «нагревается» | Статус «нагрев» на карточке устройства: волнистая полоса, отражает активный нагрев | `MavWavyProgressIndicator`, `MavLinearProgress` |
| Выразительная типографика и крупные числа | Температура на главном экране — крупным шрифтом, как главный «показатель» | `MavTypeRole` (display) |
| Пружинная анимация (spring motion) | Переходы страниц и выдвижение карточек на пружине, без жёстких длительностей | `MavMotion` / `MavMotionScheme` |
| Вертикальные меню с «зазорами» | Меню действий устройства: группы через gap, без длинных разделителей | `MavMenu` |
| Форма контейнеров и морфинг (shape) | Карточки устройств и групп: мягкие скругления разного радиуса по иерархии | `MavCard` (variants), `MavShapeScale` |

### 4a.2 Фирменный стиль и цвет

* **Основной цвет** — фирменный `#3E94FF` (из `Light.xaml`) как seed для `MavTheme`. Динамическая схема: `MavTheme.Configure` с seed, светлая и тёмная тема.
* **Семантика состояний**: нагрев — тёплый tertiary (янтарный), охлаждение/выкл. — нейтральный, ошибка — error role. Не вводим отдельных цветов вне схемы.
* **Контраст**: три уровня контраста (стандарт, средний, высокий), проверка WCAG через `MavAccessibility.MeetsContrast`.
* **Тема системы**: следуем системной, пользователь может переопределить в настройках (как в исходнике, `PreferredTheme`).

### 4a.3 Экраны (концепт)

1. **Вход и аккаунт.** Минимум полей, одна основная кнопка. Регистрация и восстановление — `MavStepper` для шагов, код подтверждения — `MavPinInput`. Ошибки — через коды ошибок API, текст из локализации (ТЗ 2025, раздел 3 — отложено).
2. **Группы (локации).** Карточки групп с агрегированной температурой и количеством устройств. Пустое состояние — `MavEmptyState` с действием.
3. **Устройства.** Список карточек. Тап — страница устройства. Состояние онлайн/офлайн — `MavBadge`. Пул-ту-рефреш — `MavRefreshIndicator`.
4. **Страница устройства (главный экран продукта).**
   * Крупная текущая температура (`MavRadialGauge` или крупная типографика) и целевая температура с `MavNumberBox` / `MavSlider`.
   * Переключатель режима — `MavButtonGroup`: Ручной / Расписание / Выкл.
   * Гистерезис и коэффициенты — в `MavSettingsExpander`, не на главном экране.
   * Нижняя `MavToolbar`: расписание, имя, удалить.
5. **Расписание.** Недельная полоса дней — `MavSegmentedButton`. Дневной график — список интервалов `MavTimeline` с цветом по температуре. Редактирование интервала — `MavBottomSheet` с `MavTimeDial`.
6. **Мастер добавления устройства.** Шаги `MavStepper`, на Windows скрыт (решение). Ожидание сети устройства — `MavLoadingIndicator`, ошибки — `MavBanner`.
7. **Настройки.** Группы в `MavSettingsCard`: язык, тема, часовой пояс, аккаунт, «Удалить аккаунт» в конце со `MavDialog`.

### 4a.4 Адаптивность и размеры

* Базовый макет — телефон (390×844 dp, это же окно Windows для визуального теста).
* Breakpoints библиотеки: compact < 600 dp (нижняя навигация), medium 600–839 dp (rail), expanded ≥ 840 dp (rail с подписями / две колонки на странице устройства).
* Все интерактивные элементы ≥ 48 dp (touch-цель). Проверяется в тестах.
* Локализация: украинский текст длиннее английского на 20–30 %. Все кнопки и подписи проверяются на переполнение (ellipsis и перенос), не фиксируем ширину.

### 4a.5 Движение и обратная связь

* Переходы страниц — `MavNavigationPage` с `Transition = Slide` (вперёд) и `Fade` (модальные).
* Изменение температуры и режима — мгновенный визуальный отклик, подтверждение от сервера — отдельным индикатором. Не блокируем всю страницу.
* Снекбар для результатов («Режим изменён», «Не удалось отправить, повторить») — `MavSnackbarService`.
* Режим «уменьшить движение»: в `MavUI` есть `MavMotionScheme.Reduced`, но системный флаг не читается автоматически. Приложение сопоставляет системную настройку (Android: `Settings.Global.ANIMATOR_DURATION_SCALE`, Windows: «Show animations») с `MavMotionScheme.Reduced` вручную. Этот пункт проверить при реализации.

### 4a.6 Проверка дизайна

* Визуальная проверка — на Windows-окне 390×844 (ваша машина) и на Android-устройстве.
* Скриншоты в песочнице недоступны, поэтому визуальное ревью — по вашим фото/запускам. Для ревью делаем короткий список экранов и состояний (пусто, загрузка, ошибка, онлайн, офлайн, нагрев).
* Контроль: все состояния из списка проходят без обрезки текста на 390 dp и 320 dp.

---

## 5. Этапы

Каждый этап подтверждается CI до старта следующего (правило проекта). Для Android-сборки используется тег `[heads]` в сообщении коммита.

**Этап 0. Решения и подготовка** — сейчас
* Ответы на вопросы раздела 2.
* Удаление `support/info.txt` с тестовыми учётными данными из `migrated/` (сам `ref/` оставляем как источник).

**Этап 1. Каркас**
* `HC.sln`, проект `HC` с целями Android и Windows, `ProjectReference` на MavUI и MavUI.Essentials.
* Пустое приложение: `App.axaml`, главное окно Windows (~390x844 dp), `MainActivity` для Android.
* Дизайн-токены: `MavTheme.Configure` с фирменным seed `#3E94FF`, светлая и тёмная схемы, три уровня контраста (раздел 4a.2).
* Адрес API читается из настроек, по умолчанию продакшен (раздел 2, п. 1).
* Шаг CI: сборка `migrated/HC` (Windows-цель; Android — по `[heads]`).
* Проверка: окно открывается на Windows, сборка Android проходит.

**Этап 2. Cloud API**
* `Api/Cloud`: Refit-интерфейс по `API.yaml` / `API_Doc_2.yaml`, модели `System.Text.Json`.
* Один `HttpClient` на время жизни приложения (вместо 35 создаваемых). Обработка `Cookie` через один `CookieContainer`.
* Проверка сертификата: без изменений, с комментарием.
* Параметр `language` для `user_authorization`, `user_registration`, `user_validation`, `user_password_restoration`, `password_restoration_validate`, `error_codes` (ТЗ 2025, раздел 3). Работает и без ТЗ: если язык пустой, сервер отдаёт EN.
* Тесты: `PasswordEncrypter` даёт тот же результат, что и исходный код (эталонные значения снимаем из `ref`). Маппинг ответов на модели.

**Этап 3. Local API и Wi-Fi**
* `Api/Local`: `ILocalDeviceApi` (`/dev_id`, `/dev_ver`, `/scan`, `/data`, `/user_id`), один клиент с таймаутом 5 с.
* Интерфейс `IWifiProvisioning`:
  * Android: `WifiManager` (подключение, suggestions) — перенос `WifiNetworkService`, очистка;
  * Windows: по решению из раздела 2 (п. 3).
* Мастер добавления устройства (3 шага + ошибка) в `Pages/Device/AddDevice/`. На Windows скрыт (раздел 2, п. 3).

**Этап 4. Сервисы и состояние**
* `SessionService` (логин, автологин, выход). Пароль в `MavSecureStorage`, email и настройки — в `MavPreferences`.
* `DeviceService`, `LocationService`, `ScheduleService`, `SettingsService`, `LanguageService`, `ThemeService`.
* Исправление «Cannot access a disposed object»: один клиент, без `Dispose` в середине запроса (ТЗ 2024, п. 4).
* Блокировка UI на время запроса `set_heat_state` (ТЗ 2024, п. 3), если выбрано.

**Этап 4a. Дизайн-ревью (до этапа 5)**
* Сделать черновики экранов по разделу 4a.3 на MavUI (XAML): вход, группы, устройства, страница устройства, расписание, настройки.
* Показать вам на Windows-окне 390×844. Согласование состояний и отступов. Только после согласования переходим к этапам 5–8.
* Черновики — в `migrated/HC`, без логики (статические данные).

**Этап 5. Страницы: аккаунт**
* Вход, регистрация, подтверждение email, восстановление пароля, смена пароля, выбор языка.
* Исправление `password_restoration_validate` (ТЗ 2024, п. 9): передаём `code`, `password`, `email`.

**Этап 6. Страницы: устройства и локации**
* Список устройств, страница устройства (состояние, температуры, антифриз, гистерезис, коэффициенты, имя, группа, датчик) — по макетам этапа 4a.
* Локации (группы): список, создание, переименование, удаление.

**Этап 7. Расписания**
* Недельное и дневное расписание, типы `time`, `temperature`, а с этапа 7 и `humidity` (ТЗ 2024, п. 5; ТЗ 2025, раздел 2). Поля `h`, `t`, `p`, `s`.
* Пределы температуры и влажности берутся из `get_device_types` (`temp_min`, `temp_max`, `humi_min`, `humi_max`, `gis_min`, `gis_max`, `controll_by`) (ТЗ 2025, раздел 1).

**Этап 8. Настройки и локализация**
* Настройки: язык, тема (светлая/тёмная/системная), часовой пояс, выход, удаление аккаунта.
* `Localization/`: два файла, `En.cs` и `Uk.cs`. Ключи из `Resources.resx`, перенесённые в C#.

**Этап 9. ТЗ-доработки** — отложено по решению пользователя (раздел 2, п. 2)
* 2024: п. 1 (`antifreeze_bit` удалить), п. 2 (картинка umbrella-th), п. 4 (окно ошибки), п. 6 (поля для влажностных устройств), п. 7–8 (пределы), п. 9 (`password_restoration_validate`).
* 2025: раздел 1 (пределы), раздел 2 (влажность), раздел 4 (`antifreeze mod` только для `control_type = temperature`), раздел 5 (`control_type` — массив, внешний датчик).

**Этап 10. Проверка и документация**
* Тесты: ViewModel и сервисы на `Avalonia.Headless`, маппинг API, шифрование пароля.
* Ручная проверка на Windows (визуально, размер телефона) и на Android-устройстве (вы).
* `API_MAPPING.md`: все операции API → методы сервисов.
* Обновление `migrated/docs/NOTES.md`.

---

## 6. Риски

* **Тренды MD3 меняются.** Expressive — свежий стандарт, часть компонентов в Compose ещё помечена как экспериментальная. Внешние образцы (Compose, Google-приложения) сверяем с поведением `MavUI`, а не копируем.
* **Визуальное ревью без скриншотов из песочницы.** Решается ревью по вашим запускам (этап 4a).

* **Android-устройство нельзя проверить из песочницы.** Сборка проверяется CI, поведение на устройстве — вами. Это влияет на этапы 3 и 4.
* **Wi-Fi-мастер** зависит от платформы и от ОС (разрешения Android 13+, `NEARBY_WIFI_DEVICES`). Требует проверки на устройстве.
* **Протокол пароля** (`PasswordEncrypter`, двойной MD5 с префиксом и суффиксом) задан сервером. Менять нельзя, он должен совпадать с сервером.
* **Тестовый API** может отличаться от продакшена (см. раздел 2, п. 1).
* **Объём**: `ref` — ~14 300 строк XAML и C# без `obj`. Миграция ориентирована на сокращение (меньше контролов, общий клиент, сервисы вместо дублей), но не на 1:1.
