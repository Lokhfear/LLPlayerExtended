# LLPlayerExtended Architecture Documentation

## Overview

LLPlayerExtended is a scalable language learning system integrated into the LLPlayer video player application. It supports words, phrases, and full sentences with media context, learning workflow, archiving, favorites, filtering, and advanced organization.

---

## Core Architecture

### Directory Structure

```
LLPlayer/
├── Models/
│   ├── ItemType.cs              ← Enum: Word, Phrase, Sentence
│   ├── LearningStatus.cs        ← Enum: New, Learning, Learned, Ignored, Archived
│   ├── MediaContext.cs          ← Video/file reference + timestamp
│   └── WordEntry.cs             → Now LearningItem (unified entity)
│
├── Services/
│   ├── LearningItemService.cs   ← Storage, deduplication, querying
│   ├── ImportExportService.cs   ← JSON import/export
│   ├── LearningItemHelper.cs    ← Utility methods
│   └── DictionaryService.cs     ← Legacy service (kept for compatibility)
│
├── ViewModels/
│   ├── LearningLibraryViewModel.cs  ← Main library window VM
│   └── DictionaryViewModel.cs       ← Legacy dictionary VM
│
├── Views/
│   ├── LearningLibraryWindow.xaml   ← New extended library UI
│   └── LearningLibraryWindow.xaml.cs
│
└── Converters/
    └── StatusToColorConverter.cs    ← Value converters for UI
```

---

## 1. Unified Entity Model: LearningItem

The `LearningItem` class (in `Models/WordEntry.cs`) is the single source of truth for all learning content.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Unique GUID identifier |
| `Type` | ItemType | Word, Phrase, or Sentence |
| `Text` | string | The content to learn |
| `Translation` | string? | Translated text (optional) |
| `ContextSentence` | string? | Original subtitle context |
| `ContextSentenceTranslation` | string? | Translation of context |
| `Media` | MediaContext? | Video/file reference with timestamp |
| `Status` | LearningStatus | Current learning state |
| `IsFavorite` | bool | Favorite flag |
| `Tags` | List<string> | User-defined tags |
| `ReviewCount` | int | Number of reviews |
| `LastReviewedAt` | long? | Last review timestamp (ms) |
| `CreatedAt` | long | Creation timestamp (ms) |
| `UpdatedAt` | long | Last update timestamp (ms) |
| `DeduplicationKey` | string | Normalized text for duplicate detection |
| `IsArchived` | bool | Computed: true if Status == Archived |
| `TypeLabel` | string | Computed: "W", "P", or "S" |

### Deduplication Logic

Duplicates are detected by normalizing the text:
- Convert to lowercase
- Trim whitespace
- Remove punctuation from edges: `. , ! ? ; : " '`

---

## 2. Media Context System

Each `LearningItem` can contain a `MediaContext` object:

```csharp
public class MediaContext
{
    public string? VideoTitle { get; set; }      // Filename without extension
    public string? FilePath { get; set; }        // Full path to video file
    public double? TimestampSeconds { get; set; } // Position in seconds
    public string? TimestampDisplay { get; }     // Formatted: hh:mm:ss
    public bool HasMedia { get; }                // True if FilePath is valid
}
```

### Open Video Behavior

When user clicks "Open Video" button:
1. Check if `Media.FilePath` exists on disk
2. If exists: Open with default system player via `Process.Start()`
3. If missing: Show warning dialog (no crash)
4. Future enhancement: Seek to `TimestampSeconds` in LLPlayer

---

## 3. Import/Export System

### Export Format

JSON structure:
```json
{
  "Version": 1,
  "ExportedAt": "2024-01-15T10:30:00.0000000Z",
  "Items": [
    {
      "Id": "guid-here",
      "Type": "Word",
      "Text": "example",
      "Translation": "пример",
      "Status": "Learning",
      "IsFavorite": true,
      "Tags": ["important"],
      "Media": {
        "VideoTitle": "movie_name",
        "FilePath": "/path/to/video.mp4",
        "TimestampSeconds": 3665.5
      },
      "CreatedAt": 1705312200000,
      ...
    }
  ]
}
```

### Import Modes

| Mode | Behavior |
|------|----------|
| `Skip` | Skip items with matching deduplication key |
| `Overwrite` | Update existing items, preserve original ID |

### Import Result Summary

Returns counts for:
- `Total`: Items in file
- `Added`: New items created
- `Skipped`: Duplicates skipped
- `Overwritten`: Existing items updated
- `Invalid`: Items with missing required fields

