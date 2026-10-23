namespace Mav.ControlsGalleryMarkup.Localization;

internal static class RuPickers
{
    public static readonly (string Key, string Value)[] Items =
    [
        ("Pickers.Fields.Section", "Поля с выбором"),
        ("Pickers.Fields.Section.Body", "Готовые поля, которые показывают выбранное значение и открывают соответствующий выбор."),
        ("Pickers.Fields.Card.Body", "Встроенные поля делают форму компактнее; их можно очищать и проверять через MavFormValidator."),
        ("Pickers.Field.Due", "Срок"),
        ("Pickers.Field.Sprint", "Спринт"),
        ("Pickers.Field.Reminder", "Напоминание"),
        ("Pickers.Embedded.Section", "Встроенные выборы"),
        ("Pickers.Embedded.Section.Body", "Календарную сетку, циферблат времени и палитру цвета можно встроить в любой макет."),
        ("Pickers.Embedded.Card.Body", "Встраивайте выбор напрямую, когда диалог или шторка не подходят."),
        ("Pickers.Modal.Section", "Модальные выборы"),
        ("Pickers.Modal.Section.Body", "Асинхронные помощники возвращают выбранное значение или null, если выбор отменён."),
        ("Pickers.Modal.Card.Body", "Статические помощники используют текущий верхний уровень и возвращают null, если пользователь закрыл оверлей."),
        ("Pickers.Modal.Date", "Выбрать дату"),
        ("Pickers.Modal.Range", "Выбрать период"),
        ("Pickers.Modal.Time", "Выбрать время"),
        ("Pickers.Modal.Color", "Выбрать цвет"),
        ("Pickers.Modal.None", "Пока ничего не выбрано"),
        ("Pickers.Result.DateDismissed", "Дата не выбрана"),
        ("Pickers.Result.Date", "Дата: {0:dd MMM yyyy}"),
        ("Pickers.Result.RangeDismissed", "Период не выбран"),
        ("Pickers.Result.Range", "Период: {0:dd MMM} – {1:dd MMM}"),
        ("Pickers.Result.TimeDismissed", "Время не выбрано"),
        ("Pickers.Result.Time", "Время: {0:hh\\:mm}"),
        ("Pickers.Result.ColorTitle", "Акцентный цвет"),
        ("Pickers.Result.ColorDismissed", "Цвет не выбран"),
        ("Pickers.Result.Color", "Цвет: {0}"),
    ];
}
