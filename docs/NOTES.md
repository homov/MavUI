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

## Скриншоты и логи: механизм доставки агенту

Задача: картинки и логи рождаются на self-hosted раннере пользователя, а смотрит их агент в песочнице,
куда закрыт весь интернет кроме `github.com`, `api.github.com`, `codeload`, npm и PyPI. Работающих
каналов два, третий (артефакты прогона) невозможен по сети.

| Канал | Когда | Что даёт |
|---|---|---|
| 1. файл в `docs/screenshots/` | кадры со своего рабочего хоста | ничего не настраивать, но нет CI |
| 2. ветка `ci/reports` + `tools/read_screenshots.py` | всё, что производит CI | PNG, дампы экранов, `build.log` |
| 3. артефакты прогона | — | **не работает**: ссылка ведёт в Azure Blob, хост закрыт |

---

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
- `ci/reports/` в `.gitignore`: это временная папка для выгрузки из CI, её не коммитят;
- в тексте сообщения указать, что именно смотреть («метка Email наложена на текст»).

---

### Канал 2 (для CI): ветка `ci/reports` — полный механизм

Проверено на прогонах `40033920973`, `40034644563`, `40036382515`, `40036791845`, `40037773180`.
Короткая версия для повседневной работы:

```bash
python3 tools/read_screenshots.py                     # последние отчёты -> ci/reports/
python3 tools/read_screenshots.py --list              # что лежит в ветке, без скачивания
python3 tools/read_screenshots.py --run 40037773180   # проверить, что в ветке именно этот прогон
python3 tools/read_screenshots.py --annotations --run 40034644563   # старые прогоны (base64)
```

Ниже — как это воспроизвести с нуля.

#### Схема в пяти шагах

```
[1] тест рисует кадр (Skia, headless)
        -> ui-report/*.png + gallery.txt   (папка из MAV_SHOT_DIR)
[2] шаг workflow собирает папку: PNG, *.txt, build.log, SHA256SUMS.txt, README.md
        -> git init -b ci/reports, один коммит, git push --force
[3] GitHub хранит ветку ci/reports: всегда ровно один коммит без родителей
[4] tools/read_screenshots.py идёт через api.github.com:
        contents API (base64, файлы до 1 МБ) или blob API (сырые байты, без лимита)
[5] скрипт сверяет размер из tree и sha256 из SHA256SUMS.txt, пишет файлы в ci/reports/
```

#### 0. Что требуется

- self-hosted раннер с `dotnet`, `git`, `python3` (в этом репозитории метки `[self-hosted, linux]`);
- `permissions: contents: write` у job: без него `git push` в ветку упадёт;
- на машине агента: `gh` с авторизацией и доступ к `api.github.com`;
- `.gitignore`: строка `ci/reports/` (рабочая копия отчётов не должна попадать в коммиты).

#### 1. Тест пишет PNG — обязательные условия

Кадр в headless-режиме получается только при **реальном рисовании**, иначе
`CaptureRenderedFrame()` вернёт пустую заглушку:

```csharp
// AppBuilder тестовой сборки
AppBuilder.Configure<App>()
    .UseMav(...)
    .UseSkia()                                                  // нужен package Avalonia.Skia
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });

// снимок
for (var i = 0; i < 3; i++)
    AvaloniaHeadlessPlatform.ForceRenderTimerTick();            // дать окну отрисоваться
using var frame = window.CaptureRenderedFrame();                // не null только с UseSkia
frame!.Save(path, PngBitmapEncoderOptions.Default);             // в 12.1 старый Save(path, int?) — obsolete
```

Проверки, без которых снимок бесполезен (все уже в коде):

- файл больше ~4 КБ — иначе это однотонная заглушка, а не экран (`Assert` в `Shots.Capture`);
- страница длиннее окна: снимаются **два** кадра — первый экран и вся страница в окне, выросшем до
  `extent.Height` (у галереи 989…3453 px контента, поэтому первый экран показывает лишь верх);
