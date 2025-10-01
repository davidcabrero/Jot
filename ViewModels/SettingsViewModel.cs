using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jot.Services;
using Microsoft.UI.Xaml;

namespace Jot.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ElementTheme selectedTheme;

        public SettingsViewModel()
        {
            SelectedTheme = ThemeService.GetSavedTheme();
        }

        [RelayCommand]
        private void ChangeTheme(ElementTheme theme)
        {
            SelectedTheme = theme;
            ThemeService.SetTheme(theme);
        }
    }
}