# Flutter: тесты, скриншоты длинных экранов и получение результатов из CI

Автономная инструкция для **любого Flutter-репозитория**. Документ можно хранить отдельно от целевого Flutter-проекта: все относительные пути ниже отсчитываются от корня *того* Flutter-проекта, с которым предстоит работать. Для выполнения не требуются .NET и файлы репозитория, в котором хранится эта инструкция.

## 1. Выбрать нужный уровень проверки

| Задача | Инструмент | Где работает | Что показывает PNG |
|---|---|---|---|
| Логика и обычные widget-тесты | `flutter test` | Локально или в Linux CI, без эмулятора | Необязательно снимать PNG |
| Быстрая визуальная проверка экранов | `flutter test visual_test/ui_capture_test.dart` | Headless Flutter test runner | Flutter-виджет в заданном тестовом viewport; **это не снимок настоящего Android/iOS** |
| Проверка изображения на устройстве | `integration_test` + `flutter drive` | Android-эмулятор/устройство; iOS на macOS; также возможен Web с драйвером браузера | Кадр Flutter-приложения на соответствующей платформе |

Сначала получить воспроизводимые widget-снимки и журналы в обычном CI. Если задача касается платформенных шрифтов, плагинов, клавиатуры, нативной оболочки или фактического рендеринга Android/iOS, добавить отдельный integration-прогон. Один PNG первого viewport **не означает**, что длинная страница отснята целиком.

## 2. Подготовка в новом репозитории

1. Открыть `pubspec.yaml`, узнать версию Flutter и существующие каталоги `test/`, `integration_test/`, CI и способ запуска нужного экрана. Не предполагать, что `lib/main.dart` можно запустить без авторизации или сети.
2. Определить экраны, состояния, ширины, локали и тему. Подготовить фиксированные данные и время, отключить случайность/сетевые зависимости, замокать плагины. Не снимать реальные персональные данные и секреты.
3. Для базового шаблона ниже нужен `flutter_test` в `dev_dependencies` (обычно он уже есть). Назначить **конкретный прокручиваемый элемент** и последний элемент страницы: в примере это ключ `capture-scroll` и текст `Item 39`.
4. Создать в целевом репозитории `visual_test/ui_capture_test.dart`. Добавить `ui-report/` и `.ui-report-downloaded/` в его `.gitignore`. Это каталоги результатов, а не исходники.
5. Если Flutter установлен, из корня целевого проекта выполнить:

```bash
flutter --version
flutter doctor -v                 # диагностика; Android SDK для headless-тестов не нужен
flutter pub get
flutter analyze
flutter test --reporter expanded  # существующие тесты в test/
flutter test visual_test/ui_capture_test.dart --reporter expanded
```

Если SDK недоступен в рабочей среде, не подменять проверку догадками: запустить workflow ниже в CI и прочитать его логи/PNG. Для самодостаточности следующие шаблоны *сначала* снимают встроенную демонстрационную страницу; затем заменить её реальным экраном проекта.

## 3. Headless PNG: все положения прокрутки

Скопировать код в `visual_test/ui_capture_test.dart`. `_buildTarget` — **единственное место демонстрационного UI**, которое обязательно заменить на настоящее приложение/экран с необходимыми зависимостями, маршрутом и фикстурами. Ключ должен стоять на нужном `ListView`, `CustomScrollView` и т. п.; для вложенных прокруток выбрать именно нужный `Scrollable`, а не первый попавшийся. Заменить проверку `Item 39` на признак **фактического конца** реальной страницы.

