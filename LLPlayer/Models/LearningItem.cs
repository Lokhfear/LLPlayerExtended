using System;
using System.Collections.Generic;

namespace LLPlayer.Models;

/// <summary>
/// Unified learning item entity - supports words, phrases, and sentences.
/// Includes media context, learning status, favorites, tags, and review tracking.
/// </summary>
public class LearningItem
{
    /// <summary>
    /// Unique identifier for this item
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of this item: word, phrase, or sentence
    /// </summary>
    public ItemType Type { get; set; } = ItemType.Word;

    /// <summary>
    /// The text content (word/phrase/sentence) to learn
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Translation of the text (optional, may be filled asynchronously)
    /// </summary>
    public string? Translation { get; set; }

    /// <summary>
    /// Context sentence where this item was found (optional)
    /// </summary>
    public string? ContextSentence { get; set; }

    /// <summary>
    /// Translation of the context sentence (optional)
    /// </summary>
    public string? ContextSentenceTranslation { get; set; }

    /// <summary>
    /// Media context - video/file reference and timestamp
    /// </summary>
    public MediaContext? Media { get; set; }

    /// <summary>
    /// Current learning status
    /// </summary>
    public LearningStatus Status { get; set; } = LearningStatus.New;

    /// <summary>
    /// Whether this item is marked as favorite
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// User-defined tags for organization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Number of times this item has been reviewed
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Unix timestamp (ms) of last review
    /// </summary>
    public long? LastReviewedAt { get; set; }

    /// <summary>
    /// Unix timestamp (ms) when this item was created
    /// </summary>
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// Unix timestamp (ms) when this item was last updated
    /// </summary>
    public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// Normalized key for deduplication (lowercase, trimmed punctuation)
    /// </summary>
    public string DeduplicationKey =>
        Text.Trim().ToLowerInvariant()
            .Trim('.', ',', '!', '?', ';', ':', '"', '\'');

    /// <summary>
    /// Whether this item is archived
    /// </summary>
    public bool IsArchived => Status == LearningStatus.Archived;

    /// <summary>
    /// Short type label for UI display (W/P/S)
    /// </summary>
    public string TypeLabel => Type switch
    {
        ItemType.Word     => "W",
        ItemType.Phrase   => "P",
        ItemType.Sentence => "S",
        _                 => "?"
    };
}
