using System;
using Microsoft.UI.Xaml.Data;
using Jot.Services;

namespace Jot.Converters
{
    public class LocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string key && !string.IsNullOrEmpty(key))
            {
                return LocalizationService.Instance.GetString(key);
            }

            if (value is string stringValue)
            {
                return LocalizationService.Instance.GetString(stringValue);
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}