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

## 7. Скриншоты и build.log: как забрать их из прогона

Пошаговое воспроизведение механизма (с кодом теста, точным YAML-шагом и чек-листом переноса
в другой репозиторий) — `docs/NOTES.md`, раздел «Скриншоты и логи: механизм доставки агенту».
Здесь — короткая версия.

Замерить пришлось два ограничения, оба действуют и сегодня:

- **Байты артефактов из песочницы не доходят.** `gh run download`, `gh api .../artifacts/<id>/zip`
  и любой GET архива получают от GitHub подписанную ссылку на
  `productionresultssa*.blob.core.windows.net`; TLS к этому хосту рвётся (`SSL_ERROR_SYSCALL`),
  потому что в песочнице разрешены только `github.com`, `api.github.com`, `codeload`,
  `registry.npmjs.org`, `pypi.org`, `files.pythonhosted.org`. Видно только метаданные артефакта
  (`id`, имя, размер, `expired`).
- **Аннотации вмещают мало.** Текст одной аннотации обрезается до 4096 символов, шаг держит
  10 нотисов, а весь job — около 300 КБ текста. Галерея делает 18 PNG на 1.7 МБ, то есть
  аннотациями она не передаётся (проверено: 16 файлов ушли в `SKIPPED over budget`).

Поэтому CI **force-push’ит отчёты в единственную ветку `ci/reports`** — один коммит без истории,
каждый прогон заменяет предыдущий. В ветке: все PNG, текстовые дампы экранов (`*.txt`),
`gallery.txt` (размеры кадров, число контролов) и **`build.log`** (полный лог сборки — то, чего
не хватало для диагностики), плюс `SHA256SUMS.txt` и `README.md` с номером прогона.

```bash
python3 tools/read_screenshots.py            # отчёты последнего прогона -> ci/reports/
python3 tools/read_screenshots.py --list     # что лежит в ветке, без скачивания
python3 tools/read_screenshots.py --run 40036382515   # проверить, что в ветке именно этот прогон
python3 tools/read_screenshots.py --annotations --run 40034644563   # старые прогоны (base64)
```

Скрипт ходит только через `api.github.com`, сверяет каждый файл с `SHA256SUMS.txt` и пишет в
`ci/reports/` (каталог в `.gitignore`). При несовпадении — код выхода 1.

Если push в ветку упал (нет прав), шаг печатает `::warning Reports not published`, но результат
тестов это не скрывает; `permissions: contents: write` для этого шага уже включён в workflow.

Для картинок с **своего хоста** (не CI) канал проще: снять окно
(`tools/linux/mavui.py shot --out ...`, `tools/win/mavui.ps1 -Action shot -Out ...`), положить PNG
в `docs/screenshots/` и запушить в текущую ветку — агент читает файл прямо из рабочего дерева.
