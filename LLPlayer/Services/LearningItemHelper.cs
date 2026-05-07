using System;
using System.IO;
using System.Linq;
using LLPlayer.Models;

namespace LLPlayer.Services;

/// <summary>
/// Helper utilities for creating and managing learning items
/// </summary>
public static class LearningItemHelper
{
    /// <summary>
    /// Determine item type based on text content (word count)
    /// </summary>
    public static ItemType DetermineItemType(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ItemType.Word;

        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        return wordCount switch
        {
            1 => ItemType.Word,
            <= 5 => ItemType.Phrase,
            _ => ItemType.Sentence
        };
    }

    /// <summary>
    /// Create a learning item from subtitle context with media reference
    /// </summary>
    public static LearningItem CreateFromSubtitle(
        string text,
        string? currentSubtitleText,
        string? videoPath,
        double? timestampSeconds)
    {
        var type = DetermineItemType(text);

        return new LearningItem
        {
            Type = type,
            Text = text.Trim(),
            ContextSentence = currentSubtitleText,
            Media = new MediaContext
            {
                VideoTitle = !string.IsNullOrWhiteSpace(videoPath) 
                    ? Path.GetFileNameWithoutExtension(videoPath) 
                    : null,
                FilePath = videoPath,
                TimestampSeconds = timestampSeconds
            }
        };
    }

    /// <summary>
    /// Normalize text for display and comparison
    /// </summary>
    public static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim()
            .Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']');
    }

    /// <summary>
    /// Check if a media file exists at the given path
    /// </summary>
    public static bool ValidateMediaPath(string? filePath)
    {
        return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
    }

    /// <summary>
    /// Get a formatted display string for media context
    /// </summary>
    public static string GetMediaDisplayString(MediaContext? media)
    {
        if (media == null || !media.HasMedia)
            return "No media";

        var title = media.VideoTitle ?? "Unknown";
        var time = media.TimestampDisplay ?? "00:00:00";
        
        return $"{title} @ {time}";
    }
}
