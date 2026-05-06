using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LLPlayer.Models;

namespace LLPlayer.Services;

/// <summary>
/// Сервис словаря. Хранит записи в JSON-файле в AppData.
/// Потокобезопасен через SemaphoreSlim.
/// </summary>
public class DictionaryService
{
    private static readonly string StoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LLPlayer",
        "dictionary.json"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<WordEntry>? _cache;

    // ─── Публичный API ────────────────────────────────────────────────────────

    /// <summary>
    /// Добавить слово. Если word+sentence уже существует — вернуть существующую запись.
    /// </summary>
    public async Task<(WordEntry entry, bool isNew)> AddAsync(WordEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadAsync();

            // Дедупликация по word + sentence
            var existing = entries.FirstOrDefault(e => e.DeduplicationKey == entry.DeduplicationKey);
            if (existing != null)
                return (existing, false);

            entries.Add(entry);
            await SaveAsync(entries);
            return (entry, true);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Обновить запись (например, добавить перевод после асинхронного вызова).
    /// </summary>
    public async Task UpdateAsync(WordEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadAsync();
            var idx = entries.FindIndex(e => e.Id == entry.Id);
            if (idx < 0) return;

            entry.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            entries[idx] = entry;
            await SaveAsync(entries);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Удалить запись по Id.</summary>
    public async Task RemoveAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadAsync();
            entries.RemoveAll(e => e.Id == id);
            await SaveAsync(entries);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Получить все записи, отсортированные по дате (новые сначала).</summary>
    public async Task<IReadOnlyList<WordEntry>> ListAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadAsync();
            return entries.OrderByDescending(e => e.CreatedAt).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Найти по слову (частичное совпадение).</summary>
    public async Task<IReadOnlyList<WordEntry>> FindByWordAsync(string word)
    {
        var all = await ListAsync();
        return all.Where(e =>
            e.Word.Contains(word, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    // ─── Внутреннее ───────────────────────────────────────────────────────────

    private async Task<List<WordEntry>> LoadAsync()
    {
        if (_cache != null) return _cache;

        if (!File.Exists(StoragePath))
        {
            _cache = new List<WordEntry>();
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(StoragePath);
            _cache = JsonSerializer.Deserialize<List<WordEntry>>(json, JsonOptions)
                     ?? new List<WordEntry>();
        }
        catch
        {
            _cache = new List<WordEntry>();
        }

        return _cache;
    }

    private async Task SaveAsync(List<WordEntry> entries)
    {
        _cache = entries;
        Directory.CreateDirectory(Path.GetDirectoryName(StoragePath)!);
        var json = JsonSerializer.Serialize(entries, JsonOptions);
        await File.WriteAllTextAsync(StoragePath, json);
    }
}
