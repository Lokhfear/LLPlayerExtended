using System;
using System.Collections.Generic;

namespace LLPlayer.Models;

/// <summary>
/// Единая сущность для обучения: слово, фраза или предложение.
/// Поддерживает медиа-контекст, статусы, избранное, теги.
/// </summary>
public class LearningItem
{
    // ─── Идентификация ─────────────────────────────────────────────────────
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // ─── Тип и контент ─────────────────────────────────────────────────────
    public ItemType Type { get; set; } = ItemType.Word;

    /// <summary>Исходный текст (слово / фраза / предложение)</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Перевод текста</summary>
    public string? Translation { get; set; }

    /// <summary>
    /// Контекстное предложение (для Word/Phrase — предложение из субтитров).
    /// Для Sentence — совпадает с Text.
    /// </summary>
    public string? ContextSentence { get; set; }

    /// <summary>Перевод контекстного предложения</summary>
    public string? ContextSentenceTranslation { get; set; }

    // ─── Медиа-контекст ────────────────────────────────────────────────────
    public MediaContext? Media { get; set; }

    // ─── Организация ───────────────────────────────────────────────────────
    public LearningStatus Status { get; set; } = LearningStatus.New;
    public bool IsFavorite { get; set; }
    public List<string> Tags { get; set; } = new();

    // ─── Статистика обучения ───────────────────────────────────────────────
    public int ReviewCount { get; set; }
    public long? LastReviewedAt { get; set; }

    // ─── Временные метки ───────────────────────────────────────────────────
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // ─── Дедупликация ──────────────────────────────────────────────────────
    /// <summary>Ключ уникальности: нормализованный text</summary>
    public string DeduplicationKey =>
        Text.Trim().ToLowerInvariant()
            .Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '«', '»');

    // ─── UI State ──────────────────────────────────────────────────────────
    /// <summary>Для UI: раскрыта ли карточка</summary>
    public bool IsExpanded { get; set; }

    // ─── Вычисляемые свойства ──────────────────────────────────────────────
    public bool IsArchived => Status == LearningStatus.Archived;
    
    public string TypeLabel => Type switch
    {
        ItemType.Word     => "W",
        ItemType.Phrase   => "P",
        ItemType.Sentence => "S",
        _                 => "?"
    };
}