```dart
import 'dart:io';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';

const _scrollKey = ValueKey('capture-scroll');

// ПРОБНЫЙ экран: заменить на MaterialApp/CupertinoApp + нужную страницу проекта.
Widget _buildTarget() => MaterialApp(
      home: Scaffold(
        appBar: AppBar(title: const Text('Demo — replace with app screen')),
        body: ListView(
          key: _scrollKey,
          children: List.generate(
            40,
            (i) => SizedBox(
              height: 72,
              child: ListTile(title: Text('Item $i')),
            ),
          ),
        ),
      ),
    );

Future<void> _savePng(GlobalKey boundaryKey, File file) async {
  final renderObject = boundaryKey.currentContext?.findRenderObject();
  if (renderObject is! RenderRepaintBoundary) {
    throw StateError('RepaintBoundary не найден: ${file.path}');
  }
  final image = await renderObject.toImage(pixelRatio: 1);
  try {
    final data = await image.toByteData(format: ui.ImageByteFormat.png);
    if (data == null) throw StateError('Пустой PNG: ${file.path}');
    file.writeAsBytesSync(
      data.buffer.asUint8List(data.offsetInBytes, data.lengthInBytes),
      flush: true,
    );
  } finally {
    image.dispose();
  }
}

void main() {
  testWidgets('captures the whole scroll at two sizes', (tester) async {
    final output = Directory(
      Platform.environment['UI_SHOT_DIR'] ?? 'ui-report/shots',
    )..createSync(recursive: true);

    // Установить размер *до* pumpWidget; DPR=1 упрощает сборку кадров.
    tester.view.devicePixelRatio = 1;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    for (final size in <Size>[const Size(390, 844), const Size(1280, 800)]) {
      tester.view.physicalSize = size;
      final boundaryKey = GlobalKey();
      await tester.pumpWidget(
        RepaintBoundary(key: boundaryKey, child: _buildTarget()),
      );
      await tester.pumpAndSettle();

      final target = find.byKey(_scrollKey);
      expect(target, findsOneWidget);
      final scrollable = find.descendant(
        of: target,
        matching: find.byType(Scrollable),
      ).first;
      final prefix = 'screen_${size.width.toInt()}x${size.height.toInt()}';
      final manifest = StringBuffer(
        'file\toffset\tviewport\tmaxScrollExtent\textentAfter\n',
      );
      var reachedEnd = false;

      for (var frame = 0; frame < 60; frame++) {
        final position = tester.state<ScrollableState>(scrollable).position;
        final name = '$prefix-${frame.toString().padLeft(2, '0')}.png';
        await _savePng(boundaryKey, File('${output.path}/$name'));
        manifest.writeln(
          '$name\t${position.pixels.toStringAsFixed(1)}'
          '\t${position.viewportDimension.toStringAsFixed(1)}'
          '\t${position.maxScrollExtent.toStringAsFixed(1)}'
          '\t${position.extentAfter.toStringAsFixed(1)}',
        );
        expect(tester.takeException(), isNull);

        if (position.extentAfter <= 1) {
          reachedEnd = true;
          break;
        }
        // Шаг меньше высоты viewport: соседние кадры перекрываются.
        final next = (position.pixels + position.viewportDimension * 0.75)
            .clamp(position.minScrollExtent, position.maxScrollExtent)
            .toDouble();
        expect(next, greaterThan(position.pixels + 0.01));
        position.jumpTo(next);
        await tester.pump(); // дать новому положению отрисоваться до PNG
      }

      File('${output.path}/$prefix.tsv').writeAsStringSync(manifest.toString());
      expect(reachedEnd, isTrue, reason: 'Конец страницы не достигнут');
      // ЗАМЕНИТЬ на маркер последнего раздела экрана своего приложения.
      expect(find.text('Item 39'), findsOneWidget);
      await tester.pumpWidget(const SizedBox.shrink());
    }
  });
}
```

Результат: `ui-report/shots/screen_390x844-00.png`, `...-01.png` и так далее вплоть до последнего положения; аналогично для ширины 1280. Файл `screen_390x844.tsv` содержит смещения, размер видимой области и `extentAfter`; успешный тест требует **нулевого остатка** и видимого маркера конца. Ограничение 60 кадров предотвращает бесконечную прокрутку: при необходимости увеличить его после выяснения причины. Если контент догружается бесконечно, задать конечную тестовую фикстуру и явный признак завершения. Кадры показывают **всю прокрученную страницу в совокупности**, включая виртуализованные списки; `toImage` само по себе захватывает только *текущий* отрисованный viewport. При требованиях к единому «длинному PNG» кадры можно склеить по `offset` из TSV, обрезав повторяющиеся закреплённые шапку/нижнюю панель; исходные кадры и TSV всегда сохранять для проверки швов.

