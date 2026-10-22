namespace Mav.ControlsGallery.Localization;

internal static class RuInputs
{
    public static readonly (string Key, string Value)[] Items =
    [
        ("Inputs.Fields.Section", "Текстовые поля"),
        ("Inputs.Fields.Section.Body", "Контурные и заполненные поля с иконками, режимом пароля, кнопкой очистки, счётчиком и ошибками."),
        ("Inputs.Fields.Card.Body", "Однострочное поле с вариантами, иконками слева и справа, показом пароля, кнопкой очистки, счётчиком и состоянием ошибки."),
        ("Inputs.Field.Name", "Имя"),
        ("Inputs.Field.Email", "Почта"),
        ("Inputs.Field.Email.Support", "Мы никогда ею не делимся"),
        ("Inputs.Field.Password", "Пароль"),
        ("Inputs.Field.ApiKey", "API-ключ"),
        ("Inputs.Field.ApiKey.Error", "Ключ должен содержать 32 символа"),
        ("Inputs.Field.Code", "Код"),
        ("Inputs.Other.Section", "Многострочный и структурированный ввод"),
        ("Inputs.Other.Section.Body", "Многострочные поля, ячейки PIN/OTP, числовые степперы, теги и поиск."),
        ("Inputs.Other.Card.Body", "Многострочный редактор, ячейки OTP, числовой степпер, ввод тегов и строка поиска с панелью результатов."),
        ("Inputs.Area.Label", "Заметки"),
        ("Inputs.Area.Placeholder", "Напишите заметку к релизу…"),
        ("Inputs.Area.Support", "Приложение может использовать Markdown"),
        ("Inputs.Number.Label", "Места"),
        ("Inputs.Number.Suffix", "человек"),
        ("Inputs.Tags.Label", "Теги"),
        ("Inputs.Search.Placeholder", "Поиск по галерее"),
        ("Inputs.Validation.Section", "Валидация"),
        ("Inputs.Validation.Section.Body", "MavFormValidator связывает правила с полями и записывает ErrorText и HasError."),
        ("Inputs.Validation.Card.Body", "Правила: Required, MinLength, MaxLength, Email, Numeric, Pattern, Matches и Must."),
        ("Inputs.Form.Name", "Полное имя"),
        ("Inputs.Form.City", "Город"),
        ("Inputs.Form.Validate", "Проверить"),
        ("Inputs.Form.Status", "Отправьте форму для проверки"),
        ("Inputs.Form.Valid", "Все поля заполнены верно."),
    ];
}
