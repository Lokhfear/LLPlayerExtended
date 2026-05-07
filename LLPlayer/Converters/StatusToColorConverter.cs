using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LLPlayer.Models;

namespace LLPlayer.Converters;

/// <summary>
/// Converts LearningStatus to a color brush for visual indicators
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is LearningStatus status ? status switch
        {
            LearningStatus.New      => new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Blue
            LearningStatus.Learning => new SolidColorBrush(Color.FromRgb(255, 152, 0)),    // Orange
            LearningStatus.Learned  => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
            LearningStatus.Ignored  => new SolidColorBrush(Color.FromRgb(158, 158, 158)),  // Gray
            LearningStatus.Archived => new SolidColorBrush(Color.FromRgb(121, 85, 72)),    // Brown
            _                       => Brushes.Gray
        } : Brushes.Gray;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean to star icon name (Star/StarOutline)
/// </summary>
public class BoolToStarIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "Star" : "StarOutline";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts ItemType to a short label (W/P/S)
/// </summary>
public class ItemTypeLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is ItemType type ? type switch
        {
            ItemType.Word     => "W",
            ItemType.Phrase   => "P",
            ItemType.Sentence => "S",
            _                 => "?"
        } : "?";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts Unix timestamp (ms) to formatted date string
/// </summary>
public class TimestampToDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long ts && ts > 0)
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(ts).DateTime;
            return dt.ToString("yyyy-MM-dd HH:mm");
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts nullable double timestamp to formatted time string (hh:mm:ss)
/// </summary>
public class SecondsToTimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double secs && secs > 0)
        {
            return TimeSpan.FromSeconds(secs).ToString(@"hh\:mm\:ss");
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