Проверить каждый PNG, включая середину и конец, а не только наличие файлов: переполнение, обрезанный текст, пустые изображения, повторяющиеся элементы, некорректные локали, размеры. Для важных состояний (например, тёмной темы) сделать отдельный тест и отдельный префикс. Размеры виджета не доказывают работу на физических устройствах.

## 4. Переносимый CI на GitHub Actions

Пример `.github/workflows/flutter-ui.yml` **в новом репозитории**. `subosito/flutter-action` удобен для GitHub-hosted runner; на self-hosted runner с уже установленным Flutter убрать этот шаг и проверить `flutter --version`. Не добавлять `integration_test` в headless Linux-job без эмулятора.

```yaml
name: Flutter UI tests and screenshots
on:
  push:
    branches-ignore: ['ci/ui-captures'] # исключить цикл публикации отчётов
  workflow_dispatch:
permissions:
  contents: write # только если включена опциональная публикация в ветку ниже
jobs:
  test-and-capture:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: subosito/flutter-action@v2
        with:
          channel: stable
          cache: true
      - name: Tests and PNG
        shell: bash
        run: |
          mkdir -p ui-report/shots
          set -o pipefail
          status=0
          flutter --version 2>&1 | tee ui-report/flutter-version.log || status=1
          flutter pub get 2>&1 | tee ui-report/pub-get.log || status=1
          if (( status == 0 )); then
            flutter analyze 2>&1 | tee ui-report/analyze.log || status=1
            flutter test --reporter expanded 2>&1 | tee ui-report/tests.log || status=1
            UI_SHOT_DIR="$PWD/ui-report/shots" \
              flutter test visual_test/ui_capture_test.dart --reporter expanded \
              2>&1 | tee ui-report/capture.log || status=1
          fi
          shopt -s nullglob
          pngs=(ui-report/shots/*.png)
          if ((${#pngs[@]} == 0)); then
            echo '::error::Нет PNG — проверить capture.log и тест'
            status=1
          fi
          exit "$status"
      - name: Run metadata and SHA-256
        if: always()
        shell: bash
        run: |
          python3 - <<'PY'
          import hashlib
          import os
          from pathlib import Path
          base = Path('ui-report')
          base.mkdir(exist_ok=True)
          (base / 'RUN.txt').write_text(
              f"run={os.environ['GITHUB_RUN_ID']}\n"
              f"source_sha={os.environ['GITHUB_SHA']}\n"
              f"source_ref={os.environ['GITHUB_REF']}\n"
          )
          files = sorted(p for p in base.rglob('*')
                         if p.is_file() and p.name != 'SHA256SUMS.txt')
          (base / 'SHA256SUMS.txt').write_text(''.join(
              f'{hashlib.sha256(p.read_bytes()).hexdigest()}  '
              f'{p.relative_to(base).as_posix()}\n' for p in files
          ))
          PY
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: flutter-ui-${{ github.run_id }}
          path: ui-report/
          if-no-files-found: warn
```

При ошибках тестов job остаётся красным, но `if: always()` сохраняет логи и уже полученные PNG. Если CI должен проверять только часть тестов, изменить `flutter test` на конкретные файлы/папки. Закрепить используемую версию Flutter, если стабильность golden/screenshot изображений критична. На self-hosted runner загрузка Flutter через action требует доступ к внешним серверам Flutter; при закрытой сети используется заранее установленный SDK.

Получение обычного artifact (в среде, где работает его скачивание):

```bash
# Выполнять в каталоге целевого Flutter-репозитория; gh уже авторизован.
gh run list --workflow flutter-ui.yml --limit 5
RUN_ID=<номер-нового-прогона>
gh run view "$RUN_ID" --log-failed
gh run download "$RUN_ID" -n "flutter-ui-$RUN_ID" -D .ui-report-downloaded
find .ui-report-downloaded -type f | sort
(cd .ui-report-downloaded && sha256sum -c SHA256SUMS.txt)
```

Архив Actions artifacts может загружаться с другого хоста через перенаправление. Если текущая сеть не позволяет скачать его через `gh run download`, это не означает, что тесты или публикация сломались: используйте независимый способ через **GitHub API** ниже. Номер прогона брать из `gh run list` / CI, не угадывать.

