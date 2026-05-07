namespace LLPlayer.Models;

/// <summary>
/// Медиа-контекст для элемента обучения.
/// Содержит информацию о видео, из которого был взят элемент.
/// </summary>
public class MediaContext
{
    /// <summary>Название видео (Breaking Bad S01E01)</summary>
    public string? VideoTitle { get; set; }

    /// <summary>Локальный путь к файлу</summary>
    public string? FilePath { get; set; }

    /// <summary>Timestamp в секундах</summary>
    public double? TimestampSeconds { get; set; }

    /// <summary>Форматированный timestamp для отображения (01:25:33)</summary>
    public string? TimestampDisplay => TimestampSeconds.HasValue
        ? TimeSpan.FromSeconds(TimestampSeconds.Value).ToString(@"hh\:mm\:ss")
        : null;

    public bool HasMedia => !string.IsNullOrWhiteSpace(FilePath);
}
