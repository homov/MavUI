# Рабочие заметки

Обновлено: 23 октября 2026. Ветка: `arena/7784b16e-mavui`.

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