## 5. Опциональный канал через `api.github.com`, если artifacts недоступны

Следующий шаг добавить **только когда он нужен**, в конец того же job после `upload-artifact`. Он помещает последнюю подборку отчётов в выделенную ветку **целевого** репозитория `ci/ui-captures`. Отчёт перезаписывается очередным успешным захватом (а не только при зелёных unit-тестах); всегда сверять `run` и `source_sha` из `RUN.txt`. Не использовать рабочую ветку или хранилище релизов для временных PNG. На форках/недоверенных PR отключать запись; этот workflow запускается только по `push`/`workflow_dispatch`. Репозиторий должен разрешать workflow запись `contents: write` и создание/обновление выделенной **незащищённой** ветки. Если политики запрещают это, оставить только artifacts или использовать согласованное внешнее хранилище. Не печатать токен и не сохранять его в файлах.

```yaml
      - name: Optional API-readable report branch
        if: always() && github.event_name != 'pull_request'
        shell: bash
        env:
          GITHUB_TOKEN: ${{ github.token }}
        run: |
          set -euo pipefail
          shopt -s nullglob
          pngs=(ui-report/shots/*.png)
          if ((${#pngs[@]} == 0)); then
            echo 'No screenshots; keeping previous report branch unchanged.'
            exit 0
          fi
          work="$(mktemp -d)"
          trap 'rm -rf "$work"' EXIT
          cp -a ui-report/. "$work/"
          (
            cd "$work"
            git init -q -b ci/ui-captures
            git config user.name 'github-actions[bot]'
            git config user.email '41898282+github-actions[bot]@users.noreply.github.com'
            git add -A
            git commit -qm "UI run ${GITHUB_RUN_ID} source ${GITHUB_SHA}"
            # Токен только в переменной окружения, не в URL или конфиге Git.
            git -c credential.helper= \
              -c 'credential.helper=!f() { printf "username=x-access-token\npassword=%s\n" "$GITHUB_TOKEN"; }; f' \
              push --force --quiet "https://github.com/${GITHUB_REPOSITORY}.git" \
              HEAD:refs/heads/ci/ui-captures
          )
```

Ветка с PNG разрастается при частых прогонах даже при `--force` (старые объекты удаляются сервером не сразу). Хранить только компактные, обезличенные отчёты и при необходимости отключать шаг. Доступ к ветке частного репозитория регулируется правами GitHub; checksums проверяют целостность скачанного, **не** заменяют доверие к тому, кто имеет право переписать ветку.

### Чтение этой ветки только через GitHub API

Сохранить следующий **самостоятельный** скрипт как `tools/fetch_ui_report.py` в целевом репозитории или во внешнем каталоге. Требуются Python 3 и авторизованный `gh` с доступом к целевому репозиторию. Он не обращается к домену хранилища Actions artifacts. Проверяет наличие полного дерева, размеры blob, SHA-256 *каждого* файла по `SHA256SUMS.txt` и (при `--run`) номер прогона; при неполном отчёте завершится с ошибкой.

