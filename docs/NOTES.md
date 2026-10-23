# Рабочие заметки

Обновлено: 24 октября 2026. Заметки не привязаны к ветке: актуальны для любой `arena/**`.

## Отложено

- **macOS / iOS для MavUI.Essentials** — отложено по решению пользователя (23.10.2026).
  Нужны Keychain для SecureStorage и `NSUserDefaults` для Preferences. Таргеты Apple не собираются
  на Linux-раннере, поэтому делать только вместе с macOS-раннером или проверкой на Mac.

## MavUI.Essentials: состояние

- Один проект `MavUI.Essentials/` с таргетами `net10.0`, `net10.0-windows`, `net10.0-android`
  (`-p:IncludeAndroid=true`), `net10.0-browser` (`-p:IncludeBrowser=true`).
- Все таргеты собираются в CI: `[heads]` в сообщении коммита запускает шаг Android/Browser.
- SecureStorage в браузере не подключён (fail closed): `localStorage` хранит открытым текстом.
- Android-код собран, но на устройстве ещё не проверен (Keystore, SharedPreferences).
- Браузерный Preferences (`localStorage`) собран, в браузере не проверен.

## CI

- Ручной запуск `workflow_dispatch` из агента недоступен (HTTP 403). Для шагов Android/Browser
  использовать `[heads]` в сообщении коммита.
- Коммиты только с правками `docs/**` или `**/*.md` не запускают workflow (`paths-ignore`).

## Скриншоты: как передать картинку агенту

Проверено 24.10.2026 на прогонах `40033920973`, `40034328614`, `40034644563`. Подробности схемы —
`docs/CI.md`, раздел 7. Здесь — короткая памятка «что делать руками».

### Канал 1 (для своего хоста, самый быстрый): файл в репозитории

CI не нужен вообще: картинка лежит в рабочем дереве, агент открывает её сам.

```bash
# снять окно демо
python3 tools/linux/mavui.py start
python3 tools/linux/mavui.py shot --out /tmp/before.png
# или Windows: pwsh.exe -File tools\win\mavui.ps1 -Action shot -Out C:\tmp\before.png

# положить в репозиторий и запушить в текущую ветку
mkdir -p docs/screenshots
cp /tmp/before.png docs/screenshots/2026-10-24-signin-before.png
git add docs/screenshots && git commit -m "docs: screenshot of the sign-in page" && git push
```

Правила, чтобы это оставалось удобным:

- путь `docs/screenshots/<дата>-<тема>-<before|after>.png`; имя без пробелов и кириллицы;
- PNG или JPEG, до ~2 МБ (headless-кадр 390×844 весит 15–25 КБ);
- коммиты, которые трогают только `docs/**`, **не запускают CI** (`paths-ignore`) — быстро и без раннера;
- `ci/screenshots/` в `.gitignore`: это временная папка для выгрузки из CI, её не коммитят;
- в тексте сообщения указать, что именно смотреть («метка Email наложена на текст»).

### Канал 2 (для CI): base64-нотисы + `tools/read_screenshots.py`

Headless-тесты HC сами пишут PNG в `ui-report/` (`ScreenDump.Capture`). CI отдаёт их аннотациями,
скрипт собирает обратно:

```bash
python3 tools/read_screenshots.py                  # последний прогон текущей ветки
python3 tools/read_screenshots.py --list           # только список файлов и чанков
python3 tools/read_screenshots.py --run 40034644563
# -> ci/screenshots/run-<id>/*.png, размер и sha256 сверены с манифестом
```

Что читает скрипт: заголовки нотисов `PNG <имя> <k>/<N>` и манифест `PNG manifest`
(`<имя>.png  <байты> bytes  <N> chunk(s)  sha256 <hex>`). Код выхода 1, если файл неполный,
не совпал размер/sha256 или чанки не декодировались.

### Канал 3 (не работает): артефакты прогона

`gh run download` из песочницы падает на редиректе: ссылка ведёт на
`productionresultssa*.blob.core.windows.net`, и этот хост закрыт. Не тратить на это время,
даже для `build-log`.

### Жёсткие лимиты GitHub (замерены, не менять наугад)

| Лимит | Значение | Где учтено |
|---|---|---|
| длина текста одной аннотации | **4096** символов (обрезается молча) | `PNG_CHUNK = 4000` |
| нотисов на шаг | **10** (11-й теряется) | `PNG_PER_STEP = 10` |
| бюджет прогона | `PNG_BATCHES * 10` чанков ≈ 90 КБ | `PNG_BATCHES = 3` |

Инвариант: **число шагов `Screenshots as base64 (batch k of N)` в `build.yml` должно быть равно
`PNG_BATCHES`**. Если картинки перестали влезать, шаг печатает `::warning PNG budget exceeded`,
а манифест помечает файл `SKIPPED over budget` — тогда поднять обе цифры сразу.

### Если картинка «не пришла»

1. `python3 tools/read_screenshots.py --list` — видно, сколько чанков дошло (`4/7` = обрезано).
2. `4/7` или «missing» → прогон старый (до `647375c`, части по 12 000 символов) либо в `build.yml`
   меньше batch-шагов, чем `PNG_BATCHES`.
3. «no PNG annotations in this run» → тесты не дошли до `ScreenDump.Capture` (смотрит шаг
   `Run tests`), или прогон был только по `docs/**` (workflow не запускался).
4. Нотисы есть, sha256 не сходится → менять ничего не нужно, файл уже записан; просто взять
   другой прогон или переснять картинку каналом 1.

### Что уже отснято и что ищем на скриншотах

Сейчас снимаются три кадра HC: `startup` (страница входа после запуска), `sign-in-error`
(неверный пароль + баннер), `home` (пустое состояние «No groups»).
Первые же кадры нашли два бага: наезд метки `Email` на предзаполненный текст (`MavTextField`,
фикс `b96e5ee`) и контент `MavTabs`, не занимающий высоту окна (`0ea8af2`).
При просмотре смотреть: наложение текста, обрезку по краям, пустые области вместо контента,
не тот оттенок/радиус, ripple и focus-слой (это анимация — на мгновенном кадре он выглядит
пятном и багом не является).

