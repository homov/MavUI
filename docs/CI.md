# CI: как запускается сборка и как читать результаты

Документ описывает, как проверки работают в этом репозитории и как получить их результат, чтобы воспроизвести то же самое без повторных догадок.

## 1. Что настроено

- Workflow: `.github/workflows/build.yml`, имя `Build and test (self-hosted)`.
- Запуск:
  - `push` в ветки `arena/**`, кроме изменений только в `docs/**` и `**/*.md`;
  - `workflow_dispatch` (ручной запуск из вкладки Actions или `gh workflow run`).
- Раннер: собственная Linux-машина пользователя, метки `[self-hosted, linux]`. Пока раннер не онлайн, прогоны остаются в очереди.
- Права токена: `contents: read`. Лог сборки в ветку не публикуется (коммит `035a9c7`).
- Таймаут задания: 45 минут.

Локальный «эмулятор» GitHub Actions (act и т. п.) не используется. Всё выполняется на раннере.

## 2. Что собирается

Шаг «Build tests and ControlsGallery desktop» (Desktop, Release):

- `MavUI/MavUI.csproj`
- `Tests/MavUI.Tests.csproj`
- `samples/ControlsGallery/ControlsGallery.Desktop`
- `samples/ControlsGalleryMarkup/ControlsGalleryMarkup.Desktop`
- `MavUI.Essentials/MavUI.Essentials.csproj`
- `migrated/HC/HC/HC.csproj` для `net10.0` и `net10.0-windows`
- `migrated/HC/HC.Tests`

Затем «Run tests»: `Tests/MavUI.Tests.csproj` и `migrated/HC/HC.Tests` через `dotnet test --no-build`.

Android и Browser собираются только при `workflow_dispatch` или если сообщение коммита содержит `[heads]`. Android для HC в CI не собирается.

`[profile]` в сообщении коммита включает шаг профилирования (`Profile theme switch`).

## 3. Как запустить и дождаться результата (gh)

Ветку сначала отправляют, затем берут самый свежий прогон этой ветки:

```bash
git push origin <ветка>
sleep 8
RUN=$(gh run list --branch <ветка> --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch "$RUN" --exit-status    # код выхода = результат прогона
gh run list --branch <ветка> --limit 3   # сводка
```

Ручной запуск с Android/Browser:

```bash
gh workflow run build.yml --ref <ветка>
```

Проверить, что прогон относится к нужному коммиту:

```bash
gh run view "$RUN" --json headSha,conclusion,status --jq '.headSha[0:7], .status, .conclusion'
git rev-parse --short HEAD
```

Шаги задания:

```bash
gh run view "$RUN" --json jobs --jq '.jobs[] | .name, (.steps[] | "  \(.conclusion) \(.name)")'
```

## 4. Как читать ошибки компилятора

Полный лог недоступен, но шаг «Annotate compiler errors» превращает ошибки `dotnet build` в аннотации проверки (Checks API). Их читают так:

```bash
JOB=$(gh api repos/<owner>/<repo>/actions/runs/$RUN/jobs --jq '.jobs[0].id')
gh api "repos/<owner>/<repo>/check-runs/$JOB/annotations?per_page=100" \
  --jq '.[] | select(.annotation_level=="failure") | "\(.path):\(.start_line) \(.message[0:260])"' | sort -u
```

Для всех уровней (предупреждения тоже):

```bash
gh api "repos/<owner>/<repo>/check-runs/$JOB/annotations?per_page=100" \
  --jq '.[] | "\(.annotation_level) \(.path):\(.start_line) \(.message[0:160])"' | sort | uniq -c | sort -rn
```

Если аннотаций нет, а шаг сборки упал, причину ищут в шагах задания, а не в аннотациях.

## 5. Ограничения

- Логи сборки не сохраняются в репозитории и не выгружаются. Единственный источник диагностики — аннотации и статусы шагов.
- Артефакты (`build.log`, будущие скриншоты) из песочницы агента **не скачиваются**: `gh run download` редиректит на `productionresultssa*.blob.core.windows.net`, а этот хост закрыт. Поэтому картинки передаются аннотациями (раздел 7).
- В песочнице агента нет `dotnet`, поэтому локальная сборка невозможна. Проверка выполняется только в CI.
- Предупреждения GitHub про Node.js 20 у `actions/checkout@v4` и `actions/upload-artifact@v4` не связаны с кодом; обновление действий не делалось.

## 6. Воспроизведение с нуля

1. Установить `gh` и выполнить `gh auth status` (авторизация должна быть настроена заранее; токены в чат не передаются).
2. Убедиться, что раннер `self-hosted, linux` онлайн (Settings → Actions → Runners).
3. Отправить коммит в `arena/**`, затем выполнить команды из раздела 3.
4. При падении — раздел 4.

## 7. Скриншоты из HEADLESS-тестов (картинки в песочницу агента)

Headless-тесты HC (`migrated/HC/HC.Tests/Ui`, `ScreenDump.Capture`) пишут PNG в `HC_UI_REPORT_DIR`
(`ui-report/` в рабочем каталоге прогона). Чтобы картинки доходили до агента, они едут base64-нотисами
на check run задания, а `tools/read_screenshots.py` собирает их обратно в файлы.

Почему именно так — два жёстких лимита GitHub, замеренных на прогоне `40031288662`:

- текст одной аннотации обрезается до **4096** символов (не 12 000, как было в первом варианте);
- шаг держит не больше **10** нотисов (11-й пропадает).

Отсюда схема: `PNG_CHUNK = 4000`, `PNG_PER_STEP = 10`, `PNG_BATCHES = 3` в
`ci/scripts/test_annotations.py`; в `build.yml` один шаг печатает манифест
(`PNG manifest`: размер, число чанков, sha256) и ещё три шага печатают по 10 чанков
(`--png-batch 0..2`). Итого до 30 чанков ≈ 120 000 символов base64 ≈ 90 КБ на прогон.
Если картинки перестают влезать, манифест помечает их `SKIPPED over budget` и шаг печатает
`::warning PNG budget exceeded` — тогда поднимают `PNG_BATCHES` и добавляют столько же шагов.

Прочитать скриншоты последнего прогона текущей ветки:

```bash
python3 tools/read_screenshots.py                       # последний прогон текущей ветки
python3 tools/read_screenshots.py --run 40031288662     # конкретный прогон
python3 tools/read_screenshots.py --list                # только список файлов и чанков
```

Файлы пишутся в `ci/screenshots/run-<id>/` (каталог в `.gitignore`), размер и sha256 сверяются
с манифестом; при несовпадении скрипт возвращает код 1. Дальше PNG можно открыть локально или
показать агенту.

Для картинок с **своего рабочего хоста** (не CI) канал другой и он проще: снять окно
(`tools/linux/mavui.py shot --out ...`, `tools/win/mavui.ps1 -Action shot -Out ...`), положить PNG
в репозиторий и запушить в текущую ветку — агент читает файл прямо из рабочего дерева.
