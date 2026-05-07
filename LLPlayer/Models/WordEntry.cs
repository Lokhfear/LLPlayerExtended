using System;

namespace LLPlayer.Models;

/// <summary>
/// Represents a dictionary word entry with translation and metadata.
/// </summary>
public class WordEntry
{
    /// <summary>
    /// Unique identifier for this entry
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The word text
    /// </summary>
    public string Word { get; set; } = string.Empty;

    /// <summary>
    /// Translation of the word (optional, may be filled asynchronously)
    /// </summary>
    public string? Translation { get; set; }

    /// <summary>
    /// Context sentence where this word was found (optional)
    /// </summary>
    public string? Sentence { get; set; }

    /// <summary>
    /// Translation of the context sentence (optional)
    /// </summary>
    public string? SentenceTranslation { get; set; }

    /// <summary>
    /// Unix timestamp (ms) when this entry was created
    /// </summary>
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// Unix timestamp (ms) when this entry was last updated
    /// </summary>
    public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// Normalized key for deduplication (word + sentence, lowercase)
    /// </summary>
    public string DeduplicationKey => $"{Word.Trim().ToLowerInvariant()}|{(Sentence ?? "").Trim().ToLowerInvariant()}";
}
