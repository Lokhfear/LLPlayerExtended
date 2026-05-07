using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LLPlayer.Models;

namespace LLPlayer.Services;

/// <summary>
/// Sort modes for learning items
/// </summary>
public enum SortMode
{
    Newest,
    Oldest,
    Alphabetical,
    RecentlyReviewed,
    MostReviewed
}

/// <summary>
/// Filter criteria for querying learning items
/// </summary>
public class LibraryFilter
{
    public string? SearchText { get; set; }
    public bool ShowArchived { get; set; }
    public bool FavoritesOnly { get; set; }
    public ItemType? Type { get; set; }
    public LearningStatus? Status { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool HasMediaOnly { get; set; }
    public SortMode SortBy { get; set; } = SortMode.Newest;
}

/// <summary>
/// Service for managing learning items with storage, deduplication, and querying
/// </summary>
public class LearningItemService
{
    public static readonly string StoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LLPlayer", "learning_items.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<LearningItem>? _cache;

    /// <summary>
    /// Add a new learning item. Returns existing item if duplicate found.
    /// </summary>
    public async Task<(LearningItem item, bool isNew)> AddAsync(LearningItem item)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            var existing = list.FirstOrDefault(x =>
                x.DeduplicationKey == item.DeduplicationKey);

            if (existing != null) return (existing, false);

            list.Add(item);
            await PersistAsync(list);
            return (item, true);
        }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Update an existing learning item
    /// </summary>
    public async Task UpdateAsync(LearningItem item)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            var idx = list.FindIndex(x => x.Id == item.Id);
            if (idx < 0) return;
            
            item.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            list[idx] = item;
            await PersistAsync(list);
        }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Remove a learning item by ID
    /// </summary>
    public async Task RemoveAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            list.RemoveAll(x => x.Id == id);
            await PersistAsync(list);
        }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Get all learning items
    /// </summary>
    public async Task<IReadOnlyList<LearningItem>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try { return (await LoadAsync()).ToList(); }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Query learning items with filter criteria
    /// </summary>
    public async Task<IReadOnlyList<LearningItem>> QueryAsync(LibraryFilter filter)
    {
        var all = await GetAllAsync();
        IEnumerable<LearningItem> q = all;

        // Filter by archived state
        q = filter.ShowArchived
            ? q.Where(x => x.IsArchived)
            : q.Where(x => !x.IsArchived);

        // Filter by favorites
        if (filter.FavoritesOnly)
            q = q.Where(x => x.IsFavorite);

        // Filter by type
        if (filter.Type.HasValue)
            q = q.Where(x => x.Type == filter.Type.Value);

        // Filter by status
        if (filter.Status.HasValue)
            q = q.Where(x => x.Status == filter.Status.Value);

        // Filter by tags (all tags must match)
        if (filter.Tags.Count > 0)
            q = q.Where(x => filter.Tags.All(t => x.Tags.Contains(t)));

        // Filter by media presence
        if (filter.HasMediaOnly)
            q = q.Where(x => x.Media?.HasMedia == true);

        // Search across multiple fields
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var s = filter.SearchText.ToLowerInvariant();
            q = q.Where(x =>
                x.Text.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (x.Translation?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.ContextSentence?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Media?.VideoTitle?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.Tags.Any(t => t.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        // Sorting
        q = filter.SortBy switch
        {
            SortMode.Newest         => q.OrderByDescending(x => x.CreatedAt),
            SortMode.Oldest         => q.OrderBy(x => x.CreatedAt),
            SortMode.Alphabetical   => q.OrderBy(x => x.Text),
            SortMode.RecentlyReviewed => q.OrderByDescending(x => x.LastReviewedAt ?? 0),
            SortMode.MostReviewed   => q.OrderByDescending(x => x.ReviewCount),
            _                       => q.OrderByDescending(x => x.CreatedAt)
        };

        return q.ToList();
    }

    /// <summary>
    /// Get all unique tags from all items
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAllTagsAsync()
    {
        var all = await GetAllAsync();
        return all.SelectMany(x => x.Tags).Distinct().OrderBy(x => x).ToList();
    }

    // ─── Internal ──────────────────────────────────────────────────────────────

    private async Task<List<LearningItem>> LoadAsync()
    {
        if (_cache != null) return _cache;
        
        if (!File.Exists(StoragePath))
        {
            _cache = new List<LearningItem>();
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(StoragePath);
            _cache = JsonSerializer.Deserialize<List<LearningItem>>(json, JsonOpts) ?? new();
        }
        catch 
        { 
            _cache = new(); 
        }
        
        return _cache;
    }

    private async Task PersistAsync(List<LearningItem> list)
    {
        _cache = list;
        Directory.CreateDirectory(Path.GetDirectoryName(StoragePath)!);
        await File.WriteAllTextAsync(StoragePath,
            JsonSerializer.Serialize(list, JsonOpts));
    }
}
