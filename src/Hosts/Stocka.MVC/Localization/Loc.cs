using System.Globalization;

namespace Stocka.MVC.Localization;

/// <summary>
/// Reads a key out of the single <see cref="S"/> table and returns the text
/// for the request's current culture. Views and controllers just call
/// <c>Loc.T("Nav_Home")</c> — no culture checks sprinkled around the app.
/// </summary>
public static class Loc
{
    /// <summary>Two-letter language of the current UI culture ("ar" / "en").</summary>
    public static string Language => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    /// <summary>True when the current UI culture is right-to-left.</summary>
    public static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

    /// <summary>The culture to switch to from the current one.</summary>
    public static string OtherLanguage => IsRtl ? "en" : "ar";

    /// <summary>Label shown on the language switch for the current culture.</summary>
    public static string OtherLanguageLabel => IsRtl ? "EN" : "عربي";

    /// <summary>Text for a key in the current culture. Unknown key returns the key itself.</summary>
    public static string T(string key)
    {
        if (!S.All.TryGetValue(key, out var entry))
            return key;

        return IsRtl ? entry.Ar : entry.En;
    }

    /// <summary>Text for a key with <see cref="string.Format(string, object[])"/> placeholders.</summary>
    public static string F(string key, params object?[] args) => string.Format(T(key), args);
}