- прокрутка доходит до конца: `scroller.Offset.Y == extent.Height - viewport.Height`;
- `extent.Width <= viewport.Width` — горизонтальной прокрутки на страницах галереи быть не должно;
- ни один видимый контрол не выходит за окно (`FindHorizontalOverflow`).

Куда писать: папка из `MAV_SHOT_DIR`, иначе `HC_UI_REPORT_DIR`, иначе `%TEMP%/mav-gallery-shots`.
В CI обе переменные указывают на `$GITHUB_WORKSPACE/ui-report`, чтобы PNG и дампы HC лежали вместе.
Рядом тест пишет `gallery.txt` — построчный отчёт (размер PNG, `extent`, `viewport`, `fits`), который
потом читается как обычный текст.

#### 2. Шаг workflow — точный YAML

```yaml
jobs:
  build:
    runs-on: [self-hosted, linux]
    permissions:
      contents: write          # только ради push в ci/reports

    steps:
      # ... build и dotnet test ...

      - name: Run tests
        env:
          HC_UI_REPORT_DIR: ${{ github.workspace }}/ui-report
          MAV_SHOT_DIR: ${{ github.workspace }}/ui-report
        run: |
          # все наборы тестов пишут сюда; build.log собирается через tee
          ...

      - name: Publish screenshots and build log to the ci/reports branch
        if: always()                       # отчёты нужны и при падении тестов
        shell: bash
        env:
          GITHUB_TOKEN: ${{ github.token }}  # токен не хранится на раннере
        run: |
          set -uo pipefail
          [ -d ui-report ] || { echo "no ui-report folder"; exit 0; }
          count=$(find ui-report -maxdepth 1 -name '*.png' | wc -l)
          echo "screenshots: $count"
          [ "$count" -gt 0 ] || { echo "nothing to publish"; exit 0; }
          work="$RUNNER_TEMP/shots-$GITHUB_RUN_ID"
          rm -rf "$work"; mkdir -p "$work"
          cp ui-report/*.png "$work"/
          cp ui-report/*.txt "$work"/ 2>/dev/null || true
          [ -f build.log ] && cp build.log "$work"/
          # контрольные суммы всего, что публикуем
          ( cd "$work" && for f in *; do
              [ "$f" = SHA256SUMS.txt ] && continue
              [ -f "$f" ] && sha256sum "$f"
            done > SHA256SUMS.txt )
          printf 'CI reports of run %s (%s), branch %s.\nScreenshots (PNG), screen outlines (*.txt) and build.log.\nRead them with tools/read_screenshots.py.\n' \
            "$GITHUB_RUN_ID" "${GITHUB_SHA:0:8}" "$GITHUB_REF_NAME" > "$work/README.md"
          cd "$work"
          git init -q -b ci/reports
          git add -A
          git -c user.name="mav-ci" -c user.email="mav-ci@users.noreply.github.com" \
            commit -q -m "Reports of run $GITHUB_RUN_ID (${GITHUB_SHA:0:8}): $count image(s)"
          # падение публикации не должно скрывать результат тестов
          if ! git push --force --quiet \
              "https://x-access-token:${GITHUB_TOKEN}@github.com/${GITHUB_REPOSITORY}.git" ci/reports; then
            echo "::warning title=Reports not published::git push to ci/reports failed (contents: write?)"
          fi
```

Почему именно так:

- `git init` в отдельной папке + `--force`: ветка всегда содержит **один** коммит без родителей,
  история не растёт (замер: `commits?sha=ci/reports` → 1, `parents` → 0);
- имя ветки `ci/reports` **не** попадает под `on.push.branches: arena/**`, поэтому публикация не
  запускает новый прогон и не зацикливается;
- push идёт по явному URL с `x-access-token`, а не через `git remote`, чтобы токен не осел в `.git`;
- `sha256sum` в самой ветке: получатель может доказать, что байты не побились по дороге.