```python
#!/usr/bin/env python3
import argparse
import base64
import hashlib
import json
import subprocess
from pathlib import Path, PurePosixPath


def gh(*args):
    return subprocess.check_output(['gh', *args])


def api(path):
    return json.loads(gh('api', path))


def blob(repo, sha):
    path = f'repos/{repo}/git/blobs/{sha}'
    response = api(path)
    if response.get('encoding') != 'base64' or 'content' not in response:
        raise RuntimeError(f'GitHub blob has unsupported format: {sha}')
    return base64.b64decode(response['content'])


def safe_path(text):
    value = PurePosixPath(text)
    if not text or text.startswith('/') or any(part in ('..', '.') for part in value.parts):
        raise ValueError(f'Unsafe report path: {text!r}')
    return value


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repo', help='owner/repo; default: current gh repository')
    parser.add_argument('--branch', default='ci/ui-captures')
    parser.add_argument('--run', help='require this GitHub Actions run ID')
    parser.add_argument('--out', default='.ui-report-downloaded')
    args = parser.parse_args()
    repo = args.repo or gh('repo', 'view', '--json', 'nameWithOwner',
                           '--jq', '.nameWithOwner').decode().strip()
    if len(repo.split('/')) != 2:
        raise ValueError('Specify --repo owner/repo')

    head = api(f'repos/{repo}/branches/{args.branch}')['commit']['sha']
    tree_sha = api(f'repos/{repo}/git/commits/{head}')['tree']['sha']
    tree = api(f'repos/{repo}/git/trees/{tree_sha}?recursive=1')
    if tree.get('truncated'):
        raise RuntimeError('Report tree truncated; cannot verify completeness')
    files = {entry['path']: entry for entry in tree['tree']
             if entry['type'] == 'blob'}
    if not {'RUN.txt', 'SHA256SUMS.txt'} <= files.keys():
        raise RuntimeError('Missing report metadata or SHA256SUMS.txt')

    manifest = blob(repo, files['SHA256SUMS.txt']['sha']).decode('utf-8')
    expected = {}
    for line in manifest.splitlines():
        digest, sep, filename = line.partition('  ')
        if not sep or len(digest) != 64 or filename in expected:
            raise ValueError(f'Bad checksum record: {line!r}')
        safe_path(filename)
        expected[filename] = digest
    if set(expected) != set(files) - {'SHA256SUMS.txt'}:
        raise RuntimeError('Manifest does not describe every report file')
    if not any(path.endswith('.png') for path in expected):
        raise RuntimeError('No screenshots in report')

    run = blob(repo, files['RUN.txt']['sha']).decode('utf-8')
    if args.run and f'run={args.run}' not in run.splitlines():
        raise RuntimeError(f'Report is for another run, not {args.run}')

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    for name, entry in sorted(files.items()):
        safe_path(name)
        data = (manifest.encode('utf-8') if name == 'SHA256SUMS.txt'
                else blob(repo, entry['sha']))
        if len(data) != entry['size']:
            raise RuntimeError(f'Length mismatch: {name}')
        if name != 'SHA256SUMS.txt':
            actual = hashlib.sha256(data).hexdigest()
            if actual != expected[name]:
                raise RuntimeError(f'SHA-256 mismatch: {name}')
        dest = out.joinpath(*PurePosixPath(name).parts)
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
        print(dest)
    print(f'Commit {head}; {run.strip()}')


if __name__ == '__main__':
    main()
```

Пример использования (в любом месте с `gh` и сетью к `api.github.com`):

```bash
python3 tools/fetch_ui_report.py --repo OWNER/FLUTTER_REPO --run "$RUN_ID" \
  --out .ui-report-downloaded
# Теперь открыть все .ui-report-downloaded/shots/*.png и соответствующие *.tsv.
```

Заменить `OWNER/FLUTTER_REPO` на фактическое имя нового репозитория. Если ветка содержит другой `run`, не выдавать её кадры за результат нового CI: дождаться публикации, проверить логи шага и права на запись. При большом числе кадров/больших PNG предпочесть artifacts: GitHub API для Git blobs не предназначен для неограниченного хранилища.

## 6. При необходимости — скриншоты на Android/iOS (отдельный прогон)

Добавить в `pubspec.yaml`:

```yaml
dev_dependencies:
  flutter_test:
    sdk: flutter
  integration_test:
    sdk: flutter
```

Либо выполнить `flutter pub add 'dev:integration_test:{sdk: flutter}'`, если версия CLI поддерживает эту запись. Создать `integration_test/ui_device_test.dart` (заменить импорт приложения, маршрут, ключ и маркер конца):

