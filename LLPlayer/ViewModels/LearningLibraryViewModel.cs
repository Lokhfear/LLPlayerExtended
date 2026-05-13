using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LLPlayer.Models;
using LLPlayer.Services;

namespace LLPlayer.ViewModels;

/// <summary>
/// ViewModel для окна Learning Library (редизайн в стиле DictionaryControl).
/// </summary>
public class LearningLibraryViewModel : INotifyPropertyChanged
{
    private readonly LearningItemService _service;
    private readonly ImportExportService _importExport;

    // Debounce для поиска
    private CancellationTokenSource? _searchCts;

    public LearningLibraryViewModel(LearningItemService service, ImportExportService ie)
    {
        _service = service;
        _importExport = ie;
        DeleteCommand = new RelayCommand<WordEntry>(OnDelete);
        PlayAtTimestampCommand = new RelayCommand<WordEntry>(OnPlayAtTimestamp);
        RefreshCommand = new RelayCommand(async _ => await LoadEntriesAsync());
    }

    // ─── Коллекции ─────────────────────────────────────────────────────────

    private ObservableCollection<WordEntry> _entries = new();
    public ObservableCollection<WordEntry> Entries
    {
        get => _entries;
        private set 
        { 
            if (_entries != null)
                _entries.CollectionChanged -= Entries_CollectionChanged;
            
            _entries = value; 
            
            if (_entries != null)
                _entries.CollectionChanged += Entries_CollectionChanged;
            
            OnPropertyChanged(); 
            UpdateCountProperties();
        }
    }

    private void Entries_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateCountProperties();
    }

    private void UpdateCountProperties()
    {
        OnPropertyChanged(nameof(HasEntries));
        OnPropertyChanged(nameof(HasNoEntries));
        DisplayedCount = Entries.Count;
    }

    // ─── Свойства ──────────────────────────────────────────────────────────

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            OnSearchChanged();
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set { _totalCount = value; OnPropertyChanged(); }
    }

    private int _displayedCount;
    public int DisplayedCount
    {
        get => _displayedCount;
        set { _displayedCount = value; OnPropertyChanged(); }
    }

    private bool _isPinned;
    public bool IsPinned
    {
        get => _isPinned;
        set { _isPinned = value; OnPropertyChanged(); }
    }

    private string _sortMode = "Newest";
    public string SortMode
    {
        get => _sortMode;
        set
        {
            _sortMode = value;
            OnPropertyChanged();
            OnSortModeChanged();
        }
    }

    // Computed properties for UI visibility (avoid binding to read-only Count)
    public bool HasEntries => Entries.Count > 0;
    public bool HasNoEntries => Entries.Count == 0;

    // ─── Команды ───────────────────────────────────────────────────────────

    public ICommand DeleteCommand { get; }
    public ICommand PlayAtTimestampCommand { get; }
    public ICommand RefreshCommand { get; }

    // ─── Методы ────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        var all = await _service.GetAllAsync();
        TotalCount = all.Count;
        await LoadEntriesAsync();
    }

    public async Task LoadEntriesAsync()
    {
        IsLoading = true;
        try
        {
            var filter = BuildFilter();
            var results = await _service.QueryAsync(filter);

            // Convert LearningItem to WordEntry for the new UI
            var entries = results.Select(item => new WordEntry
            {
                Id = Guid.Parse(item.Id),
                Word = item.Text,
                Translation = item.Translation,
                Sentence = item.ContextSentence,
                SentenceTranslation = item.ContextSentenceTranslation,
                Timestamp = item.Media?.TimestampSeconds > 0 ? item.Media.TimestampSeconds : null,
                VideoId = item.Media?.FilePath,
                CreatedAtDateTime = DateTimeOffset.FromUnixTimeMilliseconds(item.CreatedAt).DateTime
            }).ToList();

            Entries = new ObservableCollection<WordEntry>(entries);
            TotalCount = entries.Count;
            // DisplayedCount and visibility properties are updated via CollectionChanged handler
        }
        finally
        {
            IsLoading = false;
        }
    }

    private LibraryFilter BuildFilter() => new()
    {
        SearchText = SearchText,
        SortBy = SortMode switch
        {
            "Alphabetical" => Services.SortMode.Alphabetical,
            _ => Services.SortMode.Newest
        }
    };

    private void OnSearchChanged()
    {
        // Debounce 300 мс
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                App.Current.Dispatcher.Invoke(async () => await LoadEntriesAsync());
        }, TaskScheduler.Default);
    }

    private void OnSortModeChanged()
    {
        // Reload entries with new sort order
        _ = LoadEntriesAsync();
    }

    private async void OnDelete(WordEntry? entry)
    {
        if (entry == null) return;

        // Find the corresponding LearningItem
        var item = await _service.GetByIdAsync(entry.Id.ToString());
        if (item == null) return;

        // Show confirmation dialog
        var result = MessageBox.Show(
            $"Are you sure you want to delete \"{item.Text}\"?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes) return;

        await _service.RemoveAsync(item.Id);
        
        // Remove from collection on UI thread (CollectionChanged will update counts)
        Application.Current.Dispatcher.Invoke(() =>
        {
            var entryToRemove = Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (entryToRemove != null)
            {
                Entries.Remove(entryToRemove);
                TotalCount--;
            }
        });
    }

    private void OnPlayAtTimestamp(WordEntry? entry)
    {
        if (entry?.Timestamp == null || string.IsNullOrEmpty(entry.VideoId)) return;

        // Попытка получить доступ к плееру через FlyleafManager
        try
        {
            var flManager = ((App)App.Current).Container.Resolve<FlyleafManager>();
            if (flManager?.Player != null)
            {
                // Конвертируем секунды в тики (1 секунда = 10,000,000 тиков)
                long ticks = (long)(entry.Timestamp.Value * 10_000_000);
                flManager.Player.CurTime = ticks;
                
                // Also try to load the video file if it's different
                if (!string.IsNullOrEmpty(entry.VideoId) && 
                    flManager.Player.MediaPath != entry.VideoId)
                {
                    flManager.OpenFile(entry.VideoId);
                }
                
                return;
            }
        }
        catch
        {
            // Игнорируем ошибки доступа к плееру
        }

        // Если не удалось выполнить seek — показываем информационное сообщение
        MessageBox.Show(
            $"Would play video at {TimeSpan.FromSeconds(entry.Timestamp.Value):hh\\:mm\\:ss}\n\nIntegration with main player needed.",
            "Play at Timestamp",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // ─── INotifyPropertyChanged ────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Модель записи словаря для отображения в Learning Library.
/// </summary>
public class WordEntry
{
    public Guid Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string? Translation { get; set; }
    public string? Sentence { get; set; }
    public string? SentenceTranslation { get; set; }
    public double? Timestamp { get; set; }
    public string? VideoId { get; set; }
    public DateTime CreatedAtDateTime { get; set; }
}

/// <summary>
/// Простая реализация ICommand
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);
    public void Execute(object? parameter) => _execute((T?)parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