#### 3. Хранение: одна ветка, один коммит

```
ci/reports
└── Reports of run 40037773180 (…): 29 image(s)     ← единственный коммит, parents: 0
    ├── page-Inputs.png, page-Inputs-full.png, …     ← 29 PNG (11 страниц × 2, тёмная тема,
    │                                                   3 ширины shell, 3 кадра HC)
    ├── gallery.txt, startup.txt, home.txt, …        ← текстовые отчёты
    ├── build.log                                    ← полный лог сборки
    ├── SHA256SUMS.txt
    └── README.md                                    ← номер прогона и коммита
```

Старые объекты после force-push на ветку больше не ссылаются и удаляются сборщиком мусора GitHub.
Рабочая ветка от этого не меняется: в её истории из картинок только ассеты Android из исходного
импорта (`git log --all --diff-filter=A -- '*.png'` → один старый коммит), скриншотов нет.

#### 4. Чтение на стороне агента

`tools/read_screenshots.py` (стандартная библиотека + `gh`):

1. `GET /repos/{owner}/{repo}/branches/ci%2Freports` → sha, сообщение коммита, tree;
2. `GET /repos/{owner}/{repo}/git/trees/{tree}?recursive=1` → список файлов и размеров;
3. на каждый файл `GET /repos/{owner}/{repo}/contents/{path}?ref=ci/reports` — JSON с
   `"encoding": "base64"` (это транспорт GitHub, не ручная склейка), для файлов >1 МБ поле `content`
   пустое → запасной путь `GET /repos/{owner}/{repo}/git/blobs/{sha}` с `Accept: application/vnd.github.raw`;
4. сверка: размер с tree + sha256 с `SHA256SUMS.txt`; при несовпадении — код выхода 1;
5. запись в `ci/reports/` (перезаписывает прошлую выгрузку).

#### 5. Проверка, что канал жив

```bash
gh api repos/homov/MavUI/git/ref/heads/ci%2Freports --jq .object.sha   # 40 символов, не 8!
gh api repos/homov/MavUI/commits?sha=ci/reports --jq length            # ожидаем 1
python3 tools/read_screenshots.py --list                               # 29 image(s), размеры
python3 tools/read_screenshots.py                                      # все строки "ok"
```

Ожидаемый хвост выгрузки: `output: ci/reports`, суммарно ~3.7 МБ на 29 PNG (36 файлов вместе с логами). Проверка одного файла:

```bash
python3 -c "print(open('ci/reports/page-Inputs.png','rb').read(8))"     # b'\x89PNG\r\n\x1a\n'
```

#### Перенос в другой репозиторий — чек-лист

1. Скопировать `tools/read_screenshots.py`; поменять `BRANCH = "ci/reports"`, если имя другое.
2. Добавить шаг «Publish …» из п.2, заменив `ui-report` на свою папку с отчётами.
3. Поставить `permissions: contents: write` (или выдать токену право на push).
4. Убедиться, что имя ветки не подпадает под триггеры workflow.
5. Добавить `ci/reports/` в `.gitignore`.
6. В тестах: `UseSkia()` + `UseHeadlessDrawing = false`, папка из переменной окружения,
   сохранение через `PngBitmapEncoderOptions.Default`.
7. Прогнать один раз, проверить `--list` и `--run <id>`.
8. Не складывать в ветку ничего, что нельзя показывать: содержимое ветки видно всем, у кого есть
   доступ к репозиторию (поэтому в тестах поля ввода маскируются: `<N chars hidden>`).

#### Типовые поломки

| Симптом | Причина | Что делать |
|---|---|---|
| `Reports not published` | нет `contents: write` или протух токен | включить права, перезапустить |
| Ветки нет совсем | шаг не дошёл: пустой `ui-report` | смотреть шаг `Run tests` |
| В ветке чужой прогон | она всегда хранит последний | `--run` покажет номер, перезапустить нужный |
| `sha256 mismatch` | файл битый/недошёл | скачать заново; при повторе — переснять каналом 1 |
| PNG весит ~300 Б | нет реального рисования | `UseSkia()` + `UseHeadlessDrawing=false` |
| Кадр только первый экран | окно не выросло до контента | снимать `-full` по `extent.Height` |
| `gh run download` падает | артефакты в Azure Blob, хост закрыт | не использовать, брать из ветки |

