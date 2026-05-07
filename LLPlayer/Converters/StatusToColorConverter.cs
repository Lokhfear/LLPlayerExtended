using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LLPlayer.Models;

namespace LLPlayer.Converters;

/// <summary>
/// Конвертер статуса обучения в цвет фона.
/// </summary>
public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (LearningStatus)value switch
        {
            LearningStatus.New      => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            LearningStatus.Learning => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            LearningStatus.Learned  => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            LearningStatus.Ignored  => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
            LearningStatus.Archived => new SolidColorBrush(Color.FromRgb(121, 85, 72)),
            _                       => Brushes.Gray
        };

    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>
/// Конвертер булева значения (избранное) в имя иконки.
/// </summary>
public class BoolToStarIconConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? "Star" : "StarOutline";

    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>
/// Конвертер типа элемента в label (W/P/S).
/// </summary>
public class ItemTypeLabelConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (ItemType)value switch
        {
            ItemType.Word     => "W",
            ItemType.Phrase   => "P",
            ItemType.Sentence => "S",
            _                 => "?"
        };

    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}
