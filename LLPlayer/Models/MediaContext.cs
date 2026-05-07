using System;

namespace LLPlayer.Models;

/// <summary>
/// Media context for a learning item - stores video/file reference and timestamp
/// </summary>
public class MediaContext
{
    /// <summary>
    /// Title of the video (filename without extension)
    /// </summary>
    public string? VideoTitle { get; set; }

    /// <summary>
    /// Full path to the local video file
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Timestamp in seconds within the video
    /// </summary>
    public double? TimestampSeconds { get; set; }

    /// <summary>
    /// Formatted timestamp display (hh:mm:ss)
    /// </summary>
    public string? TimestampDisplay => TimestampSeconds.HasValue
        ? TimeSpan.FromSeconds(TimestampSeconds.Value).ToString(@"hh\:mm\:ss")
        : null;

    /// <summary>
    /// Whether this context has a valid media file path
    /// </summary>
    public bool HasMedia => !string.IsNullOrWhiteSpace(FilePath);
}