```dart
import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:YOUR_PACKAGE/main.dart' as app; // заменить YOUR_PACKAGE

void main() {
  final binding = IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  testWidgets('device page from top through bottom', (tester) async {
    app.main(); // если запуск асинхронный — дождаться состояния готовности
    await tester.pumpAndSettle();
    if (defaultTargetPlatform == TargetPlatform.android) {
      await binding.convertFlutterSurfaceToImage();
      await tester.pumpAndSettle(); // обязательный кадр после переключения Surface
    }
    final target = find.byKey(const ValueKey('capture-scroll'));
    expect(target, findsOneWidget);
    final scrollable = find.descendant(
      of: target,
      matching: find.byType(Scrollable),
    ).first;
    var done = false;
    for (var i = 0; i < 60; i++) {
      final position = tester.state<ScrollableState>(scrollable).position;
      await binding.takeScreenshot('device-${i.toString().padLeft(2, '0')}');
      if (position.extentAfter <= 1) {
        done = true;
        break;
      }
      final next = (position.pixels + position.viewportDimension * 0.75)
          .clamp(position.minScrollExtent, position.maxScrollExtent)
          .toDouble();
      expect(next, greaterThan(position.pixels + 0.01));
      position.jumpTo(next);
      await tester.pumpAndSettle();
    }
    expect(done, isTrue);
    // Заменить на проверку видимого последнего элемента реального экрана.
    expect(find.text('LAST_SECTION_OF_YOUR_APP'), findsOneWidget);
  });
}
```

Сохранение PNG **на хосте**, а не только в памяти приложения: `test_driver/ui_screenshot_driver.dart`:

```dart
import 'dart:io';
import 'package:integration_test/integration_test_driver_extended.dart';

Future<void> main() async {
  await integrationDriver(
    onScreenshot: (String name, List<int> bytes,
        [Map<String, Object?>? args]) async {
      final dir = Directory(
        Platform.environment['UI_SHOT_DIR'] ?? 'ui-report/device-shots',
      )..createSync(recursive: true);
      File('${dir.path}/$name.png').writeAsBytesSync(bytes, flush: true);
      return bytes.isNotEmpty;
    },
  );
}
```

```bash
flutter devices
# Android: предварительно поднять эмулятор или подключить устройство.
UI_SHOT_DIR="$PWD/ui-report/device-shots" flutter drive \
  -d emulator-5554 \
  --driver=test_driver/ui_screenshot_driver.dart \
  --target=integration_test/ui_device_test.dart
```

Проверить доступный device ID командой `flutter devices`, а не копировать `emulator-5554` вслепую. iOS требует macOS/Xcode runner. Для Web нужен установленный и запущенный драйвер браузера, соответствующий версии браузера; пользоваться инструкцией Flutter для `flutter drive -d web-server`. Эти прогоны идут **в дополнение**, а не вместо быстрых widget-тестов; в отдельном device job также загружать device PNG и логи artifact и/или выбранным каналом отчётов. Android-вызов `convertFlutterSurfaceToImage()` нужен до `takeScreenshot`, а в widget-тесте не нужен.

## 7. Контрольный список для агента перед отчётом пользователю

- [ ] Указано, какая версия Flutter и какой проект реально тестировались; если тест запускался только в CI, сказано прямо.
- [ ] `flutter analyze`, unit/widget-тесты и screenshot-тест запущены, exit codes и логи доступны. Ошибки не скрыты успешной загрузкой artifact.
- [ ] PNG созданы для каждой оговорённой ширины/темы/экрана; несколько перекрывающихся кадров доходят до конца (`extentAfter <= 1` и маркер конца).
- [ ] Открыты **первый, средние и последний** кадры, просмотрены длинные страницы, а не только их файлы/первые экраны.
- [ ] При скачивании из ветки совпали номер прогона, исходный commit, размеры и SHA-256. При отсутствии нового отчёта старый результат не выдан за новый.
- [ ] Widget PNG названы headless Flutter-рендерингом; реальные Android/iOS/Web проверки отмечены отдельно, только если они действительно запускались.
- [ ] PNG/логи доступны в указанном каталоге и/или по конкретному artifact/run; в них нет секретов и персональных данных.

Справка: [Flutter widget tests](https://docs.flutter.dev/cookbook/testing/widget/introduction), [RenderRepaintBoundary.toImage](https://api.flutter.dev/flutter/rendering/RenderRepaintBoundary/toImage.html), [размер тестового FlutterView](https://api.flutter.dev/flutter/flutter_test/TestFlutterView/physicalSize.html), [Flutter integration tests](https://docs.flutter.dev/testing/integration-tests), [README `integration_test`: скриншоты и extended driver](https://github.com/flutter/flutter/blob/main/packages/integration_test/README.md).
