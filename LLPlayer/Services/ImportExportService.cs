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
/// Сервис импорта/экспорта элементов обучения в JSON.
/// </summary>
public class ImportExportService
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly LearningItemService _store;
    
    public ImportExportService(LearningItemService store) => _store = store;

    // ─── EXPORT ────────────────────────────────────────────────────────────

    /// <summary>Экспортировать все элементы в JSON файл.</summary>
    public async Task ExportAsync(string filePath)
    {
        var all = await _store.GetAllAsync();
        var payload = new ExportPayload
        {
            Version = 1,
            ExportedAt = DateTimeOffset.UtcNow.ToString("o"),
            Items = all.ToList()
        };
        var json = JsonSerializer.Serialize(payload, Opts);
        await File.WriteAllTextAsync(filePath, json);
    }

    // ─── IMPORT ────────────────────────────────────────────────────────────

    /// <summary>Импортировать элементы из JSON файла.</summary>
    public async Task<ImportResult> ImportAsync(string filePath, ImportMode mode)
    {
        if (!File.Exists(filePath))
            return ImportResult.Fail("File not found.");

        ExportPayload payload;
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            payload = JsonSerializer.Deserialize<ExportPayload>(json, Opts)
                      ?? throw new Exception("Null payload");
        }
        catch (Exception ex)
        {
            return ImportResult.Fail($"Invalid or corrupted JSON: {ex.Message}");
        }

        if (payload.Items == null || payload.Items.Count == 0)
            return ImportResult.Fail("File contains no items.");

        var result = new ImportResult { Total = payload.Items.Count };

        var existing = await _store.GetAllAsync();
        var existingKeys = existing.ToDictionary(x => x.DeduplicationKey);

        foreach (var item in payload.Items)
        {
            // Валидация обязательных полей
            if (string.IsNullOrWhiteSpace(item.Text)) { result.Invalid++; continue; }

            var key = item.DeduplicationKey;

            if (existingKeys.ContainsKey(key))
            {
                if (mode == ImportMode.Skip) { result.Skipped++; continue; }
                if (mode == ImportMode.Overwrite)
                {
                    var orig = existingKeys[key];
                    item.Id = orig.Id; // сохраняем Id при перезаписи
                    await _store.UpdateAsync(item);
                    result.Overwritten++;
                    continue;
                }
            }

            // Новый элемент — даём свежий UUID
            item.Id = Guid.NewGuid().ToString();
            await _store.AddAsync(item);
            result.Added++;
        }

        result.Success = true;
        return result;
    }
}

// ─── DTO ───────────────────────────────────────────────────────────────────

/// <summary>Формат экспорта.</summary>
public class ExportPayload
{
    public int Version { get; set; }
    public string ExportedAt { get; set; } = string.Empty;
    public List<LearningItem> Items { get; set; } = new();
}

/// <summary>Режим импорта.</summary>
public enum ImportMode { Skip, Overwrite }

/// <summary>Результат импорта.</summary>
public class ImportResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int Total { get; set; }
    public int Added { get; set; }
    public int Skipped { get; set; }
    public int Overwritten { get; set; }
    public int Invalid { get; set; }

    public static ImportResult Fail(string msg)
        => new() { Success = false, Error = msg };

    public string Summary =>
        $"Total: {Total}  |  Added: {Added}  |  " +
        $"Skipped: {Skipped}  |  Overwritten: {Overwritten}  |  Invalid: {Invalid}";
}