---

### Канал 3 (не работает): артефакты прогона

`gh run download` и `gh api .../artifacts/<id>/zip` возвращают подписанную ссылку на
`productionresultssa*.blob.core.windows.net`, а TLS к нему рвётся (`SSL_ERROR_SYSCALL`):
в песочнице разрешены только `github.com`, `api.github.com`, `codeload`, npm и PyPI.
Метаданные артефакта видны (`gh api repos/<repo>/actions/artifacts`: id, имя, размер, expired),
байты — нет. Поэтому `build.log` и кадры дублируются в `ci/reports`.

---

### Жёсткие лимиты GitHub (замерены, не менять наугад)

| Лимит | Значение | Где учтено |
|---|---|---|
| длина текста одной аннотации | **4096** символов (обрезается молча) | `PNG_CHUNK = 4000` |
| нотисов на шаг | **10** (11-й теряется) | `PNG_PER_STEP = 10` |
| бюджет прогона | ~300 КБ текста на job (наши кадры — 3.7 МБ) | поэтому ветка, а не аннотации |
| файл в contents API | 1 МБ (дальше `content` пустой) | запасной путь через blob API |

Аннотациями пользуется только `--annotations` (чтение старых прогонов) и `report_ui` для текстовых
дампов: `PNG_CHUNK = 4000`, `PNG_PER_STEP = 10`, `PNG_BATCHES = 3` в `ci/scripts/test_annotations.py`.
Эти константы трогать не нужно — новые картинки идут в ветку.

### Если отчётов нет

1. `python3 tools/read_screenshots.py --list` — что вообще лежит в ветке и от какого прогона.
2. «the branch holds the screenshots of run X, not Y» → в ветке последний прогон; взять его или
   прогнать нужную ветку заново.
3. Пусто/нет ветки → смотреть шаг `Publish screenshots and build log` в прогоне; если он упал с
   `Reports not published`, не хватает `contents: write`.
4. Кадров нет, а шаг прошёл → тесты не дошли до `Capture` (смотреть шаг `Run tests`), либо прогон
   был только по `docs/**` (workflow не запускался).
5. «sha256 mismatch» → файл записан, но битый: скачать ещё раз, при повторе — переснять каналом 1.

### Что уже отснято и что ищем на скриншотах

Снимаются 3 кадра HC: `startup` (страница входа после запуска), `sign-in-error` (неверный пароль +
баннер), `home` (пустое состояние «No groups») — и 32 кадра галереи: 11 страниц × первый экран +
полный кадр, 3 ширины shell (1600/980/420), тёмная тема, открытое FAB-меню и 5 полных кадров
страницы Theming на узком экране для украинского, немецкого, французского, итальянского и испанского.

Все страницы галереи выше окна (989…3453 px контента), поэтому на каждый destination снимаются два
кадра: `page-NAME.png` — первый экран 1280×900, и `page-NAME-full.png` — вся страница в окне,
выросшем до высоты контента (до 12000 px). Числа по каждому кадру — в `ci/reports/gallery.txt`.

Первые же кадры нашли два бага: наезд метки `Email` на предзаполненный текст (`MavTextField`,
фикс `b96e5ee`) и контент `MavTabs`, не занимающий высоту окна (`0ea8af2`).
При просмотре искать: наложение текста, обрезку по краям, пустые области вместо контента, не тот
оттенок/радиус, переполнение по горизонтали (особенно на узких экранах и при длинных переводах).
Ripple/focus-слой на мгновенном кадре выглядит светлым пятном — это анимация, а не баг.
