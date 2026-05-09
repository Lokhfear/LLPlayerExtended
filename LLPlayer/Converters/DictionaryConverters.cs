using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LLPlayer.Converters
{
    /// <summary>
    /// Конвертер — null или пустая строка → Collapsed, иначе → Visible.
    /// Используется для скрытия поля перевода предложения в карточке.
    /// </summary>
    [ValueConversion(typeof(string), typeof(Visibility))]
    public sealed class NullOrEmptyToHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Инвертированный BooleanToVisibility.
    /// True → Collapsed, False → Visible.
    /// Используется для показа заглушки "Словарь пуст" когда IsEmpty = true.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// BooleanToVisibility с поддержкой параметра "Inverse".
    /// Используется в DictionaryControl для обоих состояний (пустой/непустой список).
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool bv && bv;

            // Поддержка инверсии через ConverterParameter="Inverse"
            if (parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                b = !b;

            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool vis = value is Visibility v && v == Visibility.Visible;
            if (parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                vis = !vis;
            return vis;
        }
    }

    /// <summary>
    /// Инвертор булева значения.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;
    }
}
