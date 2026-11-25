using System.Globalization;

namespace Stocka.MVC.Localization;

/// <summary>
/// Reads the request's current culture. UI text is written inline next to where
/// it is rendered; this class only exposes the language state used by the shell
/// and the language-switch flow.
/// </summary>
public static class Loc
{
    /// <summary>Two-letter language of the current UI culture ("ar" / "en").</summary>
    public static string Language => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    /// <summary>True when the current UI culture is right-to-left.</summary>
    public static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

    /// <summary>
    /// True only when the current UI culture is English. Anything else
    /// (Arabic, missing or unknown) is treated as Arabic, the default.
    /// Views use this to pick the inline text instead of a dictionary.
    /// </summary>
    public static bool IsEn => Language == "en";

    /// <summary>The culture to switch to from the current one.</summary>
    public static string OtherLanguage => IsRtl ? "en" : "ar";

    /// <summary>Label shown on the language switch for the current culture.</summary>
    public static string OtherLanguageLabel => IsRtl ? "EN" : "عربي";
}
