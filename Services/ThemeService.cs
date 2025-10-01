using Microsoft.UI.Xaml;
using System;
using Windows.Storage;

namespace Jot.Services
{
    public class ThemeService
    {
        private const string ThemeSettingKey = "AppTheme";

        public static ElementTheme GetSavedTheme()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(ThemeSettingKey, out var themeValue))
            {
                if (Enum.TryParse<ElementTheme>(themeValue.ToString(), out var theme))
                {
                    return theme;
                }
            }
            return ElementTheme.Default;
        }

        public static void SetTheme(ElementTheme theme)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[ThemeSettingKey] = theme.ToString();

            if (App.MainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }

        public static void ApplyTheme()
        {
            var savedTheme = GetSavedTheme();
            SetTheme(savedTheme);
        }
    }
}