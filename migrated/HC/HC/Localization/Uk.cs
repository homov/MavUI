namespace Hc.Localization;

/// <summary>Ukrainian strings.</summary>
internal static class Uk
{
    public static readonly IReadOnlyDictionary<string, string> Strings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["app.name"] = "HC",

        ["startup.checking"] = "Виконуємо вхід...",

        ["error.title"] = "Помилка",

        ["auth.signin.title"] = "Вхід",
        ["auth.signin.button"] = "Увійти",
        ["auth.register.link"] = "Створити акаунт",
        ["auth.restore.link"] = "Забули пароль?",
        ["auth.field.email"] = "Електронна пошта",
        ["auth.field.password"] = "Пароль",
        ["auth.field.password2"] = "Повторіть пароль",
        ["auth.field.newpassword"] = "Новий пароль",
        ["auth.field.remember"] = "Запам'ятати на цьому пристрої",
        ["auth.register.title"] = "Створення акаунта",
        ["auth.register.button"] = "Створити акаунт",
        ["auth.restore.title"] = "Відновлення пароля",
        ["auth.restore.button"] = "Надіслати код",
        ["auth.code.title"] = "Введіть код",
        ["auth.code.hint"] = "Введіть 4-значний код, надісланий на {0}",
        ["auth.code.confirm"] = "Підтвердити",
        ["auth.code.reset"] = "Встановити новий пароль",

        ["auth.error.email"] = "Введіть коректну електронну пошту",
        ["auth.error.password"] = "Введіть пароль",
        ["auth.error.mismatch"] = "Паролі не збігаються",
        ["auth.error.code"] = "Введіть 4-значний код",
        ["auth.error.transport"] = "Немає зв'язку з сервером. Перевірте мережу і спробуйте ще раз.",
        ["auth.error.http"] = "Сервер повернув помилку. Спробуйте пізніше.",
        ["auth.error.auth"] = "Сесія завершилась. Увійдіть знову.",
        ["auth.error.storage"] = "Збереження пароля недоступне на цій системі. Увійдіть без опції «Запам'ятати».",
        ["auth.error.generic"] = "Щось пішло не так. Спробуйте ще раз.",

        ["home.title"] = "HC",
        ["home.placeholder"] = "Групи та пристрої з'являться тут.",
        ["home.signout"] = "Вийти",
    };
}
