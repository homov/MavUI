# Облачный API: соответствие операций и методов

Источник контракта — код: `ref/HC.Api` (актуальная реализация) и `ref/HeatController6/src/HeatCeram.Api`
(старая). OpenAPI-спеки (`support/API*.yaml`) не используются: они устарели и неверны.

Клиент: `Hc.Services.Cloud.CloudClient` (`CloudHttpClient.Instance` → `CloudClient.Instance`).
Запросы идут по одному на экземпляр клиента (очередь). Ошибки возвращаются в `ApiResponse<T>`,
кроме отмены, запрошенной вызывающим кодом. Тело запроса: form (`application/x-www-form-urlencoded`),
кроме `update_schedule` (JSON). Базовый адрес: `https://api.heatceram.com.ua/stinexapiv1/` (продакшен,
переопределяется `HC_API_BASE`).

Статус «проверено тестом» — есть тест в `HC.Tests/CloudApiContractTests.cs` или `PasswordEncrypterTests.cs`.

| Операция (`CloudClient`) | Маршрут | Запрос | Ответ | Тест |
|---|---|---|---|---|
| `Login` | `user_authorization/` | `LoginRequest` (email, password) | `LoginResponse` (uuid → UserId) | да |
| `Register` | `user_registration/` | `RegisterRequest` (email, password, timezone, language) | `BaseResponse` | нет |
| `UserDelete` | `user_delete/` | — | `BaseResponse` | нет |
| `ValidateEmail` | `user_validation/` | `ValidateEmailRequest` (email, code) | `BaseResponse` | нет |
| `QueryRestorePassword` | `user_password_restoration/` | `QueryRestorePasswordRequest` (email) | `BaseResponse` | нет |
| `RestorePassword` | `password_restoration_validate/` | `RestorePasswordRequest` (email, password, code) | `BaseResponse` | нет |
| `ChangePassword` | `password_change/` | `ChangePasswordRequest` (email, password, new_password) | `BaseResponse` | да |
| `GetUserSettings` | `get_user_settings/` | `GetUserSettingsRequest` (email) | `GetUserSettingsResponse` (timezone) | нет |
| `SetTimezone` | `change_timezone/` | `SetTimezoneRequest` (timezone) | `BaseResponse` | нет |
| `GetTimezones` | `get_timezones/` | — | `GetTimezonesResponse` (`Names`) | да |
| `GetUserDevices` | `get_user_devices/` | — | `DevicesResponse` | да |
| `GetUserGroups` | `get_user_groups/` | — | `GetUserGroupsResponse` | да |
| `GetGroupDevices` | `get_group_devices/` | `GroupRequest` (group_id) | `DevicesResponse` | нет |
| `RemoveGroup` | `remove_group/` | `GroupRequest` (group_id) | `DevicesResponse` | нет |
| `CreateGroup` | `create_new_group/` | `CreateGroupRequest` (group_name) | `BaseResponse` | нет |
| `SetGroupName` | `set_group_name/` | `SetGroupNameRequest` (group_id, group_name) | `BaseResponse` | нет |
| `SetGroupSensor` | `set_group_sensor/` | `SetGroupSensorRequest` (group_id, sensor_id) | `BaseResponse` | нет |
| `GetSchedule` | `get_schedule_by_id/` | `ScheduleRequest` (schedule_id) | `GetScheduleResponse` (`ScheduleInfo`) | нет |
| `SetScheduleType` | `update_schedule_type/` | `SetScheduleTypeRequest` (schedule_id, schedule_type) | `BaseResponse` | нет |
| `UpdateSchedule` | `update_schedule/` | `ScheduleInfo` (JSON) | `BaseResponse` | да |
| `SetDeviceHeatState` | `set_heat_state/` | `SetDeviceHeatStateRequest` (device_id, state) | `BaseResponse` | нет |
| `SetDeviceName` | `set_device_name/` | `SetDeviceNameRequest` (device_id, name) | `BaseResponse` | нет |
| `SetDeviceGroup` | `set_device_group/` | `SetDeviceGroupRequest` (device_id, group_id) | `BaseResponse` | нет |
| `SetDeviceSensor` | `set_device_sensor/` | `SetDeviceSensorRequest` (device_id, sensor_id) | `BaseResponse` | нет |
| `SetAntifreezeMode` | `change_antifreeze_mode/` | `SetAntifreezeModeRequest` (device_id, state: int) | `BaseResponse` | нет |
| `SetHumiMode` | `change_humidity_mode/` | `SetHumidityModeRequest` (device_id, state: int) | `BaseResponse` | нет |
| `SetPanelCoeff` | `change_panel_coef/` | `SetCoefficientRequest` (device_id, value: int) | `BaseResponse` | нет |
| `SetRoomCoeff` | `change_room_coef/` | `SetCoefficientRequest` (device_id, value: int) | `BaseResponse` | нет |
| `SetPanelTemp` | `set_panel_temp/` | `SetTemperatureRequest` (device_id, temp: int) | `BaseResponse` | нет |
| `SetHysteresis` | `change_gisteresis/` | `SetTemperatureRequest` (device_id, temp: int) | `BaseResponse` | нет |
| `DeleteDevice` | `remove_device/` | `DeviceRequest` (device_id) | `BaseResponse` | нет |
| `GetDeviceTypes` | `get_device_types/` | — | `GetDeviceTypesResponse` | нет |
| `GetDeviceInfo` | `get_device_info_by_id/` | `DeviceRequest` (device_id) | `UserDeviceInfo` | да |
| `GetErrorCodes` | `error_codes/` | `ErrorCodesRequest` (language) | `ErrorCodesResponse` | нет |
| `GetAllErrorCodes` | `get_all_error_codes/` | — | `AllErrorCodesResponse` | да |

Всего 35 операций. Не переносим: строковые варианты (`...AsString`, для отладки) и `GetDeviceInfoString`.

## Соответствие именам в старом коде

Старые имена из `ref/HeatController6` и `ref/HC.Api` сохранены, кроме:

- `Login` без параметра `language` (решение: как в `ref/HC.Api`). Параметр нужен ТЗ 2025, которое отложено.
- `UserDelete`, `SetHumiMode`, `SetPanelCoeff`, `SetRoomCoeff`, `SetPanelTemp`, `SetHysteresis` — имена из `ref/HC.Api`.

## Модели

Все модели лежат в `Models/Cloud/`. Поля JSON совпадают с сервером (`JsonPropertyName`). Вычисляемые
свойства (`ImageUrl`, `Formatted*`, `Has*`) в DTO не переносятся: их место — слой ViewModel.
Расписание: `ScheduleInfo.FromTemperature` (тип `manual`) и `FromHumidity` (тип `humidity`).