---

## 4. Archive System

### Behavior

- Archived items are **hidden** from main list by default
- Enable "Show Archived" checkbox to view them
- Archived items can be restored to `New` status
- Archive filter works independently of other filters

### Status Flow

```
New → Learning → Learned
  ↓       ↓         ↓
Ignored ←─────── Archived (restore → New)
```

---

## 5. Favorites System

### Features

- Toggle favorite with star icon button
- Filter by "Favorites Only" checkbox
- Favorites appear in sorted results based on current sort mode
- Visual indicator: filled star vs outline star

---

## 6. Learning Status System

### Statuses

| Status | Color | Meaning |
|--------|-------|---------|
| `New` | Blue (#2196F3) | Just added, not reviewed |
| `Learning` | Orange (#FF9800) | Currently being learned |
| `Learned` | Green (#4CAF50) | Mastered |
| `Ignored` | Gray (#9E9E9E) | Marked as not useful |
| `Archived` | Brown (#795548) | Hidden from main view |

### Visual Indicators

- Colored badge on left side of each card
- Filter dropdown to show specific status
- Status persists across imports/exports

---

## 7. Filtering & Sorting

### Filters

| Filter | Type | Options |
|--------|------|---------|
| Search | Text | Searches: Text, Translation, ContextSentence, VideoTitle, Tags |
| Type | Dropdown | Word, Phrase, Sentence |
| Status | Dropdown | New, Learning, Learned, Ignored |
| Sort By | Dropdown | Newest, Oldest, Alphabetical, RecentlyReviewed, MostReviewed |
| Favorites Only | Checkbox | True/False |
| Has Media | Checkbox | True/False |
| Show Archived | Checkbox | True/False |

### Search Debounce

Search queries are debounced by 300ms to avoid excessive reloading.

---

## 8. Navigation Structure

### Window Access

Two methods available in `App.xaml.cs`:

```csharp
// Legacy simple dictionary (backward compatible)
App.ShowDictionaryWindow();

// New extended learning library
App.ShowLearningLibraryWindow();
```

### Window Behavior

- Both windows are **singletons** (only one instance)
- Closing hides the window instead of destroying it
- Re-opening shows the same instance with preserved state
- Owner is always set to `MainWindow` for proper modality

---

## 9. UI/UX Design Principles

### Layout Hierarchy

```
┌─────────────────────────────────────────────────────┐
│  Header: Search Box + Import/Export Buttons         │
├─────────────────────────────────────────────────────┤
│  Filter Panel: Type, Status, Sort, Checkboxes       │
├─────────────────────────────────────────────────────┤
│  Stats Bar: "X of Y items shown"                    │
├─────────────────────────────────────────────────────┤
│  Content Area:                                      │
│  - Loading progress bar (top)                       │
│  - Empty state illustration (if no items)           │
│  - Scrollable list of cards                         │
└─────────────────────────────────────────────────────┘
```

### Card Layout

```
┌────┬───────────────────────────────────────┬──────┐
│ W  │ example — пример                      │ ★ 🗑 │
│    │ This is a context sentence            │ ▶ ⏏ │
│    │ 🎬 video_title @ 01:01:05  #tag1 #tag2│      │
│    │ Added: 2024-01-15 · Reviews: 3        │      │
└────┴───────────────────────────────────────┴──────┘
```

### Empty State

- Large icon (bookshelf)
- Title: "No learning items yet"
- Instruction: "Right-click on a word in subtitles..."
- Centered, non-scrollable

---

## 10. Performance Optimizations

### Implemented

1. **SemaphoreSlim locking** - Thread-safe storage access
2. **In-memory cache** - `_cache` field avoids repeated file reads
3. **Debounced search** - 300ms delay before query execution
4. **Selective updates** - Favorite toggle updates single item, not full list
5. **Async I/O** - All file operations use async methods

### Recommendations for Large Lists (>1000 items)

Future enhancements:
- Virtualizing panel (`VirtualizingStackPanel`)
- Pagination or infinite scroll
- Background indexing for search
- Lazy loading of translations

---

## 11. Data Model Extensions

### Timestamps

All timestamps are stored as Unix milliseconds (`long`):
- `CreatedAt`: When item was first added
- `UpdatedAt`: When item was last modified
- `LastReviewedAt`: When item was last reviewed (nullable)

### Review Tracking

- `ReviewCount`: Increment when user marks item as reviewed
- `LastReviewedAt`: Set to current time on review

Future SRS (Spaced Repetition System) can use these fields.

---

## 12. Advanced Search

### Searchable Fields

1. `Text` - Main learning content
2. `Translation` - Translated text
3. `ContextSentence` - Original subtitle
4. `Media.VideoTitle` - Video filename
5. `Tags` - Any user-defined tag

### Case-Insensitive

All searches use `StringComparison.OrdinalIgnoreCase`.

---

## 13. Duplicate Detection

### Algorithm

```csharp
public string DeduplicationKey =>
    Text.Trim().ToLowerInvariant()
        .Trim('.', ',', '!', '?', ';', ':', '"', '\'');
```

### Behavior

- Adding duplicate returns existing item with `isNew = false`
- No action taken (silent skip)
- User can see notification if needed

### Future Enhancement

Fuzzy matching could be added:
- Levenshtein distance
- Phonetic similarity
- Stemming/lemmatization

---

## 14. Error Handling

### File Not Found (Media)

```
┌────────────────────────────┐
│ ⚠️ File Not Found          │
│                            │
│ Video file not found:      │
│ /path/to/missing.mp4       │
│                            │
│              [OK]          │
└────────────────────────────┘
```

### Import Errors

- Corrupted JSON: Show error message with exception details
- Empty file: "File contains no items"
- Invalid items: Counted in `Invalid` result field

### Export Errors

- Catch exceptions and show dialog
- No silent failures

---

## 15. Integration Guide

### Adding Item from Subtitles

In your subtitle click handler:

```csharp
private async Task AddWordToDictionaryAsync(string rawWord)
{
    var word = LearningItemHelper.NormalizeText(rawWord);
    if (string.IsNullOrWhiteSpace(word)) return;

    var item = LearningItemHelper.CreateFromSubtitle(
        text: word,
        currentSubtitleText: CurrentSubtitleText,
        videoPath: Player?.Playlist?.Selected?.Url,
        timestampSeconds: Player?.CurTime is long t 
            ? TimeSpan.FromTicks(t).TotalSeconds 
            : null
    );

    var (saved, isNew) = await App.LearningItemService.AddAsync(item);
    
    if (!isNew) return; // Duplicate

    // Fetch translation asynchronously
    _ = Task.Run(async () =>
    {
        try
        {
            saved.Translation = await TranslateWordAsync(word);
            await App.LearningItemService.UpdateAsync(saved);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Translation error: {ex.Message}");
        }
    });
}
```

### Opening Learning Library

Add menu item or keyboard shortcut:

```csharp
// In MainWindow.xaml.cs or similar
private void OnOpenLearningLibrary(object sender, RoutedEventArgs e)
{
    App.ShowLearningLibraryWindow();
}
```

---

## Migration from Legacy Dictionary

### Backward Compatibility

- Old `DictionaryService` and `WordEntry` are kept
- Old dictionary window still accessible via `App.ShowDictionaryWindow()`
- Data migration can be done manually or via script

### Recommended Migration Path

1. Keep both systems running in parallel
2. Add migration button in settings
3. Migrate `WordEntry` → `LearningItem`:
   ```csharp
   var newItem = new LearningItem
   {
       Id = oldEntry.Id,
       Type = LearningItemHelper.DetermineItemType(oldEntry.Word),
       Text = oldEntry.Word,
       Translation = oldEntry.Translation,
       ContextSentence = oldEntry.Sentence,
       Media = new MediaContext
       {
           FilePath = oldEntry.VideoId,
           TimestampSeconds = oldEntry.Timestamp
       },
       CreatedAt = oldEntry.CreatedAt,
       UpdatedAt = oldEntry.UpdatedAt
   };
   ```

---

## Future Roadmap

### Phase 1 (Completed)
- ✅ Unified entity model
- ✅ Media context system
- ✅ Import/Export
- ✅ Archive/Favorites
- ✅ Advanced filtering
- ✅ Modern UI

### Phase 2 (Planned)
- [ ] Spaced Repetition System (SRS)
- [ ] Quiz/Review mode
- [ ] Statistics dashboard
- [ ] Tag management UI
- [ ] Bulk operations

### Phase 3 (Future)
- [ ] Cloud sync
- [ ] Shared dictionaries
- [ ] AI-powered examples
- [ ] Pronunciation audio
- [ ] Mobile companion app

---

## Support

For issues or questions:
1. Check this documentation
2. Review code comments in source files
3. Examine existing unit tests (if available)
4. Create issue on GitHub repository
