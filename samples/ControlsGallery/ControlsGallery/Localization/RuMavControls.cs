using Mav;

namespace Mav.ControlsGallery.Localization;

/// <summary>
/// Russian strings for the library's own controls (date and time pickers, dialogs, menus, data table).
/// Keys come from <see cref="MavLocalization.Keys"/>, so they match what the library asks for.
/// The date-field hint keeps its ISO form: the input is parsed as yyyy-MM-dd.
/// </summary>
internal static class RuMavControls
{
    public static readonly (string Key, string Value)[] Items =
    [
        (MavLocalization.Keys.Ok, "ОК"),
        (MavLocalization.Keys.Cancel, "Отмена"),
        (MavLocalization.Keys.Close, "Закрыть"),
        (MavLocalization.Keys.Back, "Назад"),
        (MavLocalization.Keys.Clear, "Очистить"),
        (MavLocalization.Keys.Search, "Поиск"),
        (MavLocalization.Keys.Select, "Выбрать"),
        (MavLocalization.Keys.SelectDate, "Выберите дату"),
        (MavLocalization.Keys.SelectDateRange, "Выберите период"),
        (MavLocalization.Keys.EnterDate, "Введите дату"),
        (MavLocalization.Keys.EnterDateRange, "Введите даты"),
        (MavLocalization.Keys.SwitchToInput, "Перейти к вводу текста"),
        (MavLocalization.Keys.SwitchToCalendar, "Перейти к календарю"),
        (MavLocalization.Keys.StartDate, "Начальная дата"),
        (MavLocalization.Keys.EndDate, "Конечная дата"),
        (MavLocalization.Keys.DateRangeSeparator, "\u2013"),
        (MavLocalization.Keys.InvalidDate, "Неверный формат даты"),
        (MavLocalization.Keys.InvalidRange, "Конечная дата не может быть раньше начальной"),
        (MavLocalization.Keys.PreviousMonth, "Предыдущий месяц"),
        (MavLocalization.Keys.NextMonth, "Следующий месяц"),
        (MavLocalization.Keys.SelectYear, "Выберите год"),
        (MavLocalization.Keys.SelectTime, "Выберите время"),
        (MavLocalization.Keys.EnterTime, "Введите время"),
        (MavLocalization.Keys.Hour, "Часы"),
        (MavLocalization.Keys.Minute, "Минуты"),
        (MavLocalization.Keys.Am, "ДП"),
        (MavLocalization.Keys.Pm, "ПП"),
        (MavLocalization.Keys.SwitchToClock, "Перейти к циферблату"),
        (MavLocalization.Keys.SwitchToTextInput, "Перейти к вводу текста"),
        (MavLocalization.Keys.ShowPassword, "Показать пароль"),
        (MavLocalization.Keys.HidePassword, "Скрыть пароль"),
        (MavLocalization.Keys.OpenMenu, "Открыть меню"),
        (MavLocalization.Keys.CloseDrawer, "Закрыть панель навигации"),
        (MavLocalization.Keys.MoreOptions, "Дополнительно"),
        (MavLocalization.Keys.Expand, "Развернуть"),
        (MavLocalization.Keys.Collapse, "Свернуть"),
        (MavLocalization.Keys.Delete, "Удалить"),
        (MavLocalization.Keys.Dismiss, "Закрыть"),
        (MavLocalization.Keys.Undo, "Отменить"),
        (MavLocalization.Keys.Redo, "Повторить"),
        (MavLocalization.Keys.Cut, "Вырезать"),
        (MavLocalization.Keys.Copy, "Копировать"),
        (MavLocalization.Keys.Paste, "Вставить"),
        (MavLocalization.Keys.SelectAll, "Выбрать всё"),
        (MavLocalization.Keys.Retry, "Повторить"),
        (MavLocalization.Keys.ClearSelection, "Снять выделение"),
        (MavLocalization.Keys.PreviousPage, "Предыдущая страница"),
        (MavLocalization.Keys.NextPage, "Следующая страница"),
        (MavLocalization.Keys.SelectRow, "Выбрать строку"),
        (MavLocalization.Keys.SelectedCountFormat, "Выбрано: {0}"),
        (MavLocalization.Keys.PageRangeFormat, "{0}–{1} из {2}"),
        (MavLocalization.Keys.EmptyPageRange, "Нет элементов"),
    ];
}
