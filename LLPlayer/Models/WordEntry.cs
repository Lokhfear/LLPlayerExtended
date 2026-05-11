using System;

namespace LLPlayer.Models;

/// <summary>
/// Сущность слова в словаре пользователя.
/// </summary>
public class WordEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Нормализованное слово (lowercase, без знаков препинания по краям)
    /// </summary>
    public string Word { get; set; } = string.Empty;

    /// <summary>
    /// Перевод слова (заполняется асинхронно)
    /// </summary>
    public string? Translation { get; set; }

    /// <summary>
    /// Предложение из субтитров, в котором встретилось слово
    /// </summary>
    public string Sentence { get; set; } = string.Empty;

    /// <summary>
    /// Перевод предложения (опционально)
    /// </summary>
    public string? SentenceTranslation { get; set; }

    /// <summary>
    /// Timestamp видео в секундах (опционально)
    /// </summary>
    public double? Timestamp { get; set; }

    /// <summary>
    /// Путь/URL видео (опционально)
    /// </summary>
    public string? VideoId { get; set; }

    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// Ключ для дедупликации (word + sentence)
    /// </summary>
    public string DeduplicationKey => $"{Word.ToLowerInvariant()}|{Sentence.Trim()}";

    /// <summary>
    /// Для UI: раскрыта ли карточка
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Дата создания (для сортировки)
    /// </summary>
    public DateTime CreatedAtDateTime => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt).DateTime;
}
