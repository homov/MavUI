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

- Логи сборки не сохраняются в репозитории и не выгружаются. Единственный источник диагностики — аннотации и статусы шагов. Артефакт `build.log` загружается в задание, но его содержимое через `gh` здесь не читалось (нужен отдельный `gh run download`, если он понадобится).
- В песочнице агента нет `dotnet`, поэтому локальная сборка невозможна. Проверка выполняется только в CI.
- Визуальную проверку демо CI не делает: проверяется только сборка и тесты.
- Предупреждения GitHub про Node.js 20 у `actions/checkout@v4` и `actions/upload-artifact@v4` не связаны с кодом; обновление действий не делалось.

## 6. Воспроизведение с нуля

1. Установить `gh` и выполнить `gh auth status` (авторизация должна быть настроена заранее; токены в чат не передаются).
2. Убедиться, что раннер `self-hosted, linux` онлайн (Settings → Actions → Runners).
3. Отправить коммит в `arena/**`, затем выполнить команды из раздела 3.
4. При падении — раздел 4.
