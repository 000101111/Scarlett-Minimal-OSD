using System;
using System.Globalization;
using System.Windows;

namespace ScarlettMonitorOSD;

public static class Localization
{
    private static ResourceDictionary? _fallback;
    private static ResourceDictionary? _translation;
    public static event Action? LanguageChanged;

    public static string Normalize(string? language) => language switch
    {
        "en" => "en",
        "zh-CN" => "zh-CN",
        _ => "auto"
    };

    public static void SetLanguage(string? preference)
    {
        var resources = System.Windows.Application.Current.Resources.MergedDictionaries;
        if (_fallback == null)
        {
            _fallback = Load("en");
            resources.Add(_fallback);
        }
        if (_translation != null)
        {
            resources.Remove(_translation);
            _translation = null;
        }

        string language = Normalize(preference);
        if (language == "auto")
            language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh-CN" : "en";
        if (language != "en")
        {
            _translation = Load(language);
            resources.Add(_translation);
        }
        LanguageChanged?.Invoke();
    }

    private static ResourceDictionary Load(string language) => new()
    {
        Source = new Uri($"/ScarlettMonitorOSD;component/Localization/Strings.{language}.xaml", UriKind.Relative)
    };

    public static string Text(string key) =>
        System.Windows.Application.Current.TryFindResource(key) as string ?? key;

    public static string Duration(double seconds) =>
        string.Format(CultureInfo.CurrentCulture, Text("DurationFormat"), seconds);
}
